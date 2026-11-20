using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Services;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.Investment.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Application.Tests.Services;

[Trait("Category", "Unit")]
public class NavigationServiceTests
{
    private readonly RecordingTelemetryTracer _tracer = new();

    private readonly StubInvestmentRepository _repository = new();
    private NavigationService CreateService() => new(_repository, TestHoldingValuationService.Create(), _tracer, NullLogger<NavigationService>.Instance);

    [Fact]
    public void GetNavigationTree_AddsOneChildPerBrokerInNameOrderAndRecordsBothSpans()
    {
        _repository.Brokers = [Broker.Create("Zeta", "BRL"), Broker.Create("Alpha", "GBP")];

        var tree = CreateService().GetNavigationTree();

        tree.Children.Select(child => child.Metadata[NavigationMetadataKeys.BrokerName]).Should().Equal("Alpha", "Zeta");
        _tracer.ShouldHaveSucceededSpan("Investment.NavigationService.GetNavigationTree")
            .Attributes[TelemetryAttributeKeys.EntityType].Should().Be("Navigation");
        _tracer.ShouldHaveSucceededSpan("Investment.NavigationService.GetBrokers");
    }

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

    [Fact]
    public void GetAssetDetails_ReturnsValuationMethodAndIncomePolicyFromAsset()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = BuildAssetWithQuantity("ASSET1", 0m);
        asset.SetValuationMethod(ValuationMethod.ProviderValue);
        asset.SetIncomePolicy(IncomePolicy.Distributing);
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var result = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        result.Should().NotBeNull();
        result!.ValuationMethod.Should().Be(ValuationMethod.ProviderValue);
        result.IncomePolicy.Should().Be(IncomePolicy.Distributing);
    }

    [Fact]
    public void GetAssetDetails_WithActiveTaxClassifications_ReturnsDistinctJurisdictions()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = BuildAssetWithQuantity("ASSET1", 0m);
        var investments = Investments.Create();
        asset.AddCredit(Credit.Create(new DateTime(2026, 1, 1), Credit.CreditType.Dividend, 50m, currency: Currency.GBP), investments);
        asset.AddCredit(Credit.Create(new DateTime(2026, 1, 1), Credit.CreditType.Dividend, 100m, currency: Currency.BRL), investments);
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details!.TaxJurisdictions.Should().Equal("BR", "UK");
        _tracer.ShouldHaveSucceededSpan("Investment.NavigationService.GetAssetDetails");
    }

    [Fact]
    public void GetAssetDetails_WithNoTaxClassifications_ReturnsEmptyJurisdictions()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        portfolio.AddAsset(BuildAssetWithQuantity("ASSET1", 0m));
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details!.TaxJurisdictions.Should().BeEmpty();
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
    public void GetAssetDetails_MapsThePositionAndGainFiguresOfTheAsset()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = Asset.Create("ASSET1", "ISIN", "BVMF", "ASSET1", CountryCode.BR, "FII", GlobalAssetClass.Equity);
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 5, 1), Transaction.TransactionType.Buy, 15m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2021, 6, 1), Credit.CreditType.Dividend, 12m));
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details.Should().NotBeNull();
        using var _ = new AssertionScope();
        details!.PositionType.Should().Be(PositionType.Long);
        details.RealizedGainLoss.Should().Be(62m);
        details.RealizedGainLossSharesOnly.Should().Be(50m);
        details.AverageSellPrice.Should().Be(110m);
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

    [Fact]
    public void GetAssetDetails_WithMultiplePriceHistoryEntries_ReturnsThemSortedByDateDescending()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = Asset.Create("ASSET1", "ISIN", "BVMF", "ASSET1", CountryCode.BR, "FII", GlobalAssetClass.Equity);
        asset.SetPrice(new DateOnly(2024, 1, 1), 10m, isManual: false);
        asset.SetPrice(new DateOnly(2024, 2, 1), 12m, isManual: false);
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details.Should().NotBeNull();
        details!.PriceSnapshots.Should().HaveCount(2);
        details.PriceSnapshots[0].Date.Should().Be(new DateOnly(2024, 2, 1));
        details.PriceSnapshots[1].Date.Should().Be(new DateOnly(2024, 1, 1));
    }

    [Fact]
    public void GetAssetDetails_ReturnsTransactionsAndCreditsNewestFirst()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = Asset.Create("ASSET1", "ISIN", "BVMF", "ASSET1", CountryCode.BR, "FII", GlobalAssetClass.Equity);
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 3, 1), Transaction.TransactionType.Buy, 5m, 100m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 4m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 4, 1), Credit.CreditType.Dividend, 6m));
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details.Should().NotBeNull();
        using var _ = new AssertionScope();
        details!.Transactions.Select(t => t.Date).Should().Equal(new DateTime(2024, 3, 1), new DateTime(2024, 1, 1));
        details.Credits.Select(c => c.Date).Should().Equal(new DateTime(2024, 4, 1), new DateTime(2024, 2, 1));
    }

    [Fact]
    public void GetAssetDetails_WithAPrice_ReturnsTheResultPercentOnTheCostOfUnitsHeld()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = Asset.Create("ASSET1", "ISIN", "BVMF", "ASSET1", CountryCode.BR, "FII", GlobalAssetClass.Equity);
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 4m, 50m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 10m));
        asset.SetPrice(new DateOnly(2024, 3, 1), 45m, isManual: false);
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details.Should().NotBeNull();
        details!.ResultPercent.Should().Be(-10m);
        details.ResultWithCreditsPercent.Should().Be(-5m);
    }

    [Fact]
    public void GetAssetDetails_WithoutAPrice_ReportsTheResultPercentUnavailable()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = Asset.Create("ASSET1", "ISIN", "BVMF", "ASSET1", CountryCode.BR, "FII", GlobalAssetClass.Equity);
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 4m, 50m, 0m));
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details.Should().NotBeNull();
        details!.ResultPercent.Should().BeNull();
        details.ResultWithCreditsPercent.Should().BeNull();
    }

    [Fact]
    public void GetAssetDetails_AfterASale_ReturnsMatchingDisposalRecord()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = Asset.Create("ASSET1", "ISIN", "BVMF", "ASSET1", CountryCode.BR, "FII", GlobalAssetClass.Equity);
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        var sale = Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m);
        asset.RecordTransaction(sale, CostBasisMethod.AverageCost);
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details.Should().NotBeNull();
        var record = details!.DisposalRecords.Should().ContainSingle().Which;
        record.TransactionId.Should().Be(sale.Id);
        record.Method.Should().Be(CostBasisMethod.AverageCost);
        record.QuantityDisposed.Should().Be(5m);
        record.Proceeds.Should().Be(sale.NetCash);
        record.GainLoss.Should().Be(record.Proceeds - record.CostBasis);
        record.Status.Should().Be(DisposalRecordStatus.Active);
    }

    [Fact]
    public void GetAssetDetails_WithCorporateActions_ReturnsThemOrderedByEffectiveDateAscending()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = Asset.Create("ASSET1", "ISIN", "BVMF", "ASSET1", CountryCode.BR, "FII", GlobalAssetClass.Equity);
        var investments = Investments.Create();
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        var merger = CorporateAction.CreateMergerSource(
            new DateTime(2026, 4, 1), 2.0m, null, null, Guid.NewGuid(), "TARGET", 20m, 1000m);
        var split = CorporateAction.CreateSplit(new DateTime(2026, 2, 1), 2.0m);
        asset.RecordCorporateAction(split, CostBasisMethod.AverageCost, investments);
        asset.RecordCorporateAction(merger, CostBasisMethod.AverageCost, investments, Currency.BRL);
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details.Should().NotBeNull();
        details!.CorporateActions.Should().HaveCount(2);
        details.CorporateActions[0].Id.Should().Be(split.Id);
        details.CorporateActions[1].Id.Should().Be(merger.Id);
    }

    [Fact]
    public void GetAssetDetails_WithNoCorporateActions_ReturnsEmptyCorporateActions()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        portfolio.AddAsset(BuildAssetWithQuantity("ASSET1", 0m));
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details.Should().NotBeNull();
        details!.CorporateActions.Should().BeEmpty();
    }

    [Fact]
    public void GetAssetDetails_WithNoDisposingTransactions_ReturnsEmptyDisposalRecords()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = Asset.Create("ASSET1", "ISIN", "BVMF", "ASSET1", CountryCode.BR, "FII", GlobalAssetClass.Equity);
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details.Should().NotBeNull();
        details!.DisposalRecords.Should().BeEmpty();
    }

    [Fact]
    public void GetAssetDetails_ReturnsTheCurrencyOfItsBroker()
    {
        var broker = Broker.Create("Broker", "GBP");
        broker.AddPortfolio("Portfolio").AddAsset(BuildAssetWithQuantity("ASSET1", 10m));
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details!.Currency.Should().Be("GBP");
    }

    [Fact]
    public void GetAssetDetails_WithNoCostBasisMethodExplicitlySet_DefaultsToAverageCost()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        portfolio.AddAsset(BuildAssetWithQuantity("ASSET1", 10m));
        _repository.Broker = broker;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details.Should().NotBeNull();
        details!.CostBasisMethod.Should().Be(CostBasisMethod.AverageCost);
    }

    [Fact]
    public void GetNavigationTree_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnGetBrokerList = new InvalidOperationException("simulated failure");

        Action act = () => CreateService().GetNavigationTree();

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("Investment.NavigationService.GetNavigationTree");
    }

    [Fact]
    public void GetAssetDetails_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnGetAsset = new InvalidOperationException("simulated failure");

        Action act = () => CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("Investment.NavigationService.GetAssetDetails");
    }

    [Fact]
    public void GetBrokers_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnGetBrokerList = new InvalidOperationException("simulated failure");

        Action act = () => CreateService().GetBrokers();

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("Investment.NavigationService.GetBrokers");
    }

    [Fact]
    public void GetAssetDetails_PrefersTheActiveBrokerWithThatNameForTheCostBasisMethod()
    {
        var investments = Investments.Create();
        var other = Broker.Create("Other", "BRL");
        other.SetCostBasisMethod(CostBasisMethod.SpecificId);
        investments.AddActiveBroker(other);
        var active = BuildBrokerWithAsset("ASSET1", GlobalAssetClass.Equity, quantity: 10m);
        active.SetCostBasisMethod(CostBasisMethod.FIFO);
        investments.AddActiveBroker(active);
        var historic = Broker.Create("Broker", "BRL");
        historic.SetCostBasisMethod(CostBasisMethod.SpecificId);
        investments.AddHistoricBroker(historic);
        _repository.Investments = investments;

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", "ASSET1");

        details!.CostBasisMethod.Should().Be(CostBasisMethod.FIFO);
    }

    [Fact]
    public void GetAssetDetails_HistoricBroker_UsesItsCostBasisMethod()
    {
        var investments = Investments.Create();
        var historic = Broker.Create("Old", "BRL");
        historic.SetCostBasisMethod(CostBasisMethod.FIFO);
        historic.AddPortfolio("Closed").AddAsset(BuildAssetWithQuantity("ASSET1", 0m));
        investments.AddHistoricBroker(historic);
        _repository.Investments = investments;

        var details = CreateService().GetAssetDetails("Old", "Closed", "ASSET1", InvestmentScope.Historic);

        details!.CostBasisMethod.Should().Be(CostBasisMethod.FIFO);
    }

    [Theory]
    [InlineData("UNKNOWN")]
    [InlineData(" ")]
    public void GetAssetDetails_UnknownOrBlankAssetName_ReturnsNullAndSucceeds(string assetName)
    {
        _repository.Broker = Broker.Create("Broker", "BRL");

        var details = CreateService().GetAssetDetails("Broker", "Portfolio", assetName);

        details.Should().BeNull();
        _tracer.ShouldHaveSucceededSpan("Investment.NavigationService.GetAssetDetails");
    }

    [Fact]
    public void GetOpenLots_WithAPartiallyConsumedBuy_ReturnsItsRemainingQuantity()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = Asset.Create("ASSET1", "ISIN", "BVMF", "ASSET1", CountryCode.BR, "FII", GlobalAssetClass.Equity);
        var buy = Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m);
        asset.AddTransaction(buy);
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 4m, 110m, 0m));
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var openLots = CreateService().GetOpenLots("Broker", "Portfolio", "ASSET1");

        openLots.Should().NotBeNull();
        var lot = openLots!.Should().ContainSingle().Which;
        lot.SourceTransactionId.Should().Be(buy.Id);
        lot.RemainingQuantity.Should().Be(6m);
        lot.UnitCost.Should().Be(100m);
        _tracer.ShouldHaveSucceededSpan("Investment.NavigationService.GetOpenLots");
    }

    [Fact]
    public void GetOpenLots_FullyConsumedBuy_IsExcluded()
    {
        var broker = Broker.Create("Broker", "BRL");
        var portfolio = broker.AddPortfolio("Portfolio");
        var asset = Asset.Create("ASSET1", "ISIN", "BVMF", "ASSET1", CountryCode.BR, "FII", GlobalAssetClass.Equity);
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 10m, 110m, 0m));
        portfolio.AddAsset(asset);
        _repository.Broker = broker;

        var openLots = CreateService().GetOpenLots("Broker", "Portfolio", "ASSET1");

        openLots.Should().NotBeNull();
        openLots.Should().BeEmpty();
    }

    [Fact]
    public void GetOpenLots_UnknownAsset_ReturnsNull()
    {
        _repository.Broker = Broker.Create("Broker", "BRL");

        var openLots = CreateService().GetOpenLots("Broker", "Portfolio", "UNKNOWN");

        openLots.Should().BeNull();
        _tracer.ShouldHaveSucceededSpan("Investment.NavigationService.GetOpenLots");
    }

    [Fact]
    public void GetOpenLots_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnGetAsset = new InvalidOperationException("simulated failure");

        Action act = () => CreateService().GetOpenLots("Broker", "Portfolio", "ASSET1");

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("Investment.NavigationService.GetOpenLots");
    }
}
