namespace Financial.CashFlow.Application.Models;

/// <summary>
/// The connection state persisted to the local Google Calendar credentials file (separate from
/// <c>data-cashflow.json</c>). <see cref="RevokedReason"/> is set once a refresh attempt fails
/// with "invalid_grant", so later status checks stop retrying the same failed refresh.
/// </summary>
public sealed record GoogleCalendarConnection(
    string AccountEmail,
    string CalendarId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset ConnectedAtUtc,
    string? RevokedReason = null);
