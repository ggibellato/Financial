namespace Financial.CashFlow.Application.DTOs;

/// <summary>
/// Read model wrapping every investment account's annual results plus the combined net-position row.
/// Reuses the existing per-account/net-position nested types verbatim.
/// </summary>
public sealed class InvestmentAnnualResultDTO
{
    public required InvestmentAccountAnnualDiffDTO[] Accounts { get; init; }
    public required NetPositionAnnualDiffDTO NetPosition { get; init; }
}
