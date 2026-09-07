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

    public Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        GetValidAccessTokenCallCount++;
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
