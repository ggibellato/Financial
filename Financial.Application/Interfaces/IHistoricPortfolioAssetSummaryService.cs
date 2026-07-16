using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface IHistoricPortfolioAssetSummaryService
{
    IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName);
}
