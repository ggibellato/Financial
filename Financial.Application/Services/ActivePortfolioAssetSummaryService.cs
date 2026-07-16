using Financial.Application.DTOs;
using Financial.Application.Interfaces;

namespace Financial.Application.Services;

public sealed class ActivePortfolioAssetSummaryService : IActivePortfolioAssetSummaryService
{
    private readonly IRepository _repository;

    public ActivePortfolioAssetSummaryService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName)
    {
        if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            return [];

        var assets = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName, InvestmentScope.Active).ToList();
        if (assets.Count == 0)
            return [];

        return PortfolioAssetSummaryBuilder.Build(assets, DateTime.Today, CalculateNetInvested, NoRealizedGainLoss);
    }

    private static decimal CalculateNetInvested(AssetTotals totals) => totals.TotalBought - totals.TotalSold;

    private static decimal? NoRealizedGainLoss(AssetTotals totals) => null;
}
