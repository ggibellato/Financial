namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for the combined net position (non-liability minus liability accounts) across a year.
/// MonthlyValues index 0 = January. MonthlyDiffs index 0 = January minus the prior year's
/// December net position (0 for an account not yet open that prior year, null if no prior-year
/// data exists at all); indexes 1-11 = each month minus the previous month within the year.
/// AverageMonthResult and SumOfMonthResults are computed over January through the year's last
/// relevant month - December for a past year, or the current calendar month for the current
/// year, so months that haven't happened yet don't drag the figures toward zero.
/// </summary>
public sealed class NetPositionAnnualDiffDTO
{
    public required decimal[] MonthlyValues { get; init; }
    public required decimal?[] MonthlyDiffs { get; init; }
    public required decimal FullYearNetChange { get; init; }
    public required decimal AverageMonthResult { get; init; }
    public required decimal SumOfMonthResults { get; init; }
}
