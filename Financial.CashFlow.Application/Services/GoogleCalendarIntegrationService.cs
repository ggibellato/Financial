using System.Security.Cryptography;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Models;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Application.Services;

public sealed class GoogleCalendarIntegrationService : IGoogleCalendarIntegrationService
{
    private const string EntityType = "GoogleCalendarConnection";
    private const string DedicatedCalendarName = "Financial - Credit Card Due Dates";
    private const string TokenRevokedReason = "token_revoked";
    private static readonly TimeSpan PendingStateLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AccessTokenRefreshSkew = TimeSpan.FromMinutes(1);

    private readonly IGoogleCalendarClient _client;
    private readonly IGoogleCalendarConnectionStore _store;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<GoogleCalendarIntegrationService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly object _stateLock = new();

    private string? _pendingState;
    private DateTimeOffset _pendingStateExpiresAtUtc;

    public GoogleCalendarIntegrationService(
        IGoogleCalendarClient client,
        IGoogleCalendarConnectionStore store,
        ITelemetryTracer tracer,
        ILogger<GoogleCalendarIntegrationService> logger,
        TimeProvider timeProvider)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
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

            var url = _client.BuildAuthorizationUrl(state);
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

    public async Task<GoogleCalendarCallbackResultDTO> CompleteConnectionAsync(
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

            var token = await _client.ExchangeCodeForTokenAsync(code, cancellationToken).ConfigureAwait(false);

            var previous = _store.Load();
            if (previous is not null)
            {
                await BestEffortDisconnectRemoteAsync(previous, cancellationToken).ConfigureAwait(false);
            }

            string accountEmail;
            string calendarId;
            try
            {
                accountEmail = await _client.GetAccountEmailAsync(token.AccessToken, cancellationToken).ConfigureAwait(false);
                calendarId = await _client.CreateCalendarAsync(token.AccessToken, DedicatedCalendarName, cancellationToken).ConfigureAwait(false);
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
                ?? throw new InvalidOperationException("Google did not issue a refresh token for this connection.");

            _store.Save(new GoogleCalendarConnection(
                accountEmail,
                calendarId,
                token.AccessToken,
                refreshToken,
                token.AccessTokenExpiresAtUtc,
                _timeProvider.GetUtcNow()));

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "CompleteConnection");
            return new GoogleCalendarCallbackResultDTO { Success = true };
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<GoogleCalendarConnectionStatusDTO> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        using var span = StartSpan("GetStatus");
        try
        {
            var connection = _store.Load();
            if (connection is null)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetStatus");
                return new GoogleCalendarConnectionStatusDTO { Connected = false };
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

    public async Task<GoogleCalendarDisconnectResultDTO> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        using var span = StartSpan("Disconnect");
        try
        {
            var connection = _store.Load();
            if (connection is null)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "Disconnect");
                return new GoogleCalendarDisconnectResultDTO { RemoteCleanupSucceeded = true };
            }

            var remoteCleanupSucceeded = await BestEffortDisconnectRemoteAsync(connection, cancellationToken).ConfigureAwait(false);
            _store.Delete();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "Disconnect");
            return new GoogleCalendarDisconnectResultDTO { RemoteCleanupSucceeded = remoteCleanupSucceeded };
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    /// <summary>Refreshes the access token when it is at or near expiry, persisting the result.
    /// Returns <see langword="null"/> - after tombstoning the stored connection with
    /// <c>"token_revoked"</c> - when Google reports the refresh token itself was revoked.</summary>
    private async Task<GoogleCalendarConnection?> EnsureFreshAccessTokenAsync(
        GoogleCalendarConnection connection, CancellationToken cancellationToken)
    {
        if (_timeProvider.GetUtcNow() + AccessTokenRefreshSkew < connection.AccessTokenExpiresAtUtc)
        {
            return connection;
        }

        try
        {
            var refreshedToken = await _client.RefreshAccessTokenAsync(connection.RefreshToken, cancellationToken).ConfigureAwait(false);
            var updated = connection with
            {
                AccessToken = refreshedToken.AccessToken,
                RefreshToken = refreshedToken.RefreshToken ?? connection.RefreshToken,
                AccessTokenExpiresAtUtc = refreshedToken.AccessTokenExpiresAtUtc
            };
            _store.Save(updated);
            return updated;
        }
        catch (GoogleCalendarTokenRevokedException)
        {
            _store.Save(connection with { RevokedReason = TokenRevokedReason });
            return null;
        }
    }

    /// <summary>Best-effort: deletes the dedicated calendar and revokes the token, logging (not
    /// throwing) on any failure. Never touches local storage - callers decide what to persist.</summary>
    private async Task<bool> BestEffortDisconnectRemoteAsync(GoogleCalendarConnection connection, CancellationToken cancellationToken)
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
            await _client.DeleteCalendarAsync(fresh.AccessToken, fresh.CalendarId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("{Operation} failed to delete the dedicated calendar: {ExceptionType}", "Disconnect", ex.GetType().Name);
            succeeded = false;
        }

        try
        {
            await _client.RevokeTokenAsync(fresh.AccessToken, cancellationToken).ConfigureAwait(false);
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
            await _client.RevokeTokenAsync(accessToken, cancellationToken).ConfigureAwait(false);
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

    private static GoogleCalendarConnectionStatusDTO ToStatusDto(GoogleCalendarConnection connection, bool connected) => new()
    {
        Connected = connected,
        AccountEmail = connection.AccountEmail,
        CalendarName = DedicatedCalendarName,
        ConnectedAtUtc = connection.ConnectedAtUtc,
        DisconnectReason = connection.RevokedReason
    };

    private static GoogleCalendarCallbackResultDTO Failure(string message) => new() { Success = false, ErrorMessage = message };

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("CashFlow", nameof(GoogleCalendarIntegrationService), operationName, EntityType);
    }
}
