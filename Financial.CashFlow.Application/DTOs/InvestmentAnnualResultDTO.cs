namespace Financial.CashFlow.Application.DTOs;

public sealed class InvestmentAnnualResultDTO
{
    public required InvestmentAccountAnnualDiffDTO[] Accounts { get; init; }
    public required NetPositionAnnualDiffDTO NetPosition { get; init; }
}
