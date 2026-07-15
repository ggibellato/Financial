using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Services;
using Financial.Domain.Entities;
using FluentAssertions;

namespace Financial.Application.Tests.Services;

/// <summary>
/// Tests that NavigationService correctly maps asset metadata so the WPF filter can
/// use type-pattern matching on GlobalAssetClass values stored in TreeNodeDTO.Metadata.
/// </summary>
public class NavigationMapperTests
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
        assetNode.Metadata["PositionType"].Should().Be(PositionType.Long);
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

    private sealed class StubRepository : IRepository
    {
        public Broker? Broker { get; set; }

        public IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active) =>
            Broker == null ? [] : Broker.Portfolios.SelectMany(p => p.Assets);

        public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active) =>
            Broker?.Portfolios.FirstOrDefault(p => p.Name == portfolio)?.Assets ?? [];

        public IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active) => Broker == null ? [] : [Broker];

        public Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active) =>
            Broker?.Portfolios.FirstOrDefault(p => p.Name == portfolioName)?.Assets.FirstOrDefault(a => a.Name == assetName);
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
