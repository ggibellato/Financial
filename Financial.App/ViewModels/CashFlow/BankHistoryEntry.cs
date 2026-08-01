using Financial.CashFlow.Application.DTOs;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public enum BankHistoryEntryKind
{
    TransferIn,
    TransferOut,
    Adjustment,
}

/// <summary>One line in a bank's expandable transfer/adjustment history, mirroring useBankHistory.ts's BankHistoryEntry.</summary>
public sealed class BankHistoryEntry
{
    public required BankHistoryEntryKind Kind { get; init; }
    public required DateOnly Date { get; init; }
    public string? CounterpartBank { get; init; }
    public decimal? Amount { get; init; }
    public decimal? Delta { get; init; }
    public string? Note { get; init; }
    public TransferDTO? Transfer { get; init; }
    public BalanceAdjustmentDTO? Adjustment { get; init; }

    public static BankHistoryEntry FromTransferOut(TransferDTO transfer) => new()
    {
        Kind = BankHistoryEntryKind.TransferOut,
        Date = transfer.Date,
        CounterpartBank = transfer.DestinationBank,
        Amount = transfer.Amount,
        Note = transfer.Note,
        Transfer = transfer,
    };

    public static BankHistoryEntry FromTransferIn(TransferDTO transfer) => new()
    {
        Kind = BankHistoryEntryKind.TransferIn,
        Date = transfer.Date,
        CounterpartBank = transfer.SourceBank,
        Amount = transfer.Amount,
        Note = transfer.Note,
        Transfer = transfer,
    };

    public static BankHistoryEntry FromAdjustment(BalanceAdjustmentDTO adjustment) => new()
    {
        Kind = BankHistoryEntryKind.Adjustment,
        Date = adjustment.Date,
        Delta = adjustment.Delta,
        Note = adjustment.Note,
        Adjustment = adjustment,
    };
}
