using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;

namespace Financial.Investment.Application.Interfaces;

public interface ICreditQueryService
{
    IReadOnlyList<CreditDTO> GetCreditsByBroker(string brokerName, InvestmentScope scope = InvestmentScope.Active);
    IReadOnlyList<CreditDTO> GetCreditsByPortfolio(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active);
}
