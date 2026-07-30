namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a single expense category's annual totals. MonthlyTotals index 0 = January.
/// </summary>
public sealed class CategoryAnnualTotalDTO
{
    public required string Category { get; init; }
    public required decimal[] MonthlyTotals { get; init; }
    public required decimal AnnualTotal { get; init; }
    public required decimal Average { get; init; }
}
