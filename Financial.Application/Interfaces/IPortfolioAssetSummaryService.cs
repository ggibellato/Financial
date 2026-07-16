using Financial.Application.DTOs;

namespace Financial.Application.Interfaces;

public interface IPortfolioAssetSummaryService
{
    IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active);
}
