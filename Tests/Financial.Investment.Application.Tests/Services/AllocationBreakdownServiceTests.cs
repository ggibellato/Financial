using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Financial.Investment.Application.Tests.Services;

public class AllocationBreakdownServiceTests
{
    private static readonly DateTimeOffset Today = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly TodayDate = DateOnly.FromDateTime(Today.UtcDateTime);
    private static readonly DateTime PurchaseDate = new(2025, 1, 1);
    private static readonly DateTime SaleDate = new(2025, 6, 1);

    private readonly StubInvestmentRepository _repository = new();
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly RecordingLogger<AllocationBreakdownService> _logger = new();

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new AllocationBreakdownService(null!, TestHoldingValuationService.Create(), _tracer, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullHoldingValuationService_Throws()
    {
        Action act = () => new AllocationBreakdownService(_repository, null!, _tracer, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("holdingValuationService");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new AllocationBreakdownService(_repository, TestHoldingValuationService.Create(), null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new AllocationBreakdownService(_repository, TestHoldingValuationService.Create(), _tracer, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void GetAllocationBreakdown_ByClass_GroupsAssetsByGlobalAssetClass()
    {
        SeedActive(MakeBroker("Alpha", "GBP",
            PricedAsset("A1", GlobalAssetClass.Equity, CountryCode.UK, 10m, 4m, 40m),
            PricedAsset("A2", GlobalAssetClass.Bond, CountryCode.UK, 10m, 3m, 30m)));

        var result = CreateService().GetAllocationBreakdown();

        using var _ = new AssertionScope();
        result.ByClass.Should().HaveCount(2);
        result.ByClass.Single(entry => entry.Class == GlobalAssetClass.Equity).MarketValue.Should().Be(400m);
        result.ByClass.Single(entry => entry.Class == GlobalAssetClass.Bond).MarketValue.Should().Be(300m);
    }

    [Fact]
    public void GetAllocationBreakdown_ByClass_UnknownClassHolding_AppearsAsUnknownEntry()
    {
        SeedActive(MakeBroker("Alpha", "GBP",
            PricedAsset("A1", GlobalAssetClass.Equity, CountryCode.UK, 10m, 4m, 40m),
            PricedAsset("A2", GlobalAssetClass.Unknown, CountryCode.UK, 10m, 3m, 30m)));

        var result = CreateService().GetAllocationBreakdown();

        result.ByClass.Should().ContainSingle(entry => entry.Class == GlobalAssetClass.Unknown && entry.MarketValue == 300m);
    }

    [Fact]
    public void GetAllocationBreakdown_ByCountry_UnknownCountryHolding_AppearsAsUnknownEntry()
    {
        SeedActive(MakeBroker("Alpha", "GBP",
            PricedAsset("A1", GlobalAssetClass.Equity, CountryCode.UK, 10m, 4m, 40m),
            PricedAsset("A2", GlobalAssetClass.Equity, CountryCode.Unknown, 10m, 3m, 30m)));

        var result = CreateService().GetAllocationBreakdown();

        result.ByCountry.Should().ContainSingle(entry => entry.Country == CountryCode.Unknown && entry.MarketValue == 300m);
    }

    [Fact]
    public void GetAllocationBreakdown_ByCurrency_GroupsAssetsByOwningBrokerCurrency()
    {
        SeedThreeDimensionPortfolio();

        var result = CreateService().GetAllocationBreakdown();

        using var _ = new AssertionScope();
        result.ByCurrency.Should().HaveCount(2);
        result.ByCurrency.Single(entry => entry.Currency == "GBP").MarketValue.Should().Be(700m);
        result.ByCurrency.Single(entry => entry.Currency == "BRL").MarketValue.Should().Be(300m);
    }

    [Fact]
    public void GetAllocationBreakdown_ByBroker_GroupsAssetsByOwningBrokerName()
    {
        SeedThreeDimensionPortfolio();

        var result = CreateService().GetAllocationBreakdown();

        using var _ = new AssertionScope();
        result.ByBroker.Should().HaveCount(2);
        result.ByBroker.Single(entry => entry.BrokerName == "Alpha").MarketValue.Should().Be(700m);
        result.ByBroker.Single(entry => entry.BrokerName == "Beta").MarketValue.Should().Be(300m);
    }

    [Fact]
    public void GetAllocationBreakdown_UnpricedActiveHolding_ExcludedFromEveryDimension()
    {
        SeedActive(
            MakeBroker("Alpha", "GBP",
                PricedAsset("A1", GlobalAssetClass.Equity, CountryCode.UK, 10m, 4m, 40m),
                PricedAsset("A2", GlobalAssetClass.Bond, CountryCode.US, 10m, 1m, 10m)),
            MakeBroker("Beta", "BRL", BoughtAsset("B1", GlobalAssetClass.RealEstate, CountryCode.BR, 10m, 3m)));

        var result = CreateService().GetAllocationBreakdown();

        using var _ = new AssertionScope();
        result.ByClass.Should().NotContain(entry => entry.Class == GlobalAssetClass.RealEstate);
        result.ByCountry.Should().NotContain(entry => entry.Country == CountryCode.BR);
        result.ByCurrency.Should().NotContain(entry => entry.Currency == "BRL");
        result.ByBroker.Should().NotContain(entry => entry.BrokerName == "Beta");
        result.ByClass.Single(entry => entry.Class == GlobalAssetClass.Equity).Percentage.Should().Be(80m);
        PercentageTotals(result).Should().AllBeEquivalentTo(100m);
    }

    [Fact]
    public void GetAllocationBreakdown_HistoricHolding_ExcludedFromEveryDimension()
    {
        var investments = Investments.Create();
        investments.AddActiveBroker(MakeBroker("Alpha", "GBP", PricedAsset("A1", GlobalAssetClass.Equity, CountryCode.UK, 10m, 4m, 40m)));
        investments.AddHistoricBroker(MakeBroker("Beta", "BRL", ClosedAsset("B1", GlobalAssetClass.Bond, CountryCode.BR)));
        _repository.Investments = investments;

        var result = CreateService().GetAllocationBreakdown();

        using var _ = new AssertionScope();
        result.ByBroker.Should().ContainSingle(entry => entry.BrokerName == "Alpha");
        result.ByClass.Should().NotContain(entry => entry.Class == GlobalAssetClass.Bond);
        result.ByCountry.Should().NotContain(entry => entry.Country == CountryCode.BR);
        result.ByCurrency.Should().NotContain(entry => entry.Currency == "BRL");
    }

    [Fact]
    public void GetAllocationBreakdown_EachDimension_PercentagesSumToExactlyOneHundred()
    {
        SeedThreeDimensionPortfolio();

        var result = CreateService().GetAllocationBreakdown();

        using var _ = new AssertionScope();
        result.ByClass.Should().HaveCount(3);
        result.ByCountry.Should().HaveCount(3);
        PercentageTotals(result).Should().AllBeEquivalentTo(100m);
        result.ByClass.Single(entry => entry.Class == GlobalAssetClass.Equity).Percentage.Should().Be(50m);
        result.ByCurrency.Single(entry => entry.Currency == "GBP").Percentage.Should().Be(70m);
    }

    [Fact]
    public void GetAllocationBreakdown_EntriesSortedDescendingByMarketValue()
    {
        SeedThreeDimensionPortfolio();

        var result = CreateService().GetAllocationBreakdown();

        using var _ = new AssertionScope();
        result.ByClass.Select(entry => entry.Class).Should().ContainInOrder(
            GlobalAssetClass.Equity, GlobalAssetClass.Bond, GlobalAssetClass.RealEstate);
        result.ByCountry.Select(entry => entry.Country).Should().ContainInOrder(
            CountryCode.UK, CountryCode.BR, CountryCode.US);
        result.ByCountry[1].MarketValue.Should().Be(result.ByCountry[2].MarketValue, "the two equal groups are tie-broken by label");
    }

    [Fact]
    public void GetAllocationBreakdown_NoActiveHoldingsPriced_ReturnsEmptyListsForAllFourDimensions()
    {
        SeedActive(MakeBroker("Alpha", "GBP", BoughtAsset("A1", GlobalAssetClass.Equity, CountryCode.UK, 10m, 4m)));

        var result = CreateService().GetAllocationBreakdown();

        using var _ = new AssertionScope();
        result.ByClass.Should().BeEmpty();
        result.ByCurrency.Should().BeEmpty();
        result.ByCountry.Should().BeEmpty();
        result.ByBroker.Should().BeEmpty();
    }

    [Fact]
    public void GetAllocationBreakdown_WhenEveryActiveHoldingIsFlat_ReportsZeroPercentagesInsteadOfDividingByZero()
    {
        SeedActive(MakeBroker("Alpha", "GBP", ClosedAsset("A1", GlobalAssetClass.Equity, CountryCode.UK)));

        var result = CreateService().GetAllocationBreakdown();

        using var _ = new AssertionScope();
        result.ByClass.Should().ContainSingle(entry => entry.MarketValue == 0m && entry.Percentage == 0m);
        result.ByBroker.Should().ContainSingle(entry => entry.MarketValue == 0m && entry.Percentage == 0m);
    }

    [Fact]
    public void GetAllocationBreakdown_WhenABrokerCurrencyIsUnrecognized_RecordsFailedSpanAndRethrows()
    {
        SeedActive(MakeBroker("Alpha", "XYZ", PricedAsset("A1", GlobalAssetClass.Equity, CountryCode.UK, 10m, 4m, 40m)));

        Action act = () => CreateService().GetAllocationBreakdown();

        act.Should().Throw<ArgumentException>();
        _tracer.Spans.Should().ContainSingle(span => span.RecordedException is ArgumentException);
    }

    [Fact]
    public void GetAllocationBreakdown_WhenRepositoryThrows_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnGetInvestments = new InvalidOperationException("simulated failure");

        Action act = () => CreateService().GetAllocationBreakdown();

        act.Should().Throw<InvalidOperationException>();
        using var _ = new AssertionScope();
        _tracer.Spans.Should().ContainSingle(span => span.RecordedException is InvalidOperationException);
        _logger.Entries.Should().NotContain(entry => entry.Message.Contains("simulated failure"));
    }

    private static IEnumerable<decimal> PercentageTotals(AllocationBreakdownDTO result) =>
    [
        result.ByClass.Sum(entry => entry.Percentage),
        result.ByCurrency.Sum(entry => entry.Percentage),
        result.ByCountry.Sum(entry => entry.Percentage),
        result.ByBroker.Sum(entry => entry.Percentage)
    ];

    private AllocationBreakdownService CreateService() =>
        new(_repository, TestHoldingValuationService.Create(new FakeTimeProvider(Today)), _tracer, _logger);

    private void SeedActive(params Broker[] brokers)
    {
        var investments = Investments.Create();
        foreach (var broker in brokers)
        {
            investments.AddActiveBroker(broker);
        }

        _repository.Investments = investments;
    }

    private void SeedThreeDimensionPortfolio() =>
        SeedActive(
            MakeBroker("Alpha", "GBP",
                PricedAsset("A1", GlobalAssetClass.Equity, CountryCode.UK, 10m, 4m, 40m),
                PricedAsset("A2", GlobalAssetClass.Bond, CountryCode.US, 10m, 3m, 30m)),
            MakeBroker("Beta", "BRL",
                PricedAsset("B1", GlobalAssetClass.RealEstate, CountryCode.BR, 10m, 2m, 20m),
                PricedAsset("B2", GlobalAssetClass.Equity, CountryCode.BR, 10m, 1m, 10m)));

    private static Broker MakeBroker(string name, string currency, params Asset[] assets)
    {
        var broker = Broker.Create(name, currency);
        var portfolio = broker.AddPortfolio("Default");
        foreach (var asset in assets)
        {
            portfolio.AddAsset(asset);
        }

        return broker;
    }

    private static Asset BoughtAsset(string name, GlobalAssetClass assetClass, CountryCode country, decimal quantity, decimal unitPrice)
    {
        var asset = Asset.Create(name, $"ISIN-{name}", "BVMF", name, country, string.Empty, assetClass);
        asset.AddTransaction(Transaction.Create(PurchaseDate, Transaction.TransactionType.Buy, quantity, unitPrice, 0m));
        return asset;
    }

    private static Asset PricedAsset(
        string name, GlobalAssetClass assetClass, CountryCode country, decimal quantity, decimal unitPrice, decimal price)
    {
        var asset = BoughtAsset(name, assetClass, country, quantity, unitPrice);
        asset.SetPrice(TodayDate, price, isManual: false);
        return asset;
    }

    private static Asset ClosedAsset(string name, GlobalAssetClass assetClass, CountryCode country)
    {
        var asset = BoughtAsset(name, assetClass, country, quantity: 5m, unitPrice: 10m);
        asset.AddTransaction(Transaction.Create(SaleDate, Transaction.TransactionType.Sell, 5m, 12m, 0m));
        return asset;
    }
}
