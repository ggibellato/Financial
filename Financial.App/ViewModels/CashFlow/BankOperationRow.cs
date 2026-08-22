using Financial.CashFlow.Application.DTOs;

namespace Financial.Presentation.App.ViewModels.CashFlow;

public enum BankOperationKind
{
    Transfer,
    Adjustment,
}

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

    public decimal DisplayAmount => Kind == BankOperationKind.Transfer ? Amount ?? 0m : Delta ?? 0m;

    public bool MatchesBank(string bankName) => Kind == BankOperationKind.Transfer
        ? SourceBank == bankName || DestinationBank == bankName
        : Bank == bankName;

    public static BankOperationRow FromTransfer(TransferDTO transfer) => new()
    {
        Kind = BankOperationKind.Transfer,
        Date = transfer.Date,
        BankLabel = $"{transfer.SourceBankName} → {transfer.DestinationBankName}",
        SourceBank = transfer.SourceBankName,
        DestinationBank = transfer.DestinationBankName,
        Amount = transfer.Amount,
        Note = transfer.Note,
        Transfer = transfer,
    };

    public static BankOperationRow FromAdjustment(BalanceAdjustmentDTO adjustment) => new()
    {
        Kind = BankOperationKind.Adjustment,
        Date = adjustment.Date,
        BankLabel = adjustment.BankName,
        Bank = adjustment.BankName,
        Delta = adjustment.Delta,
        Note = adjustment.Note,
        Adjustment = adjustment,
    };
}
