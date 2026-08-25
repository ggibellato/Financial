using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.Services;

internal static class NavigationMapper
{
    internal static TreeNodeDTO BuildBrokerTreeNode(BrokerNodeDTO broker)
    {
        var brokerNode = new TreeNodeDTO
        {
            NodeType = TreeNodeType.Broker,
            DisplayName = $"{broker.Name} ({broker.Currency})",
            Metadata = new Dictionary<string, object>
            {
                [NavigationMetadataKeys.BrokerName] = broker.Name,
                [NavigationMetadataKeys.Currency] = broker.Currency,
                [NavigationMetadataKeys.PortfolioCount] = broker.PortfolioCount,
                [NavigationMetadataKeys.TotalAssets] = broker.TotalAssets
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
            NodeType = TreeNodeType.Portfolio,
            DisplayName = $"{portfolio.Name} ({portfolio.AssetCount} assets)",
            Metadata = new Dictionary<string, object>
            {
                [NavigationMetadataKeys.PortfolioName] = portfolio.Name,
                [NavigationMetadataKeys.AssetCount] = portfolio.AssetCount
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
            NodeType = TreeNodeType.Asset,
            DisplayName = asset.Name,
            Metadata = new Dictionary<string, object>
            {
                [NavigationMetadataKeys.AssetName] = asset.Name,
                [NavigationMetadataKeys.Ticker] = asset.Ticker,
                [NavigationMetadataKeys.Exchange] = asset.Exchange,
                [NavigationMetadataKeys.ISIN] = asset.ISIN,
                [NavigationMetadataKeys.Country] = asset.Country,
                [NavigationMetadataKeys.LocalTypeCode] = asset.LocalTypeCode,
                [NavigationMetadataKeys.GlobalAssetClass] = asset.Class,
                [NavigationMetadataKeys.Quantity] = asset.Quantity,
                [NavigationMetadataKeys.AveragePrice] = asset.AveragePrice,
                [NavigationMetadataKeys.PositionType] = asset.PositionType.ToString(),
                [NavigationMetadataKeys.TransactionCount] = asset.TransactionCount,
                [NavigationMetadataKeys.CreditCount] = asset.CreditCount
            }
        };
    }

    internal static BrokerNodeDTO MapBroker(Broker broker, InvestmentScope scope)
    {
        var portfolios = MapPortfolios(broker.Portfolios, scope).ToList();
        return new BrokerNodeDTO
        {
            Name = broker.Name,
            Currency = broker.Currency,
            PortfolioCount = portfolios.Count,
            TotalAssets = portfolios.Sum(p => p.AssetCount),
            Portfolios = portfolios
        };
    }

    internal static TransactionDTO MapTransaction(Transaction transaction)
    {
        return new TransactionDTO
        {
            Id = transaction.Id,
            Date = transaction.Date,
            Type = transaction.Type.ToString(),
            Quantity = transaction.Quantity,
            UnitPrice = transaction.UnitPrice,
            Fees = transaction.Fees,
            TotalPrice = transaction.TotalPrice
        };
    }

    internal static TransactionSummaryItemDTO MapTransactionSummaryItem(Asset asset, Transaction transaction)
    {
        return new TransactionSummaryItemDTO
        {
            AssetName = asset.Name,
            Date = transaction.Date,
            Type = transaction.Type.ToString(),
            TotalPrice = transaction.TotalPrice
        };
    }

    internal static CreditDTO MapCredit(Credit credit)
    {
        return new CreditDTO
        {
            Id = credit.Id,
            Date = credit.Date,
            Type = credit.Type.ToString(),
            Value = credit.Value
        };
    }

    internal static AssetPriceSnapshotDTO MapPriceEntry(AssetPriceSnapshot entry)
    {
        return new AssetPriceSnapshotDTO
        {
            Date = entry.Date,
            Price = entry.Price,
            IsManual = entry.IsManual
        };
    }

    internal static PositionType PositionTypeFor(Asset asset, InvestmentScope scope) =>
        scope == InvestmentScope.Historic ? PositionType.Flat : asset.PositionType;

    private static IEnumerable<PortfolioNodeDTO> MapPortfolios(IEnumerable<Portfolio> portfolios, InvestmentScope scope)
    {
        return portfolios.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).Select(portfolio => MapPortfolio(portfolio, scope));
    }

    private static PortfolioNodeDTO MapPortfolio(Portfolio portfolio, InvestmentScope scope)
    {
        var assets = portfolio.Assets
            .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(asset => MapAsset(asset, scope))
            .ToList();

        return new PortfolioNodeDTO
        {
            Name = portfolio.Name,
            AssetCount = assets.Count,
            Assets = assets
        };
    }

    internal static AssetNodeDTO MapAsset(Asset asset, InvestmentScope scope)
    {
        return new AssetNodeDTO
        {
            Name = asset.Name,
            Ticker = asset.Ticker,
            Exchange = asset.Exchange,
            ISIN = asset.ISIN,
            Country = asset.Country,
            LocalTypeCode = asset.LocalTypeCode,
            Class = asset.Class,
            Quantity = asset.Quantity,
            AveragePrice = asset.AveragePrice,
            PositionType = PositionTypeFor(asset, scope),
            TransactionCount = asset.Transactions.Count,
            CreditCount = asset.Credits.Count
        };
    }
}
