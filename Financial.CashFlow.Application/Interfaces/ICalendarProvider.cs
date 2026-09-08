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

    /// <summary>Returns the id of an existing calendar with this exact name in the connected
    /// account, or <see langword="null"/> if none exists - lets connecting reuse a previously
    /// created dedicated calendar instead of creating a duplicate.</summary>
    Task<string?> FindCalendarByNameAsync(string accessToken, string calendarName, CancellationToken cancellationToken = default);

    Task DeleteCalendarAsync(string accessToken, string calendarId, CancellationToken cancellationToken = default);

    /// <summary>Returns the id of the first event in the calendar whose title starts with
    /// <paramref name="titlePrefix"/>, or <see langword="null"/> if none matches - lets a sync
    /// reconcile with an event that already exists (e.g. in a reused calendar, see
    /// <see cref="FindCalendarByNameAsync"/>) instead of creating a duplicate. Throws
    /// <see cref="Exceptions.CalendarNotFoundException"/> when <paramref name="calendarId"/> no
    /// longer exists.</summary>
    Task<string?> FindEventIdByTitlePrefixAsync(
        string accessToken, string calendarId, string titlePrefix, CancellationToken cancellationToken = default);

    /// <summary>Creates a single all-day event with a fixed 1-day-before popup reminder, and
    /// returns its provider-assigned id. Throws <see cref="Exceptions.CalendarNotFoundException"/>
    /// when <paramref name="calendarId"/> no longer exists.</summary>
    Task<string> CreateEventAsync(
        string accessToken, string calendarId, string title, string description, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing all-day event's date and content in place. Throws
    /// <see cref="Exceptions.CalendarNotFoundException"/> when <paramref name="calendarId"/> no
    /// longer exists.</summary>
    Task UpdateEventAsync(
        string accessToken, string calendarId, string eventId, string title, string description, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Deletes a single event. Throws <see cref="Exceptions.CalendarNotFoundException"/>
    /// when <paramref name="calendarId"/> no longer exists.</summary>
    Task DeleteEventAsync(string accessToken, string calendarId, string eventId, CancellationToken cancellationToken = default);
}
