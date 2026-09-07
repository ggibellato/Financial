namespace Financial.CashFlow.Application.Models;

/// <summary>
/// The connection state persisted to the local calendar-provider credentials file (separate from
/// <c>data-cashflow.json</c>). Provider-agnostic in shape - it holds whichever
/// <see cref="Interfaces.ICalendarProvider"/> implementation is wired in today - even though only
/// a Google Calendar connection exists right now. <see cref="RevokedReason"/> is set once a
/// refresh attempt fails, so later status checks stop retrying the same failed refresh.
/// </summary>
public sealed record CalendarConnection(
    string AccountEmail,
    string CalendarId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset ConnectedAtUtc,
    string? RevokedReason = null);
