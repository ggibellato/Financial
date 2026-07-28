namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for a single investment account's annual values and month-over-month diffs.
/// MonthlyValues index 0 = January. MonthlyDiffs index 0 = January minus the prior year's
/// December value (0 if the account didn't exist yet that prior year, null if no prior-year
/// data exists at all); indexes 1-11 = each month minus the previous month within the year.
/// </summary>
public sealed class InvestmentAccountAnnualDiffDTO
{
    public required string Account { get; init; }
    public required bool IsLiability { get; init; }
    public required decimal[] MonthlyValues { get; init; }
    public required decimal?[] MonthlyDiffs { get; init; }
}
