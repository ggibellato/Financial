using Financial.Application.DTOs;
using Financial.Application.Enums;

namespace Financial.Application.Interfaces;

public interface ISummaryService
{
    AggregatedSummaryDTO GetBrokerSummary(string brokerName, InvestmentScope scope = InvestmentScope.Active);
    AggregatedSummaryDTO GetPortfolioSummary(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active);
}
