namespace Financial.CashFlow.Application.DTOs;

public sealed class CalendarConnectionStatusDTO
{
    public required bool Connected { get; init; }

    public string? AccountEmail { get; init; }

    public string? CalendarName { get; init; }

    public DateTimeOffset? ConnectedAtUtc { get; init; }

    /// <summary>Null unless a stored connection is known-broken - currently only <c>"token_revoked"</c>.</summary>
    public string? DisconnectReason { get; init; }
}
