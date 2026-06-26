using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface ISummaryQueryService
{
    AggregatedSummaryDTO GetBrokerSummary(string brokerName);
    AggregatedSummaryDTO GetPortfolioSummary(string brokerName, string portfolioName);
}
