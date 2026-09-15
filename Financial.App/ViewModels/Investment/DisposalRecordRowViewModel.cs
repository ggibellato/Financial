using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;

namespace Financial.Presentation.App.ViewModels.Investment;

/// <summary>
/// One Active DisposalRecord plus its resolved superseded chain (oldest first), grouped by
/// TransactionId - stable across F03 regeneration, see DisposalRecordRegenerator.ComputePlan's
/// existingByTransactionId lookup.
/// </summary>
public sealed class DisposalRecordRowViewModel
{
    public DisposalRecordRowViewModel(DisposalRecordDTO record, IReadOnlyList<DisposalRecordDTO> supersededHistory)
    {
        Record = record;
        SupersededHistory = supersededHistory;
    }

    public DisposalRecordDTO Record { get; }
    public IReadOnlyList<DisposalRecordDTO> SupersededHistory { get; }

    public bool HasHistory => SupersededHistory.Count > 0;

    public DateTime Date => Record.Date;
    public decimal QuantityDisposed => Record.QuantityDisposed;
    public CostBasisMethod Method => Record.Method;
    public decimal Proceeds => Record.Proceeds;
    public decimal CostBasis => Record.CostBasis;
    public decimal GainLoss => Record.GainLoss;
    public string Currency => Record.Currency;
    public string TaxYear => Record.TaxYear;
}
