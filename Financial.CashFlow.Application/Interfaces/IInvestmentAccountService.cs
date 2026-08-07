using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IInvestmentAccountService
{
    IReadOnlyList<InvestmentAccountDTO> GetInvestmentAccounts();
}
