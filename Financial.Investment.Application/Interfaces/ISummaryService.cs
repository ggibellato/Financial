using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;

namespace Financial.Investment.Application.Interfaces;

public interface ISummaryService
{
    AggregatedSummaryDTO GetBrokerSummary(string brokerName, InvestmentScope scope = InvestmentScope.Active);
    AggregatedSummaryDTO GetPortfolioSummary(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active);
}
