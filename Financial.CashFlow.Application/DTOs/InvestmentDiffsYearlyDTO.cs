namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model wrapping every investment account's yearly diffs plus the combined net-position row.
/// </summary>
public sealed class InvestmentDiffsYearlyDTO
{
    public required InvestmentAccountYearlyDiffDTO[] Accounts { get; init; }
    public required NetPositionYearlyDiffDTO NetPosition { get; init; }
}
