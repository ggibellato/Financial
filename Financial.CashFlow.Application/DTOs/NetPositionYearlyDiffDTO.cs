namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model for the combined net position (non-liability minus liability accounts) across a year.
/// MonthlyValues index 0 = January. MonthlyDiffs index 0 = February minus January.
/// </summary>
public sealed class NetPositionYearlyDiffDTO
{
    public required decimal[] MonthlyValues { get; init; }
    public required decimal[] MonthlyDiffs { get; init; }
    public required decimal FullYearNetChange { get; init; }
}
