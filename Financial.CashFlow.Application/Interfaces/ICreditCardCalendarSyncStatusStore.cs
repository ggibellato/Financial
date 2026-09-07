using Financial.CashFlow.Application.Models;

namespace Financial.CashFlow.Application.Interfaces;

/// <summary>In-memory tracking of each credit card's calendar-event sync status. Not persisted -
/// this is operational state, re-derived on the next save or manual resync.</summary>
public interface ICreditCardCalendarSyncStatusStore
{
    void SetPending(Guid creditCardId);

    void SetSynced(Guid creditCardId, DateTimeOffset syncedAtUtc);

    void SetError(Guid creditCardId, string error);

    /// <summary>Removes the card's tracked status entirely (it no longer has an event to track).</summary>
    void Clear(Guid creditCardId);

    CreditCardCalendarSyncStatus? GetStatus(Guid creditCardId);

    IReadOnlyDictionary<Guid, CreditCardCalendarSyncStatus> GetAllStatuses();
}
