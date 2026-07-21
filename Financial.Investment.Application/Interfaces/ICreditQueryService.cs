using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

public interface ICreditQueryService
{
    IReadOnlyList<CreditDTO> GetCreditsByBroker(string brokerName);
    IReadOnlyList<CreditDTO> GetCreditsByPortfolio(string brokerName, string portfolioName);
}
