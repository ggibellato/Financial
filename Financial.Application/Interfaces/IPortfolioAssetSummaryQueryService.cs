using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface IPortfolioAssetSummaryQueryService
{
    IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName);
}
