using Financial.Application.DTOs;
using Financial.Application.Interfaces;

namespace Financial.Application.Services;

public sealed class PortfolioAssetSummaryService : IPortfolioAssetSummaryService
{
    private readonly IActivePortfolioAssetSummaryService _activePortfolioAssetSummaryService;
    private readonly IHistoricPortfolioAssetSummaryService _historicPortfolioAssetSummaryService;

    public PortfolioAssetSummaryService(
        IActivePortfolioAssetSummaryService activePortfolioAssetSummaryService,
        IHistoricPortfolioAssetSummaryService historicPortfolioAssetSummaryService)
    {
        _activePortfolioAssetSummaryService = activePortfolioAssetSummaryService ?? throw new ArgumentNullException(nameof(activePortfolioAssetSummaryService));
        _historicPortfolioAssetSummaryService = historicPortfolioAssetSummaryService ?? throw new ArgumentNullException(nameof(historicPortfolioAssetSummaryService));
    }

    public IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active) =>
        scope == InvestmentScope.Historic
            ? _historicPortfolioAssetSummaryService.GetPortfolioAssetsSummary(brokerName, portfolioName)
            : _activePortfolioAssetSummaryService.GetPortfolioAssetsSummary(brokerName, portfolioName);
}
