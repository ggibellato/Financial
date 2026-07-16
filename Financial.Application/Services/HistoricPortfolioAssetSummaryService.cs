using Financial.Application.DTOs;
using Financial.Application.Interfaces;

namespace Financial.Application.Services;

public sealed class HistoricPortfolioAssetSummaryService : IHistoricPortfolioAssetSummaryService
{
    private readonly IRepository _repository;

    public HistoricPortfolioAssetSummaryService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName)
    {
        if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            return [];

        var assets = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName, InvestmentScope.Historic).ToList();
        if (assets.Count == 0)
            return [];

        return PortfolioAssetSummaryBuilder.Build(assets, DateTime.Today, CalculateGrossBought);
    }

    private static decimal CalculateGrossBought(AssetTotals totals) => totals.TotalBought;
}
