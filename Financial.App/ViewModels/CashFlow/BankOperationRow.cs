using Financial.CashFlow.Application.DTOs;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public enum BankOperationKind
{
    Transfer,
    Adjustment,
}

/// <summary>
/// One row of the Bank tab's flat, cross-bank operations list: a single Transfer or Balance
/// Adjustment, carrying both its display fields and the fields needed to filter it by bank.
/// Replaces the per-bank <c>BankHistoryEntry</c> now that operations are shown in one combined
/// list rather than expanded per bank row.
/// </summary>
public sealed class BankOperationRow
{
    public required BankOperationKind Kind { get; init; }
    public required DateOnly Date { get; init; }
    public required string BankLabel { get; init; }
    public string? SourceBank { get; init; }
    public string? DestinationBank { get; init; }
    public string? Bank { get; init; }
    public decimal? Amount { get; init; }
    public decimal? Delta { get; init; }
    public string? Note { get; init; }
    public TransferDTO? Transfer { get; init; }
    public BalanceAdjustmentDTO? Adjustment { get; init; }

    /// <summary>Single Amount/Delta grid column value: the transfer amount, or the adjustment's signed delta.</summary>
    public decimal DisplayAmount => Kind == BankOperationKind.Transfer ? Amount ?? 0m : Delta ?? 0m;

    /// <summary>True when the selected bank filter matches this row: source-or-destination for a transfer, exact bank for an adjustment.</summary>
    public bool MatchesBank(string bankName) => Kind == BankOperationKind.Transfer
        ? SourceBank == bankName || DestinationBank == bankName
        : Bank == bankName;

    public static BankOperationRow FromTransfer(TransferDTO transfer) => new()
    {
        Kind = BankOperationKind.Transfer,
        Date = transfer.Date,
        BankLabel = $"{transfer.SourceBank} → {transfer.DestinationBank}",
        SourceBank = transfer.SourceBank,
        DestinationBank = transfer.DestinationBank,
        Amount = transfer.Amount,
        Note = transfer.Note,
        Transfer = transfer,
    };

    public static BankOperationRow FromAdjustment(BalanceAdjustmentDTO adjustment) => new()
    {
        Kind = BankOperationKind.Adjustment,
        Date = adjustment.Date,
        BankLabel = adjustment.Bank,
        Bank = adjustment.Bank,
        Delta = adjustment.Delta,
        Note = adjustment.Note,
        Adjustment = adjustment,
    };
}
