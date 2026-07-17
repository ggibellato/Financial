using Financial.Application.DTOs;
using Financial.Application.Interfaces;

namespace Financial.Application.Services;

public sealed class PortfolioAssetSummaryService : IPortfolioAssetSummaryService
{
    private readonly IRepository _repository;

    public PortfolioAssetSummaryService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active)
    {
        if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            return [];

        var assets = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName, scope).ToList();
        if (assets.Count == 0)
            return [];

        return scope == InvestmentScope.Historic
            ? PortfolioAssetSummaryBuilder.Build(assets, DateTime.Today, CalculateGrossBought)
            : PortfolioAssetSummaryBuilder.Build(assets, DateTime.Today, CalculateNetInvested);
    }

    private static decimal CalculateNetInvested(AssetTotals totals) => totals.TotalBought - totals.TotalSold;

    private static decimal CalculateGrossBought(AssetTotals totals) => totals.TotalBought;
}
