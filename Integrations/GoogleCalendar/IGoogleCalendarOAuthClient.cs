namespace Financial.Integrations.GoogleCalendar;

/// <summary>
/// Generic Google OAuth 2.0 + Calendar API primitives. No CashFlow (or any bounded-context)
/// type appears here - callers pass client id/secret/redirect/scope/tokens as plain strings,
/// matching the "vendor SDK, no bounded-context types" convention every Integrations/* project follows.
/// </summary>
public interface IGoogleCalendarOAuthClient
{
    /// <summary>Builds the Google consent-screen URL for the authorization-code flow.</summary>
    string BuildAuthorizationUrl(string clientId, string redirectUri, string scope, string state);

    /// <summary>Exchanges an authorization code for an access/refresh token pair.</summary>
    Task<GoogleOAuthTokenResult> ExchangeCodeForTokenAsync(
        string clientId, string clientSecret, string redirectUri, string code, CancellationToken cancellationToken = default);

    /// <summary>Uses a stored refresh token to obtain a new access token.</summary>
    Task<GoogleOAuthTokenResult> RefreshAccessTokenAsync(
        string clientId, string clientSecret, string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes an access or refresh token via Google's token-revocation endpoint.</summary>
    Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Returns the email address of the account the access token belongs to.</summary>
    Task<string> GetAccountEmailAsync(string accessToken, CancellationToken cancellationToken = default);

    /// <summary>Creates a calendar in the connected account and returns its Google-assigned id.</summary>
    Task<string> CreateCalendarAsync(string accessToken, string calendarName, CancellationToken cancellationToken = default);

    /// <summary>Returns the id of an existing calendar in the connected account whose name
    /// exactly matches <paramref name="calendarName"/>, or <see langword="null"/> if none
    /// exists.</summary>
    Task<string?> FindCalendarIdByNameAsync(string accessToken, string calendarName, CancellationToken cancellationToken = default);

    /// <summary>Deletes a calendar (and every event in it) from the connected account.</summary>
    Task DeleteCalendarAsync(string accessToken, string calendarId, CancellationToken cancellationToken = default);

    /// <summary>Returns the id of the first event in the calendar whose title starts with
    /// <paramref name="titlePrefix"/>, or <see langword="null"/> if none matches. Throws
    /// <see cref="GoogleCalendarNotFoundException"/> when <paramref name="calendarId"/> no
    /// longer exists.</summary>
    Task<string?> FindEventIdByTitlePrefixAsync(
        string accessToken, string calendarId, string titlePrefix, CancellationToken cancellationToken = default);

    /// <summary>Creates a single all-day event with a fixed 1-day-before popup reminder, and
    /// returns its Google-assigned id. Throws <see cref="GoogleCalendarNotFoundException"/> when
    /// <paramref name="calendarId"/> no longer exists.</summary>
    Task<string> CreateEventAsync(
        string accessToken, string calendarId, string title, string description, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing all-day event's date and content in place. Throws
    /// <see cref="GoogleCalendarNotFoundException"/> when <paramref name="calendarId"/> no longer
    /// exists.</summary>
    Task UpdateEventAsync(
        string accessToken, string calendarId, string eventId, string title, string description, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Deletes a single event. Throws <see cref="GoogleCalendarNotFoundException"/> when
    /// <paramref name="calendarId"/> no longer exists.</summary>
    Task DeleteEventAsync(string accessToken, string calendarId, string eventId, CancellationToken cancellationToken = default);
}
