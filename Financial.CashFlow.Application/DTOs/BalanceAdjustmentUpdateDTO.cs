namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to update an existing balance adjustment's details. The id and bank come from the route.
/// </summary>
public sealed class BalanceAdjustmentUpdateDTO
{
    public required DateOnly Date { get; init; }

    /// <summary>The real balance from the bank statement. Must be greater than or equal to zero.</summary>
    public required decimal TargetBalance { get; init; }

    public string? Note { get; init; }
}
