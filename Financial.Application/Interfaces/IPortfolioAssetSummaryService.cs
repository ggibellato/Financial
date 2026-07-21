using Financial.Application.DTOs;
using Financial.Application.Enums;

namespace Financial.Application.Interfaces;

public interface IPortfolioAssetSummaryService
{
    IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active);
}
