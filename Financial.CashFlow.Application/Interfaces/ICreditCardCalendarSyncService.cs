using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface ICreditCardCalendarSyncService
{
    /// <summary>Fire-and-forget: marks the card <c>Pending</c> immediately, then dispatches the
    /// actual sync in the background. Never throws and never blocks the caller - safe to call
    /// from a save path without affecting that save's own success.</summary>
    void TriggerSync(Guid creditCardId);

    /// <summary>Triggers a sync only when <paramref name="invoiceDate"/> falls in the invoice
    /// period the card's calendar event currently reports (its next due date's month) - changes to
    /// other, still-open periods leave the event untouched.</summary>
    void TriggerSyncIfInvoiceSynced(Guid creditCardId, DateOnly invoiceDate);

    /// <summary>Awaited manual retry for one card - runs the same logic as <see cref="TriggerSync"/>,
    /// returning the resulting status once the attempt completes.</summary>
    Task<CreditCardCalendarSyncStatusDTO> ResyncAsync(Guid creditCardId, CancellationToken cancellationToken = default);

    /// <summary>Awaited manual retry for every active credit card with a due date, run
    /// sequentially (every sync reads/writes the same connection record).</summary>
    Task<IReadOnlyList<CreditCardCalendarSyncStatusDTO>> ResyncAllAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<CreditCardCalendarSyncStatusDTO> GetSyncStatuses();
}
