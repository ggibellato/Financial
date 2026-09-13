using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;

namespace Financial.Investment.Application.Interfaces;

public interface ISummaryService
{
    Task<AggregatedSummaryDTO> GetBrokerSummaryAsync(string brokerName, InvestmentScope scope = InvestmentScope.Active);
    Task<AggregatedSummaryDTO> GetPortfolioSummaryAsync(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active);
}
