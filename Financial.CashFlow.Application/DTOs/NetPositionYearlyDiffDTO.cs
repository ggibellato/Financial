namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for the combined net position (non-liability minus liability accounts) across a year.
/// MonthlyValues index 0 = January. MonthlyDiffs index 0 = January minus the prior year's
/// December net position (0 for an account not yet open that prior year, null if no prior-year
/// data exists at all); indexes 1-11 = each month minus the previous month within the year.
/// </summary>
public sealed class NetPositionYearlyDiffDTO
{
    public required decimal[] MonthlyValues { get; init; }
    public required decimal?[] MonthlyDiffs { get; init; }
    public required decimal FullYearNetChange { get; init; }
}
