namespace Financial.CashFlow.Application.DTOs;

public sealed class CalendarDisconnectResultDTO
{
    /// <summary>False when the remote calendar-delete or token-revoke call failed even though
    /// local connection state was still cleared.</summary>
    public required bool RemoteCleanupSucceeded { get; init; }
}
