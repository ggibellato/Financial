namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a tracked category.
/// </summary>
public sealed class CategoryDTO
{
    /// <summary>Category identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Category name (resolution key used by Expense.Category).</summary>
    public required string Name { get; init; }

    /// <summary>Whether this category currently accepts new expenses.</summary>
    public required bool Active { get; init; }

    /// <summary>Whether this category is the investment classification.</summary>
    public required bool IsInvestment { get; init; }

    /// <summary>Whether this category is the tithe classification.</summary>
    public required bool IsTithe { get; init; }
}
