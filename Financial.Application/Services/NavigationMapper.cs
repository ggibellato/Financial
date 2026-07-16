using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;

namespace Financial.Application.Services;

internal static class NavigationMapper
{
    private const string EncerradasName = "Encerradas";

    internal static TreeNodeDTO BuildBrokerTreeNode(BrokerNodeDTO broker)
    {
        var brokerNode = new TreeNodeDTO
        {
            NodeType = TreeNodeType.Broker,
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
            NodeType = TreeNodeType.Portfolio,
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
            NodeType = TreeNodeType.Asset,
            DisplayName = asset.Name,
            Metadata = new Dictionary<string, object>
            {
                ["AssetName"] = asset.Name,
                ["Ticker"] = asset.Ticker,
                ["Exchange"] = asset.Exchange,
                ["ISIN"] = asset.ISIN,
                ["Country"] = asset.Country,
                ["LocalTypeCode"] = asset.LocalTypeCode,
                ["GlobalAssetClass"] = asset.Class,
                ["Quantity"] = asset.Quantity,
                ["AveragePrice"] = asset.AveragePrice,
                ["IsActive"] = asset.IsActive,
                ["PositionType"] = asset.PositionType,
                ["TransactionCount"] = asset.TransactionCount,
                ["CreditCount"] = asset.CreditCount
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

    internal static (decimal TotalBought, decimal TotalSold, decimal TotalCredits) CalculateTotals(Asset asset)
    {
        decimal totalBought = 0, totalSold = 0;
        foreach (var t in asset.Transactions)
        {
            if (t.Type == Transaction.TransactionType.Buy)
                totalBought += t.TotalPrice;
            else
                totalSold += t.TotalPrice;
        }

        var totalCredits = asset.Credits.Sum(c => c.Value);

        return (totalBought, totalSold, totalCredits);
    }

    private static IEnumerable<PortfolioNodeDTO> MapPortfolios(IEnumerable<Portfolio> portfolios, InvestmentScope scope)
    {
        return portfolios.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).Select(portfolio => MapPortfolio(portfolio, scope));
    }

    private static PortfolioNodeDTO MapPortfolio(Portfolio portfolio, InvestmentScope scope)
    {
        return new PortfolioNodeDTO
        {
            Name = portfolio.Name,
            AssetCount = portfolio.Assets.Count,
            ActiveAssetCount = portfolio.Assets.Count(a => a.Active),
            Assets = portfolio.Assets.OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase).Select(asset => MapAsset(asset, scope)).ToList()
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
            IsActive = asset.Active,
            PositionType = scope == InvestmentScope.Historic ? PositionType.Flat : asset.PositionType,
            TransactionCount = asset.Transactions.Count,
            CreditCount = asset.Credits.Count
        };
    }

    internal static bool IsEncerradas(string? name) =>
        string.Equals(name?.Trim(), EncerradasName, StringComparison.CurrentCultureIgnoreCase);
}
