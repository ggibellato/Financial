namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a single expense category's yearly totals. MonthlyTotals index 0 = January.
/// </summary>
public sealed class CategoryYearlyTotalDTO
{
    public required string Category { get; init; }
    public required decimal[] MonthlyTotals { get; init; }
    public required decimal YearlyTotal { get; init; }
}
