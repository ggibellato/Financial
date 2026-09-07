namespace Financial.CashFlow.Application.Models;

/// <summary>A credit card's calendar-event sync state: flips to <see cref="Pending"/> immediately
/// after a triggering save, then to <see cref="Synced"/> or <see cref="Error"/> once the
/// background sync attempt completes.</summary>
public enum CreditCardCalendarSyncState
{
    Pending,
    Synced,
    Error
}
