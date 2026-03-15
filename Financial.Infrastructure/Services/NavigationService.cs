using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;

namespace Financial.Infrastructure.Services;

/// <summary>
/// Implementation of INavigationService that provides hierarchical navigation
/// over financial data using the repository pattern
/// </summary>
public class NavigationService : INavigationService
{
    private const string EncerradasName = "Encerradas";
    private readonly IRepository _repository;

    public NavigationService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Builds the complete navigation tree from the root
    /// </summary>
    public TreeNodeDTO GetNavigationTree()
    {
        var brokers = GetBrokers().ToList();

        var rootNode = new TreeNodeDTO
        {
            NodeType = "Investments",
            DisplayName = "All Investments"
        };

        foreach (var broker in brokers)
        {
            rootNode.Children.Add(BuildBrokerTreeNode(broker));
        }

        return rootNode;
    }

    /// <summary>
    /// Gets detailed information for a specific asset
    /// </summary>
    public AssetDetailsDTO? GetAssetDetails(string brokerName, string portfolioName, string assetName)
    {
        if (string.IsNullOrWhiteSpace(brokerName) ||
            string.IsNullOrWhiteSpace(portfolioName) ||
            string.IsNullOrWhiteSpace(assetName))
        {
            return null;
        }

        var assets = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName);
        var asset = assets.FirstOrDefault(a => a.Name == assetName);

        if (asset == null)
        {
            return null;
        }

        var operations = asset.Operations
            .Select(MapOperation)
            .OrderByDescending(op => op.Date)
            .ToList();

        var credits = asset.Credits
            .Select(MapCredit)
            .OrderByDescending(c => c.Date)
            .ToList();

        var (totalBought, totalSold, totalCredits) = CalculateTotals(asset);

        return new AssetDetailsDTO
        {
            Name = asset.Name,
            BrokerName = brokerName,
            PortfolioName = portfolioName,
            Ticker = asset.Ticker,
            ISIN = asset.ISIN,
            Exchange = asset.Exchange,
            Quantity = asset.Quantity,
            AveragePrice = asset.AvargePrice,
            IsActive = asset.Active,
            TotalBought = totalBought,
            TotalSold = totalSold,
            TotalCredits = totalCredits,
            Operations = operations,
            Credits = credits
        };
    }

    /// <summary>
    /// Gets a list of all brokers with their portfolios and assets
    /// </summary>
    public IEnumerable<BrokerNodeDTO> GetBrokers()
    {
        var brokers = OrderByNameWithEncerradasLast(_repository.GetBrokerList(), broker => broker.Name);

        return brokers.Select(MapBroker).ToList();
    }

    private static TreeNodeDTO BuildBrokerTreeNode(BrokerNodeDTO broker)
    {
        var brokerNode = new TreeNodeDTO
        {
            NodeType = "Broker",
            DisplayName = $"{broker.Name} ({broker.Currency})",
            Metadata = new Dictionary<string, object>
            {
                ["BrokerName"] = broker.Name,
                ["Currency"] = broker.Currency,
                ["PortfolioCount"] = broker.PortfolioCount,
                ["TotalAssets"] = broker.TotalAssets
            }
        };

        foreach (var portfolio in broker.Portfolios)
        {
            brokerNode.Children.Add(BuildPortfolioTreeNode(portfolio));
        }

        return brokerNode;
    }

    private static TreeNodeDTO BuildPortfolioTreeNode(PortfolioNodeDTO portfolio)
    {
        var portfolioNode = new TreeNodeDTO
        {
            NodeType = "Portfolio",
            DisplayName = $"{portfolio.Name} ({portfolio.AssetCount} assets)",
            Metadata = new Dictionary<string, object>
            {
                ["PortfolioName"] = portfolio.Name,
                ["AssetCount"] = portfolio.AssetCount,
                ["ActiveAssetCount"] = portfolio.ActiveAssetCount
            }
        };

        foreach (var asset in portfolio.Assets)
        {
            portfolioNode.Children.Add(BuildAssetTreeNode(asset));
        }

        return portfolioNode;
    }

    private static TreeNodeDTO BuildAssetTreeNode(AssetNodeDTO asset)
    {
        return new TreeNodeDTO
        {
            NodeType = "Asset",
            DisplayName = asset.Name,
            Metadata = new Dictionary<string, object>
            {
                ["AssetName"] = asset.Name,
                ["Ticker"] = asset.Ticker,
                ["Exchange"] = asset.Exchange,
                ["ISIN"] = asset.ISIN,
                ["Quantity"] = asset.Quantity,
                ["AveragePrice"] = asset.AveragePrice,
                ["IsActive"] = asset.IsActive,
                ["OperationCount"] = asset.OperationCount,
                ["CreditCount"] = asset.CreditCount
            }
        };
    }

    private static BrokerNodeDTO MapBroker(Broker broker)
    {
        return new BrokerNodeDTO
        {
            Name = broker.Name,
            Currency = broker.Currency,
            PortfolioCount = broker.Portfolios.Count,
            TotalAssets = broker.Portfolios.Sum(p => p.Assets.Count),
            Portfolios = MapPortfolios(broker.Portfolios).ToList()
        };
    }

    private static IEnumerable<PortfolioNodeDTO> MapPortfolios(IEnumerable<Portfolio> portfolios)
    {
        return OrderByNameWithEncerradasLast(portfolios, portfolio => portfolio.Name)
            .Select(MapPortfolio);
    }

    private static PortfolioNodeDTO MapPortfolio(Portfolio portfolio)
    {
        return new PortfolioNodeDTO
        {
            Name = portfolio.Name,
            AssetCount = portfolio.Assets.Count,
            ActiveAssetCount = portfolio.Assets.Count(a => a.Active),
            Assets = MapAssets(portfolio.Assets).ToList()
        };
    }

    private static IEnumerable<AssetNodeDTO> MapAssets(IEnumerable<Asset> assets)
    {
        return OrderByNameWithEncerradasLast(assets, asset => asset.Name)
            .Select(MapAsset);
    }

    private static AssetNodeDTO MapAsset(Asset asset)
    {
        return new AssetNodeDTO
        {
            Name = asset.Name,
            Ticker = asset.Ticker,
            Exchange = asset.Exchange,
            ISIN = asset.ISIN,
            Quantity = asset.Quantity,
            AveragePrice = asset.AvargePrice,
            IsActive = asset.Active,
            OperationCount = asset.Operations.Count,
            CreditCount = asset.Credits.Count
        };
    }

    private static OperationDTO MapOperation(Operation operation)
    {
        return new OperationDTO
        {
            Id = operation.Id,
            Date = operation.Date,
            Type = operation.Type.ToString(),
            Quantity = operation.Quantity,
            UnitPrice = operation.UnitPrice,
            Fees = operation.Fees,
            TotalPrice = operation.TotalPrice
        };
    }

    private static CreditDTO MapCredit(Credit credit)
    {
        return new CreditDTO
        {
            Id = credit.Id,
            Date = credit.Date,
            Type = credit.Type.ToString(),
            Value = credit.Value
        };
    }

    private static (decimal TotalBought, decimal TotalSold, decimal TotalCredits) CalculateTotals(Asset asset)
    {
        var totalBought = asset.Operations
            .Where(op => op.Type == Operation.OperationType.Buy)
            .Sum(op => op.TotalPrice);

        var totalSold = asset.Operations
            .Where(op => op.Type == Operation.OperationType.Sell)
            .Sum(op => op.TotalPrice);

        var totalCredits = asset.Credits.Sum(c => c.Value);

        return (totalBought, totalSold, totalCredits);
    }

    private static IEnumerable<T> OrderByNameWithEncerradasLast<T>(IEnumerable<T> source, Func<T, string> nameSelector)
    {
        return source
            .OrderBy(item => IsEncerradas(nameSelector(item)) ? 1 : 0)
            .ThenBy(item => nameSelector(item) ?? string.Empty, StringComparer.CurrentCultureIgnoreCase);
    }

    private static bool IsEncerradas(string? name)
    {
        return string.Equals(name?.Trim(), EncerradasName, StringComparison.CurrentCultureIgnoreCase);
    }
}

