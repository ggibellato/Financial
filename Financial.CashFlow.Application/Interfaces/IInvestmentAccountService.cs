using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IInvestmentAccountService
{
    IReadOnlyList<InvestmentAccountDTO> GetInvestmentAccounts();
    Task<InvestmentAccountDTO> CreateInvestmentAccountAsync(InvestmentAccountCreateDTO request);
    Task<InvestmentAccountDTO> UpdateInvestmentAccountAsync(Guid id, InvestmentAccountUpdateDTO request);
    Task DeleteInvestmentAccountAsync(Guid id);
}
