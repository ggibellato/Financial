using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface ICreditQueryService
{
    IReadOnlyList<CreditDTO> GetCreditsByBroker(string brokerName);
    IReadOnlyList<CreditDTO> GetCreditsByPortfolio(string brokerName, string portfolioName);
}
