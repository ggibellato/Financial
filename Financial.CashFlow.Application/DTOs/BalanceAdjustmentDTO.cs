namespace Financial.CashFlow.Application.DTOs;

public sealed class BalanceAdjustmentDTO
{
    public required Guid Id { get; init; }

    public required DateOnly Date { get; init; }

    public required Guid BankId { get; init; }

    public required string BankName { get; init; }

    /// <summary>The real balance entered, from the bank statement.</summary>
    public required decimal TargetBalance { get; init; }

    /// <summary>Computed correction: TargetBalance minus the balance as of Date, excluding this adjustment.</summary>
    public required decimal Delta { get; init; }

    public string? Note { get; init; }
}
