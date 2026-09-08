using System.Security.Cryptography;
using Financial.CashFlow.Application.Configuration;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Models;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

/// <summary>
/// Owns the calendar-connection lifecycle rules against <see cref="ICalendarProvider"/> and
/// <see cref="ICalendarConnectionStore"/> alone - no provider-specific type or concept appears
/// here. Which provider is actually behind <see cref="ICalendarProvider"/> is an Infrastructure
/// DI decision, not something this class knows.
/// </summary>
public sealed class CalendarIntegrationService : ICalendarIntegrationService
{
    private const string EntityType = "CalendarConnection";
    private const string TokenRevokedReason = "token_revoked";
    private static readonly TimeSpan PendingStateLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AccessTokenRefreshSkew = TimeSpan.FromMinutes(1);

    private readonly ICalendarProvider _provider;
    private readonly ICalendarConnectionStore _store;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<CalendarIntegrationService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly object _stateLock = new();

    private string? _pendingState;
    private DateTimeOffset _pendingStateExpiresAtUtc;

    public CalendarIntegrationService(
        ICalendarProvider provider,
        ICalendarConnectionStore store,
        ITelemetryTracer tracer,
        ILogger<CalendarIntegrationService> logger,
        TimeProvider timeProvider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public string BuildAuthorizationUrl()
    {
        using var span = StartSpan("BuildAuthorizationUrl");
        try
        {
            var state = GenerateState();
            lock (_stateLock)
            {
                _pendingState = state;
                _pendingStateExpiresAtUtc = _timeProvider.GetUtcNow() + PendingStateLifetime;
            }

            var url = _provider.BuildAuthorizationUrl(state);
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "BuildAuthorizationUrl");
            return url;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<CalendarCallbackResultDTO> CompleteConnectionAsync(
        string? code, string? state, string? error, CancellationToken cancellationToken = default)
    {
        using var span = StartSpan("CompleteConnection");
        try
        {
            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || !IsPendingStateValid(state))
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "CompleteConnection");
                return Failure("Connection was not completed.");
            }

            ClearPendingState();

            var token = await _provider.ExchangeCodeForTokenAsync(code, cancellationToken).ConfigureAwait(false);

            var previous = _store.Load();
            if (previous is not null)
            {
                await BestEffortDisconnectRemoteAsync(previous, cancellationToken).ConfigureAwait(false);
            }

            string accountEmail;
            string calendarId;
            try
            {
                accountEmail = await _provider.GetAccountEmailAsync(token.AccessToken, cancellationToken).ConfigureAwait(false);
                calendarId = await _provider.CreateCalendarAsync(token.AccessToken, CalendarDefaults.DedicatedCalendarName, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "{Operation} failed to finish connecting, revoking the just-issued token: {ExceptionType}",
                    "CompleteConnection", ex.GetType().Name);
                await TryRevokeAsync(token.AccessToken, cancellationToken).ConfigureAwait(false);
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "CompleteConnection");
                return Failure("Couldn't finish connecting - please try again.");
            }

            var refreshToken = token.RefreshToken
                ?? throw new InvalidOperationException("The provider did not issue a refresh token for this connection.");

            _store.Save(new CalendarConnection(
                accountEmail,
                calendarId,
                token.AccessToken,
                refreshToken,
                token.AccessTokenExpiresAtUtc,
                _timeProvider.GetUtcNow(),
                new Dictionary<Guid, string>()));

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "CompleteConnection");
            return new CalendarCallbackResultDTO { Success = true };
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<CalendarConnectionStatusDTO> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        using var span = StartSpan("GetStatus");
        try
        {
            var connection = _store.Load();
            if (connection is null)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetStatus");
                return new CalendarConnectionStatusDTO { Connected = false };
            }

            if (connection.RevokedReason is not null)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetStatus");
                return ToStatusDto(connection, connected: false);
            }

            var refreshed = await EnsureFreshAccessTokenAsync(connection, cancellationToken).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetStatus");
            return refreshed is null
                ? ToStatusDto(connection with { RevokedReason = TokenRevokedReason }, connected: false)
                : ToStatusDto(refreshed, connected: true);
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<CalendarDisconnectResultDTO> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        using var span = StartSpan("Disconnect");
        try
        {
            var connection = _store.Load();
            if (connection is null)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "Disconnect");
                return new CalendarDisconnectResultDTO { RemoteCleanupSucceeded = true };
            }

            var remoteCleanupSucceeded = await RevokeAccessOnlyAsync(connection, cancellationToken).ConfigureAwait(false);
            _store.Delete();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "Disconnect");
            return new CalendarDisconnectResultDTO { RemoteCleanupSucceeded = remoteCleanupSucceeded };
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        using var span = StartSpan("GetValidAccessToken");
        try
        {
            var connection = _store.Load();
            if (connection is null || connection.RevokedReason is not null)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetValidAccessToken");
                return null;
            }

            var refreshed = await EnsureFreshAccessTokenAsync(connection, cancellationToken).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetValidAccessToken");
            return refreshed?.AccessToken;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    /// <summary>Refreshes the access token when it is at or near expiry, persisting the result.
    /// Returns <see langword="null"/> - after tombstoning the stored connection with
    /// <c>"token_revoked"</c> - when the provider reports the refresh token itself was revoked.</summary>
    private async Task<CalendarConnection?> EnsureFreshAccessTokenAsync(
        CalendarConnection connection, CancellationToken cancellationToken)
    {
        if (_timeProvider.GetUtcNow() + AccessTokenRefreshSkew < connection.AccessTokenExpiresAtUtc)
        {
            return connection;
        }

        try
        {
            var refreshedToken = await _provider.RefreshAccessTokenAsync(connection.RefreshToken, cancellationToken).ConfigureAwait(false);
            var updated = connection with
            {
                AccessToken = refreshedToken.AccessToken,
                RefreshToken = refreshedToken.RefreshToken ?? connection.RefreshToken,
                AccessTokenExpiresAtUtc = refreshedToken.AccessTokenExpiresAtUtc
            };
            _store.Save(updated);
            return updated;
        }
        catch (CalendarTokenRevokedException)
        {
            _store.Save(connection with { RevokedReason = TokenRevokedReason });
            return null;
        }
    }

    /// <summary>Best-effort: revokes the token only, leaving the dedicated calendar and its events
    /// intact in the user's Google account - the user asked to stop the app managing their
    /// calendar, not to delete data they may still want. Logs (not throws) on failure. Never
    /// touches local storage - callers decide what to persist.</summary>
    private async Task<bool> RevokeAccessOnlyAsync(CalendarConnection connection, CancellationToken cancellationToken)
    {
        if (connection.RevokedReason is not null)
        {
            return false;
        }

        var fresh = await EnsureFreshAccessTokenAsync(connection, cancellationToken).ConfigureAwait(false);
        if (fresh is null)
        {
            return false;
        }

        try
        {
            await _provider.RevokeTokenAsync(fresh.AccessToken, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{Operation} failed to revoke the token: {ExceptionType}", "Disconnect", ex.GetType().Name);
            return false;
        }
    }

    /// <summary>Best-effort: deletes the dedicated calendar and revokes the token, logging (not
    /// throwing) on any failure. Used only when a new connection replaces this one (the new
    /// account gets its own fresh dedicated calendar, so the old one is no longer reachable
    /// through the app and would otherwise be orphaned) - never touches local storage.</summary>
    private async Task<bool> BestEffortDisconnectRemoteAsync(CalendarConnection connection, CancellationToken cancellationToken)
    {
        if (connection.RevokedReason is not null)
        {
            return false;
        }

        var fresh = await EnsureFreshAccessTokenAsync(connection, cancellationToken).ConfigureAwait(false);
        if (fresh is null)
        {
            return false;
        }

        var succeeded = true;

        try
        {
            await _provider.DeleteCalendarAsync(fresh.AccessToken, fresh.CalendarId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{Operation} failed to delete the dedicated calendar: {ExceptionType}", "Disconnect", ex.GetType().Name);
            succeeded = false;
        }

        try
        {
            await _provider.RevokeTokenAsync(fresh.AccessToken, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{Operation} failed to revoke the token: {ExceptionType}", "Disconnect", ex.GetType().Name);
            succeeded = false;
        }

        return succeeded;
    }

    private async Task TryRevokeAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            await _provider.RevokeTokenAsync(accessToken, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{Operation} failed to revoke the just-issued token: {ExceptionType}", "CompleteConnection", ex.GetType().Name);
        }
    }

    private bool IsPendingStateValid(string? state)
    {
        lock (_stateLock)
        {
            return !string.IsNullOrEmpty(state)
                && string.Equals(_pendingState, state, StringComparison.Ordinal)
                && _timeProvider.GetUtcNow() <= _pendingStateExpiresAtUtc;
        }
    }

    private void ClearPendingState()
    {
        lock (_stateLock)
        {
            _pendingState = null;
        }
    }

    private static string GenerateState() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    private static CalendarConnectionStatusDTO ToStatusDto(CalendarConnection connection, bool connected) => new()
    {
        Connected = connected,
        AccountEmail = connection.AccountEmail,
        CalendarName = CalendarDefaults.DedicatedCalendarName,
        CalendarId = connection.CalendarId,
        ConnectedAtUtc = connection.ConnectedAtUtc,
        DisconnectReason = connection.RevokedReason
    };

    private static CalendarCallbackResultDTO Failure(string message) => new() { Success = false, ErrorMessage = message };

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(CalendarIntegrationService), operationName, EntityType);
    }
}
