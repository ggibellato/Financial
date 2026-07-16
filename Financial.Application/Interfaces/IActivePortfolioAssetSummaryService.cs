using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface IActivePortfolioAssetSummaryService
{
    IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName);
}
