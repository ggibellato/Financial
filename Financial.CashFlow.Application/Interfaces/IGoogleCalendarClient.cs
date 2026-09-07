using Financial.CashFlow.Application.Models;

namespace Financial.CashFlow.Application.Interfaces;

/// <summary>
/// Application-owned abstraction over Google OAuth + Calendar operations. Implemented in
/// Infrastructure by delegating to <c>Integrations/GoogleCalendar</c>'s
/// <c>IGoogleCalendarOAuthClient</c>, so Application never references that project's Google SDK
/// types directly (mirrors why <c>IRemoteFileClient</c> exists for Google Drive).
/// </summary>
public interface IGoogleCalendarClient
{
    string BuildAuthorizationUrl(string state);

    Task<GoogleCalendarTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws <see cref="Exceptions.GoogleCalendarTokenRevokedException"/> when Google rejects
    /// the refresh token (revoked externally), instead of returning a result the caller could
    /// mistake for a transient failure.
    /// </summary>
    Task<GoogleCalendarTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<string> GetAccountEmailAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<string> CreateCalendarAsync(string accessToken, string calendarName, CancellationToken cancellationToken = default);

    Task DeleteCalendarAsync(string accessToken, string calendarId, CancellationToken cancellationToken = default);
}
