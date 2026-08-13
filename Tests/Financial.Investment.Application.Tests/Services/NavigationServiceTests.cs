using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Services;
using Financial.TestUtilities;
using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Application.Tests.Services;

/// <summary>
/// Tests that NavigationService correctly maps asset metadata so the WPF filter can
/// use type-pattern matching on GlobalAssetClass values stored in TreeNodeDTO.Metadata.
/// </summary>
public class NavigationServiceTests
{
    private readonly StubRepository _repository = new();
    private NavigationService CreateService() => new(_repository);

    [Theory]
    [InlineData(GlobalAssetClass.Equity)]
    [InlineData(GlobalAssetClass.RealEstate)]
    [InlineData(GlobalAssetClass.Bond)]
    [InlineData(GlobalAssetClass.ETF)]
    [InlineData(GlobalAssetClass.Fund)]
    [InlineData(GlobalAssetClass.Unknown)]
    public void GetNavigationTree_AssetNode_GlobalAssetClassMetadata_IsGlobalAssetClassTypeAndMatchesAssetClass(GlobalAssetClass assetClass)
    {
        _repository.Broker = BuildBrokerWithAsset("ASSET1", assetClass);

        var tree = CreateService().GetNavigationTree();

        var assetNode = GetFirstAssetNode(tree);
        assetNode.Metadata.Should().ContainKey("GlobalAssetClass");
        assetNode.Metadata["GlobalAssetClass"].Should().BeOfType<GlobalAssetClass>();
        assetNode.Metadata["GlobalAssetClass"].Should().Be(assetClass);
    }

    [Fact]
    public void GetNavigationTree_MultipleAssetsWithDifferentClasses_MetadataReflectsEachClass()
    {
        _repository.Broker = BuildBrokerWithAssets(
            ("EQ1", GlobalAssetClass.Equity),
            ("RE1", GlobalAssetClass.RealEstate));

        var tree = CreateService().GetNavigationTree();

        var assetNodes = GetAllAssetNodes(tree).ToList();
        assetNodes.Should().HaveCount(2);

        var equityNode = assetNodes.Single(n => n.DisplayName == "EQ1");
        var reitNode = assetNodes.Single(n => n.DisplayName == "RE1");

        equityNode.Metadata["GlobalAssetClass"].Should().Be(GlobalAssetClass.Equity);
        reitNode.Metadata["GlobalAssetClass"].Should().Be(GlobalAssetClass.RealEstate);
    }

    [Fact]
    public void GetNavigationTree_AssetNode_MetadataIncludesPositionType()
    {
        _repository.Broker = BuildBrokerWithAsset("LONG1", GlobalAssetClass.Equity, quantity: 10m);

        var tree = CreateService().GetNavigationTree();

        var assetNode = GetFirstAssetNode(tree);
        assetNode.Metadata["PositionType"].Should().Be("Long");
    }

    [Theory]
    [InlineData(10, PositionType.Long)]
    [InlineData(0, PositionType.Flat)]
    [InlineData(-10, PositionType.Short)]
    public void GetAssetsByBrokerPortfolio_AssetNodeDto_PositionTypeMatchesAssetPositionType(decimal quantity, PositionType expectedPositionType)
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        portfolio.AddAsset(BuildAssetWithQuantity("ASSET1", quantity));
        _repository.Broker = broker;

        var assets = CreateService().GetAssetsByBrokerPortfolio("Broker", "Portfolio").ToList();

        assets.Should().ContainSingle().Which.PositionType.Should().Be(expectedPositionType);
    }

    [Theory]
    [InlineData(10, PositionType.Long)]
    [InlineData(0, PositionType.Flat)]
    [InlineData(-10, PositionType.Short)]
    public void GetAssetDetails_ReturnsPositionTypeMatchingAsset(decimal quantity, PositionType expectedPositionType)
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        portfolio.AddAsset(BuildAssetWithQuantity("ASSET1", quantity));
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details.Should().NotBeNull();
        details!.PositionType.Should().Be(expectedPositionType);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(0)]
    [InlineData(-10)]
    public void GetNavigationTree_HistoricScope_AssetNode_PositionTypeIsFlat(decimal quantity)
    {
        _repository.Broker = BuildBrokerWithAsset("ASSET1", GlobalAssetClass.Equity, quantity);

        var tree = CreateService().GetNavigationTree(InvestmentScope.Historic);

        var assetNode = GetFirstAssetNode(tree);
        assetNode.Metadata["PositionType"].Should().Be("Flat");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(0)]
    [InlineData(-10)]
    public void GetAssetDetails_HistoricScope_PositionTypeIsFlat(decimal quantity)
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        portfolio.AddAsset(BuildAssetWithQuantity("ASSET1", quantity));
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1", InvestmentScope.Historic);

        details.Should().NotBeNull();
        details!.PositionType.Should().Be(PositionType.Flat);
    }

    [Fact]
    public void GetAssetDetails_ComputesRealizedGainLossFromWeightedAverageCostBasisReplay()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = Asset.Create("ASSET1", "ISIN", "BVMF", "ASSET1", CountryCode.BR, "FII", GlobalAssetClass.Equity);
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 5, 1), Transaction.TransactionType.Buy, 15m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2021, 6, 1), Credit.CreditType.Dividend, 12m));
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        // Weighted-average cost after both buys is 100; capital gain = 550 - (5 x 100) = 50; plus 12 credits = 62
        details.Should().NotBeNull();
        details!.RealizedGainLoss.Should().Be(62m);
    }

    [Fact]
    public void GetAssetDetails_WithNoSales_RealizedGainLossEqualsCreditsOnly()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = Asset.Create("ASSET1", "ISIN", "BVMF", "ASSET1", CountryCode.BR, "FII", GlobalAssetClass.Equity);
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2021, 6, 1), Credit.CreditType.Dividend, 8m));
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details.Should().NotBeNull();
        details!.RealizedGainLoss.Should().Be(8m);
    }

    [Fact]
    public void GetAssetDetails_WithSales_ComputesWeightedAverageSellPrice()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = Asset.Create("ASSET1", "ISIN", "BVMF", "ASSET1", CountryCode.BR, "FII", GlobalAssetClass.Equity);
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 20m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2022, 6, 1), Transaction.TransactionType.Sell, 5m, 120m, 0m));
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        // Weighted average = (5 x 110 + 5 x 120) / 10 = 115
        details.Should().NotBeNull();
        details!.AverageSellPrice.Should().Be(115m);
    }

    [Fact]
    public void GetAssetDetails_WithNoSales_AverageSellPriceIsNull()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = Asset.Create("ASSET1", "ISIN", "BVMF", "ASSET1", CountryCode.BR, "FII", GlobalAssetClass.Equity);
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details.Should().NotBeNull();
        details!.AverageSellPrice.Should().BeNull();
    }

    [Fact]
    public void GetBrokers_ShouldOrderByNameAlphabetically()
    {
        _repository.Brokers = new[]
        {
            BuildBroker("Zeta"),
            BuildBroker("Encerradas"),
            BuildBroker("Alpha")
        };

        var brokerNames = CreateService().GetBrokers().Select(broker => broker.Name).ToList();

        brokerNames.Should().ContainInOrder("Alpha", "Encerradas", "Zeta");
    }

    [Fact]
    public void GetBrokers_PortfoliosShouldOrderByNameAlphabetically()
    {
        _repository.Broker = BuildBroker("Broker", "USD",
            ("Zeta", new[] { "B" }),
            ("Encerradas", new[] { "C" }),
            ("Alpha", new[] { "A" }));

        var portfolioNames = CreateService().GetBrokers().Single().Portfolios.Select(portfolio => portfolio.Name).ToList();

        portfolioNames.Should().ContainInOrder("Alpha", "Encerradas", "Zeta");
    }

    [Fact]
    public void GetBrokers_AssetsShouldOrderByNameAlphabetically()
    {
        _repository.Broker = BuildBroker("Broker", "USD",
            ("Portfolio", new[] { "Zeta", "Encerradas", "Alpha" }));

        var assetNames = CreateService().GetBrokers()
            .Single()
            .Portfolios.Single()
            .Assets.Select(asset => asset.Name)
            .ToList();

        assetNames.Should().ContainInOrder("Alpha", "Encerradas", "Zeta");
    }

    private static Broker BuildBroker(string name, string currency = "USD",
        params (string PortfolioName, string[] AssetNames)[] portfolios)
    {
        var broker = Broker.Create(name, currency);

        foreach (var (portfolioName, assetNames) in portfolios)
        {
            var portfolio = broker.AddPortfolio(portfolioName);
            foreach (var assetName in assetNames)
            {
                portfolio.AddAsset(Asset.Create(assetName, "ISIN", "EX", "TICKER"));
            }
        }

        return broker;
    }

    private static Asset BuildAssetWithQuantity(string name, decimal quantity)
    {
        var asset = Asset.Create(name, "ISIN", "BVMF", name, CountryCode.BR, "FII", GlobalAssetClass.Equity);
        if (quantity > 0)
        {
            asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, quantity, 10m, 0m));
        }
        else if (quantity < 0)
        {
            asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, -quantity, 10m, 0m));
        }
        return asset;
    }

    private static TreeNodeDTO GetFirstAssetNode(TreeNodeDTO tree) =>
        tree.Children
            .SelectMany(broker => broker.Children)
            .SelectMany(portfolio => portfolio.Children)
            .First();

    private static IEnumerable<TreeNodeDTO> GetAllAssetNodes(TreeNodeDTO tree) =>
        tree.Children
            .SelectMany(broker => broker.Children)
            .SelectMany(portfolio => portfolio.Children);

    private static Broker BuildBrokerWithAsset(string assetName, GlobalAssetClass assetClass, decimal quantity = 0m)
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = Asset.Create(assetName, "ISIN", "BVMF", "T1", CountryCode.BR, "FII", assetClass);
        if (quantity > 0)
        {
            asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, quantity, 10m, 0m));
        }
        else if (quantity < 0)
        {
            asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, -quantity, 10m, 0m));
        }
        portfolio.AddAsset(asset);
        return broker;
    }

    private static Broker BuildBrokerWithAssets(params (string Name, GlobalAssetClass Class)[] assets)
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var index = 0;
        foreach (var (name, assetClass) in assets)
        {
            portfolio.AddAsset(Asset.Create(name, $"ISIN{index++}", "BVMF", name, CountryCode.BR, "FII", assetClass));
        }
        return broker;
    }

}
