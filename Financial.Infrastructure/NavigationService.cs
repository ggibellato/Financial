using Financial.Application.DTO;
using Financial.Model;
using FinancialModel.Application;

namespace FinancialModel.Infrastructure;

/// <summary>
/// Implementation of INavigationService that provides hierarchical navigation
/// over financial data using the repository pattern
/// </summary>
public class NavigationService : INavigationService
{
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
                    var assetNode = new TreeNodeDTO
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

                    portfolioNode.Children.Add(assetNode);
                }

                brokerNode.Children.Add(portfolioNode);
            }

            rootNode.Children.Add(brokerNode);
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

        var assets = _repository.GetAssetsByBrokerPortifolio(brokerName, portfolioName);
        var asset = assets.FirstOrDefault(a => a.Name == assetName);

        if (asset == null)
        {
            return null;
        }

        // Map operations
        var operations = asset.Operations.Select(op => new OperationDTO
        {
            Date = op.Date,
            Type = op.Type.ToString(),
            Quantity = op.Quantity,
            UnitPrice = op.UnitPrice,
            Fees = op.Fees,
            TotalPrice = op.TotalPrice
        }).OrderByDescending(op => op.Date).ToList();

        // Map credits
        var credits = asset.Credits.Select(c => new CreditDTO
        {
            Date = c.Date,
            Type = c.Type.ToString(),
            Value = c.Value
        }).OrderByDescending(c => c.Date).ToList();

        // Calculate totals
        var totalBought = asset.Operations
            .Where(op => op.Type == Operation.OperationType.Buy)
            .Sum(op => op.TotalPrice);

        var totalSold = asset.Operations
            .Where(op => op.Type == Operation.OperationType.Sell)
            .Sum(op => op.TotalPrice);

        var totalCredits = asset.Credits.Sum(c => c.Value);

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
        var brokers = _repository.GetBrokerList();

        return brokers.Select(broker => new BrokerNodeDTO
        {
            Name = broker.Name,
            Currency = broker.Currency,
            PortfolioCount = broker.Portifolios.Count,
            TotalAssets = broker.Portifolios.Sum(p => p.Assets.Count),
            Portfolios = broker.Portifolios.Select(portfolio => new PortfolioNodeDTO
            {
                Name = portfolio.Name,
                AssetCount = portfolio.Assets.Count,
                ActiveAssetCount = portfolio.Assets.Count(a => a.Active),
                Assets = portfolio.Assets.Select(asset => new AssetNodeDTO
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
                }).ToList()
            }).ToList()
        }).ToList();
    }
}
