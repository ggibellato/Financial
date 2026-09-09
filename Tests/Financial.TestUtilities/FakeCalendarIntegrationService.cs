using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;

namespace Financial.TestUtilities;

/// <summary>Hand-written ICalendarIntegrationService test double for consumers that only need
/// <see cref="GetValidAccessTokenAsync"/> (e.g. the credit-card calendar sync service) - the
/// other members throw if accidentally exercised.</summary>
public sealed class FakeCalendarIntegrationService : ICalendarIntegrationService
{
    public string? AccessToken { get; set; } = "access-token";
    public int GetValidAccessTokenCallCount { get; private set; }
    public bool Throws { get; set; }

    /// <summary>Runs after each call, before the result is returned - lets a test simulate a
    /// side effect the real refresh-and-persist flow can have (e.g. the stored connection
    /// becoming revoked) without the fake needing per-call return sequencing.</summary>
    public Action? OnGetValidAccessToken { get; set; }

    public Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        GetValidAccessTokenCallCount++;
        if (Throws)
        {
            throw new InvalidOperationException("Simulated access token retrieval failure.");
        }

        OnGetValidAccessToken?.Invoke();
        return Task.FromResult(AccessToken);
    }

    public string BuildAuthorizationUrl() => throw new NotSupportedException();

    public Task<CalendarCallbackResultDTO> CompleteConnectionAsync(
        string? code, string? state, string? error, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<CalendarConnectionStatusDTO> GetStatusAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<CalendarDisconnectResultDTO> DisconnectAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
