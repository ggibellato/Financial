using Financial.CashFlow.Application.Models;

namespace Financial.CashFlow.Application.Interfaces;

/// <summary>
/// The standard contract any calendar provider must implement to back the app's calendar
/// integration - OAuth-style connect/refresh/revoke plus creating and deleting a dedicated
/// calendar. Nothing here is provider-specific: no client id/secret, no vendor SDK types, no
/// mention of Google. <c>Financial.CashFlow.Infrastructure</c>'s <c>GoogleCalendarProviderAdapter</c>
/// is the first implementation; a second provider is added by writing a new Infrastructure
/// adapter and pointing DI at it, without touching Application.
/// </summary>
public interface ICalendarProvider
{
    string BuildAuthorizationUrl(string state);

    Task<CalendarTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws <see cref="Exceptions.CalendarTokenRevokedException"/> when the provider rejects
    /// the refresh token (revoked externally), instead of returning a result the caller could
    /// mistake for a transient failure.
    /// </summary>
    Task<CalendarTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<string> GetAccountEmailAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<string> CreateCalendarAsync(string accessToken, string calendarName, CancellationToken cancellationToken = default);

    Task DeleteCalendarAsync(string accessToken, string calendarId, CancellationToken cancellationToken = default);
}
