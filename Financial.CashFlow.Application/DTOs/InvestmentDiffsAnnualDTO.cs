namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model wrapping every investment account's annual diffs plus the combined net-position row.
/// </summary>
public sealed class InvestmentDiffsAnnualDTO
{
    public required InvestmentAccountAnnualDiffDTO[] Accounts { get; init; }
    public required NetPositionAnnualDiffDTO NetPosition { get; init; }
}
