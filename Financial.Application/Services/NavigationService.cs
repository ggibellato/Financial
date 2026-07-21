using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;

namespace Financial.Application.Services;

public sealed class NavigationService : INavigationService
{
    private readonly IRepository _repository;

    public NavigationService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public TreeNodeDTO GetNavigationTree(InvestmentScope scope = InvestmentScope.Active)
    {
        var brokers = GetBrokers(scope);

        var rootNode = new TreeNodeDTO
        {
            NodeType = TreeNodeType.Investments,
            DisplayName = "All Investments"
        };

        foreach (var broker in brokers)
        {
            rootNode.Children.Add(NavigationMapper.BuildBrokerTreeNode(broker));
        }

        return rootNode;
    }

    public AssetDetailsDTO? GetAssetDetails(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active)
    {
        if (string.IsNullOrWhiteSpace(brokerName) ||
            string.IsNullOrWhiteSpace(portfolioName) ||
            string.IsNullOrWhiteSpace(assetName))
        {
            return null;
        }

        var asset = _repository.GetAsset(brokerName, portfolioName, assetName, scope);

        if (asset == null)
        {
            return null;
        }

        var transactions = asset.Transactions
            .Select(NavigationMapper.MapTransaction)
            .OrderByDescending(t => t.Date)
            .ToList();

        var credits = asset.Credits
            .Select(NavigationMapper.MapCredit)
            .OrderByDescending(c => c.Date)
            .ToList();

        var (totalBought, totalSold, totalCredits) = NavigationMapper.CalculateTotals(asset);

        return new AssetDetailsDTO
        {
            Name = asset.Name,
            BrokerName = brokerName,
            PortfolioName = portfolioName,
            Ticker = asset.Ticker,
            ISIN = asset.ISIN,
            Exchange = asset.Exchange,
            Country = asset.Country,
            LocalTypeCode = asset.LocalTypeCode,
            Class = asset.Class,
            Quantity = asset.Quantity,
            AveragePrice = asset.AveragePrice,
            AverageSellPrice = NavigationMapper.CalculateAverageSellPrice(asset),
            PositionType = scope == InvestmentScope.Historic ? PositionType.Flat : asset.PositionType,
            TotalBought = totalBought,
            TotalSold = totalSold,
            TotalCredits = totalCredits,
            RealizedGainLoss = NavigationMapper.CalculateRealizedGainLoss(asset),
            Transactions = transactions,
            Credits = credits,
            CashFlowsWithCredits = AssetCashFlowBuilder.BuildWithCredits(asset),
            CashFlowsWithoutCredits = AssetCashFlowBuilder.BuildWithoutCredits(asset)
        };
    }

    public IEnumerable<BrokerNodeDTO> GetBrokers(InvestmentScope scope = InvestmentScope.Active)
    {
        var brokers = _repository.GetBrokerList(scope).OrderBy(b => b.Name, StringComparer.CurrentCultureIgnoreCase);
        return brokers.Select(broker => NavigationMapper.MapBroker(broker, scope)).ToList();
    }

    public IEnumerable<AssetNodeDTO> GetAssetsByBrokerPortfolio(string brokerName, string portfolioName)
    {
        return _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName)
            .Select(asset => NavigationMapper.MapAsset(asset, InvestmentScope.Active))
            .ToList();
    }

}
