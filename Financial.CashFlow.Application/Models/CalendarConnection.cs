namespace Financial.CashFlow.Application.Models;

/// <summary>
/// The connection state persisted to the local calendar-provider credentials file (separate from
/// <c>data-cashflow.json</c>). Provider-agnostic in shape - it holds whichever
/// <see cref="Interfaces.ICalendarProvider"/> implementation is wired in today - even though only
/// a Google Calendar connection exists right now. <see cref="RevokedReason"/> is set once a
/// refresh attempt fails, so later status checks stop retrying the same failed refresh.
/// <see cref="CardEventIds"/> maps a credit card's id to its persistent calendar event id, so a
/// later sync updates the same event instead of creating a second one.
/// </summary>
public sealed record CalendarConnection(
    string AccountEmail,
    string CalendarId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset ConnectedAtUtc,
    IReadOnlyDictionary<Guid, string> CardEventIds,
    string? RevokedReason = null);
