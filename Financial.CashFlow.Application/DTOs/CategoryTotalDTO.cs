namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// A category's total expense value for a given month.
/// </summary>
public sealed class CategoryTotalDTO
{
    /// <summary>Expense category name.</summary>
    public required string Category { get; init; }

    /// <summary>Sum of that category's expense values for the month.</summary>
    public required decimal TotalValue { get; init; }
}
