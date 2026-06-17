using Financial.Application.DTOs;
using System.Collections.Generic;

namespace Financial.Application.Interfaces;

public interface ICreditQueryService
{
    IReadOnlyList<CreditDTO> GetCreditsByBroker(string brokerName);
    IReadOnlyList<CreditDTO> GetCreditsByPortfolio(string brokerName, string portfolioName);
}
