using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;

namespace Financial.TestUtilities;

public sealed class FakeCreditCardCalendarSyncService : ICreditCardCalendarSyncService
{
    public List<Guid> TriggeredCreditCardIds { get; } = new();

    public List<(Guid CreditCardId, DateOnly InvoiceDate)> InvoiceChecks { get; } = new();

    public void TriggerSync(Guid creditCardId) => TriggeredCreditCardIds.Add(creditCardId);

    public void TriggerSyncIfInvoiceSynced(Guid creditCardId, DateOnly invoiceDate) => InvoiceChecks.Add((creditCardId, invoiceDate));

    public Task<CreditCardCalendarSyncStatusDTO> ResyncAsync(Guid creditCardId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<CreditCardCalendarSyncStatusDTO>> ResyncAllAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IReadOnlyList<CreditCardCalendarSyncStatusDTO> GetSyncStatuses() => Array.Empty<CreditCardCalendarSyncStatusDTO>();
}
