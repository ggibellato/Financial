namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Request to create a new balance adjustment. The bank comes from the route; the server generates the identifier.
/// </summary>
public sealed class BalanceAdjustmentCreateDTO
{
    public required DateOnly Date { get; init; }

    /// <summary>The real balance from the bank statement. Must be greater than or equal to zero.</summary>
    public required decimal TargetBalance { get; init; }

    public string? Note { get; init; }
}
