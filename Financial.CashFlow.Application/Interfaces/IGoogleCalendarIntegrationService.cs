using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IGoogleCalendarIntegrationService
{
    /// <summary>Issues a fresh CSRF state value and returns the Google consent-screen URL.</summary>
    string BuildAuthorizationUrl();

    /// <summary>Handles the OAuth callback: validates <paramref name="state"/>, exchanges
    /// <paramref name="code"/>, creates the dedicated calendar, and persists the connection -
    /// or records why it failed.</summary>
    Task<GoogleCalendarCallbackResultDTO> CompleteConnectionAsync(
        string? code, string? state, string? error, CancellationToken cancellationToken = default);

    Task<GoogleCalendarConnectionStatusDTO> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Always clears local connection state, even when the remote calendar-delete or
    /// token-revoke call fails. Idempotent when nothing is connected.</summary>
    Task<GoogleCalendarDisconnectResultDTO> DisconnectAsync(CancellationToken cancellationToken = default);
}
