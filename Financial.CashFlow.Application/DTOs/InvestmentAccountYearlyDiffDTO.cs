namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a single investment account's yearly values and month-over-month diffs.
/// MonthlyValues index 0 = January. MonthlyDiffs index 0 = February minus January.
/// </summary>
public sealed class InvestmentAccountYearlyDiffDTO
{
    public required string Account { get; init; }
    public required bool IsLiability { get; init; }
    public required decimal[] MonthlyValues { get; init; }
    public required decimal[] MonthlyDiffs { get; init; }
}
