using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ICalendarIntegrationService
{
    /// <summary>Issues a fresh CSRF state value and returns the provider's consent-screen URL.</summary>
    string BuildAuthorizationUrl();

    /// <summary>Handles the OAuth callback: validates <paramref name="state"/>, exchanges
    /// <paramref name="code"/>, creates the dedicated calendar, and persists the connection -
    /// or records why it failed.</summary>
    Task<CalendarCallbackResultDTO> CompleteConnectionAsync(
        string? code, string? state, string? error, CancellationToken cancellationToken = default);

    Task<CalendarConnectionStatusDTO> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Always clears local connection state, even when the remote calendar-delete or
    /// token-revoke call fails. Idempotent when nothing is connected.</summary>
    Task<CalendarDisconnectResultDTO> DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a currently-valid access token, transparently refreshing it first if
    /// needed - the same check <see cref="GetStatusAsync"/> performs, reused by
    /// credit-card-event sync so it never duplicates the refresh/tombstone logic. Returns
    /// <see langword="null"/> when not connected or the connection is known-revoked.</summary>
    Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default);
}
