using Financial.Application.DTOs;
using Financial.Application.Interfaces;

namespace Financial.Application.Services;

public sealed class NavigationService : INavigationService
{
    private readonly IRepository _repository;

    public NavigationService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public TreeNodeDTO GetNavigationTree()
    {
        var brokers = GetBrokers().ToList();

        var rootNode = new TreeNodeDTO
        {
            NodeType = TreeNodeTypes.Investments,
            DisplayName = "All Investments"
        };

        foreach (var broker in brokers)
        {
            rootNode.Children.Add(NavigationMapper.BuildBrokerTreeNode(broker));
        }

        return rootNode;
    }

    public AssetDetailsDTO? GetAssetDetails(string brokerName, string portfolioName, string assetName)
    {
        if (string.IsNullOrWhiteSpace(brokerName) ||
            string.IsNullOrWhiteSpace(portfolioName) ||
            string.IsNullOrWhiteSpace(assetName))
        {
            return null;
        }

        var asset = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName)
            .FirstOrDefault(a => a.Name == assetName);

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
            IsActive = asset.Active,
            TotalBought = totalBought,
            TotalSold = totalSold,
            TotalCredits = totalCredits,
            Transactions = transactions,
            Credits = credits
        };
    }

    public IEnumerable<BrokerNodeDTO> GetBrokers()
    {
        var brokers = NavigationMapper.OrderByNameWithEncerradasLast(_repository.GetBrokerList(), b => b.Name);
        return brokers.Select(NavigationMapper.MapBroker).ToList();
    }

    public IEnumerable<AssetNodeDTO> GetAssetsByBrokerPortfolio(string brokerName, string portfolioName)
    {
        return _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName)
            .Select(NavigationMapper.MapAsset)
            .ToList();
    }

}
