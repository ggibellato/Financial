using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface ISummaryService
{
    AggregatedSummaryDTO GetBrokerSummary(string brokerName);
    AggregatedSummaryDTO GetPortfolioSummary(string brokerName, string portfolioName);
}
