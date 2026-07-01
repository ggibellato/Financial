using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;

namespace Financial.Application.Services;

public sealed class PortfolioAssetSummaryQueryService : IPortfolioAssetSummaryQueryService
{
    private readonly IRepository _repository;

    public PortfolioAssetSummaryQueryService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName)
    {
        if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            return [];

        var assets = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName).ToList();

        if (assets.Count == 0)
            return [];

        var computed = assets.Select(ComputeAssetData).ToList();
        var portfolioTotalInvested = computed.Sum(c => c.TotalInvested);

        return NavigationMapper
            .OrderByNameWithEncerradasLast(computed, c => c.AssetName)
            .Select(c => ToDTO(c, CalculateWeight(c.TotalInvested, portfolioTotalInvested)))
            .ToList();
    }

    private static AssetComputedData ComputeAssetData(Asset asset)
    {
        var (totalBought, totalSold, _) = NavigationMapper.CalculateTotals(asset);
        var firstBuyDate = asset.Transactions
            .Where(t => t.Type == Transaction.TransactionType.Buy)
            .Select(t => (DateTime?)t.Date)
            .DefaultIfEmpty(null)
            .Min();

        return new AssetComputedData(
            asset.Name, asset.Ticker, asset.Exchange,
            firstBuyDate, asset.Quantity,
            totalBought, totalSold, totalBought - totalSold);
    }

    private static PortfolioAssetSummaryItemDTO ToDTO(AssetComputedData c, decimal weight) =>
        new()
        {
            AssetName = c.AssetName,
            Ticker = c.Ticker,
            Exchange = c.Exchange,
            FirstInvestmentDate = c.FirstInvestmentDate,
            CurrentQuantity = c.CurrentQuantity,
            TotalBought = c.TotalBought,
            TotalSold = c.TotalSold,
            TotalInvested = c.TotalInvested,
            PortfolioWeight = weight
        };

    private static decimal CalculateWeight(decimal totalInvested, decimal portfolioTotalInvested) =>
        portfolioTotalInvested == 0m ? 0m : totalInvested / portfolioTotalInvested * 100m;

    private sealed record AssetComputedData(
        string AssetName, string Ticker, string Exchange,
        DateTime? FirstInvestmentDate, decimal CurrentQuantity,
        decimal TotalBought, decimal TotalSold, decimal TotalInvested);
}
