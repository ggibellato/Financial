using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using Microsoft.Extensions.Time.Testing;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Financial.Investment.Application.Tests.Services;

[Trait("Category", "Unit")]
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
    public async Task GetAllocationBreakdown_ByClass_GroupsAssetsByGlobalAssetClass()
    {
        SeedActive(MakeBroker("Alpha", "GBP",
            PricedAsset("A1", GlobalAssetClass.Equity, CountryCode.UK, 10m, 4m, 40m),
            PricedAsset("A2", GlobalAssetClass.Bond, CountryCode.UK, 10m, 3m, 30m)));

        var result = await CreateService().GetAllocationBreakdownAsync();

        using var _ = new AssertionScope();
        result.ByClass.Should().HaveCount(2);
        result.ByClass.Single(entry => entry.Class == GlobalAssetClass.Equity).MarketValue.Should().Be(400m);
        result.ByClass.Single(entry => entry.Class == GlobalAssetClass.Bond).MarketValue.Should().Be(300m);
    }

    [Fact]
    public async Task GetAllocationBreakdown_ByClass_UnknownClassHolding_AppearsAsUnknownEntry()
    {
        SeedActive(MakeBroker("Alpha", "GBP",
            PricedAsset("A1", GlobalAssetClass.Equity, CountryCode.UK, 10m, 4m, 40m),
            PricedAsset("A2", GlobalAssetClass.Unknown, CountryCode.UK, 10m, 3m, 30m)));

        var result = await CreateService().GetAllocationBreakdownAsync();

        result.ByClass.Should().ContainSingle(entry => entry.Class == GlobalAssetClass.Unknown && entry.MarketValue == 300m);
    }

    [Fact]
    public async Task GetAllocationBreakdown_ByCountry_UnknownCountryHolding_AppearsAsUnknownEntry()
    {
        SeedActive(MakeBroker("Alpha", "GBP",
            PricedAsset("A1", GlobalAssetClass.Equity, CountryCode.UK, 10m, 4m, 40m),
            PricedAsset("A2", GlobalAssetClass.Equity, CountryCode.Unknown, 10m, 3m, 30m)));

        var result = await CreateService().GetAllocationBreakdownAsync();

        result.ByCountry.Should().ContainSingle(entry => entry.Country == CountryCode.Unknown && entry.MarketValue == 300m);
    }

    [Fact]
    public async Task GetAllocationBreakdown_ByCurrency_GroupsAssetsByOwningBrokerCurrency()
    {
        SeedThreeDimensionPortfolio();

        var result = await CreateService().GetAllocationBreakdownAsync();

        using var _ = new AssertionScope();
        result.ByCurrency.Should().HaveCount(2);
        result.ByCurrency.Single(entry => entry.Currency == "GBP").MarketValue.Should().Be(700m);
        result.ByCurrency.Single(entry => entry.Currency == "BRL").MarketValue.Should().Be(300m);
    }

    [Fact]
    public async Task GetAllocationBreakdown_ByBroker_GroupsAssetsByOwningBrokerName()
    {
        SeedThreeDimensionPortfolio();

        var result = await CreateService().GetAllocationBreakdownAsync();

        using var _ = new AssertionScope();
        result.ByBroker.Should().HaveCount(2);
        result.ByBroker.Single(entry => entry.BrokerName == "Alpha").MarketValue.Should().Be(700m);
        result.ByBroker.Single(entry => entry.BrokerName == "Beta").MarketValue.Should().Be(300m);
    }

    [Fact]
    public async Task GetAllocationBreakdown_UnpricedActiveHolding_ExcludedFromEveryDimension()
    {
        SeedActive(
            MakeBroker("Alpha", "GBP",
                PricedAsset("A1", GlobalAssetClass.Equity, CountryCode.UK, 10m, 4m, 40m),
                PricedAsset("A2", GlobalAssetClass.Bond, CountryCode.US, 10m, 1m, 10m)),
            MakeBroker("Beta", "BRL", BoughtAsset("B1", GlobalAssetClass.RealEstate, CountryCode.BR, 10m, 3m)));

        var result = await CreateService().GetAllocationBreakdownAsync();

        using var _ = new AssertionScope();
        result.ByClass.Should().NotContain(entry => entry.Class == GlobalAssetClass.RealEstate);
        result.ByCountry.Should().NotContain(entry => entry.Country == CountryCode.BR);
        result.ByCurrency.Should().NotContain(entry => entry.Currency == "BRL");
        result.ByBroker.Should().NotContain(entry => entry.BrokerName == "Beta");
        result.ByClass.Single(entry => entry.Class == GlobalAssetClass.Equity).Percentage.Should().Be(80m);
        PercentageTotals(result).Should().AllBeEquivalentTo(100m);
    }

    [Fact]
    public async Task GetAllocationBreakdown_HistoricHolding_ExcludedFromEveryDimension()
    {
        var investments = Investments.Create();
        investments.AddActiveBroker(MakeBroker("Alpha", "GBP", PricedAsset("A1", GlobalAssetClass.Equity, CountryCode.UK, 10m, 4m, 40m)));
        investments.AddHistoricBroker(MakeBroker("Beta", "BRL", ClosedAsset("B1", GlobalAssetClass.Bond, CountryCode.BR)));
        _repository.Investments = investments;

        var result = await CreateService().GetAllocationBreakdownAsync();

        using var _ = new AssertionScope();
        result.ByBroker.Should().ContainSingle(entry => entry.BrokerName == "Alpha");
        result.ByClass.Should().NotContain(entry => entry.Class == GlobalAssetClass.Bond);
        result.ByCountry.Should().NotContain(entry => entry.Country == CountryCode.BR);
        result.ByCurrency.Should().NotContain(entry => entry.Currency == "BRL");
    }

    [Fact]
    public async Task GetAllocationBreakdown_EachDimension_PercentagesSumToExactlyOneHundred()
    {
        SeedThreeDimensionPortfolio();

        var result = await CreateService().GetAllocationBreakdownAsync();

        using var _ = new AssertionScope();
        result.ByClass.Should().HaveCount(3);
        result.ByCountry.Should().HaveCount(3);
        PercentageTotals(result).Should().AllBeEquivalentTo(100m);
        result.ByClass.Single(entry => entry.Class == GlobalAssetClass.Equity).Percentage.Should().Be(50m);
        result.ByCurrency.Single(entry => entry.Currency == "GBP").Percentage.Should().Be(70m);
    }

    [Fact]
    public async Task GetAllocationBreakdown_EntriesSortedDescendingByMarketValue()
    {
        SeedThreeDimensionPortfolio();

        var result = await CreateService().GetAllocationBreakdownAsync();

        using var _ = new AssertionScope();
        result.ByClass.Select(entry => entry.Class).Should().ContainInOrder(
            GlobalAssetClass.Equity, GlobalAssetClass.Bond, GlobalAssetClass.RealEstate);
        result.ByCountry.Select(entry => entry.Country).Should().ContainInOrder(
            CountryCode.UK, CountryCode.BR, CountryCode.US);
        result.ByCountry[1].MarketValue.Should().Be(result.ByCountry[2].MarketValue, "the two equal groups are tie-broken by label");
    }

    [Fact]
    public async Task GetAllocationBreakdown_NoActiveHoldingsPriced_ReturnsEmptyListsForAllFourDimensions()
    {
        SeedActive(MakeBroker("Alpha", "GBP", BoughtAsset("A1", GlobalAssetClass.Equity, CountryCode.UK, 10m, 4m)));

        var result = await CreateService().GetAllocationBreakdownAsync();

        using var _ = new AssertionScope();
        result.ByClass.Should().BeEmpty();
        result.ByCurrency.Should().BeEmpty();
        result.ByCountry.Should().BeEmpty();
        result.ByBroker.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllocationBreakdown_WhenEveryActiveHoldingIsFlat_ReportsZeroPercentagesInsteadOfDividingByZero()
    {
        SeedActive(MakeBroker("Alpha", "GBP", ClosedAsset("A1", GlobalAssetClass.Equity, CountryCode.UK)));

        var result = await CreateService().GetAllocationBreakdownAsync();

        using var _ = new AssertionScope();
        result.ByClass.Should().ContainSingle(entry => entry.MarketValue == 0m && entry.Percentage == 0m);
        result.ByBroker.Should().ContainSingle(entry => entry.MarketValue == 0m && entry.Percentage == 0m);
    }

    [Fact]
    public async Task GetAllocationBreakdown_WhenABrokerCurrencyIsUnrecognized_RecordsFailedSpanAndRethrows()
    {
        SeedActive(MakeBroker("Alpha", "XYZ", PricedAsset("A1", GlobalAssetClass.Equity, CountryCode.UK, 10m, 4m, 40m)));

        Func<Task> act = () => CreateService().GetAllocationBreakdownAsync();

        await act.Should().ThrowAsync<ArgumentException>();
        _tracer.ShouldHaveFailedSpan<ArgumentException>("Investment.AllocationBreakdownService.GetAllocationBreakdown");
    }

    [Fact]
    public async Task GetAllocationBreakdown_WhenRepositoryThrows_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnGetInvestments = new InvalidOperationException("simulated failure");

        Func<Task> act = () => CreateService().GetAllocationBreakdownAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
        using var _ = new AssertionScope();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("Investment.AllocationBreakdownService.GetAllocationBreakdown");
        _logger.Entries.Should().NotContain(entry => entry.Message.Contains("simulated failure"));
    }

    [Fact]
    public async Task GetAllocationBreakdown_WithBrokerCurrencyFilter_ExcludesNonMatchingActiveBrokers()
    {
        SeedThreeDimensionPortfolio();

        var result = await CreateService().GetAllocationBreakdownAsync(brokerCurrencyFilter: Currency.GBP);

        using var _ = new AssertionScope();
        result.ByBroker.Should().ContainSingle(entry => entry.BrokerName == "Alpha");
        result.ByBroker.Should().NotContain(entry => entry.BrokerName == "Beta");
        result.ByCurrency.Should().ContainSingle(entry => entry.Currency == "GBP");
    }

    [Fact]
    public async Task GetAllocationBreakdown_WithBrokerCurrencyFilterMatchingNoBrokers_ReturnsEmptyListsNotError()
    {
        SeedThreeDimensionPortfolio();

        var result = await CreateService().GetAllocationBreakdownAsync(brokerCurrencyFilter: Currency.USD);

        using var _ = new AssertionScope();
        result.ByClass.Should().BeEmpty();
        result.ByBroker.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllocationBreakdown_WithDisplayCurrency_ProducesNumericallyConsistentConvertedTotals()
    {
        SeedThreeDimensionPortfolio();

        var result = await CreateService(exchangeRateProvider: new StubExchangeRateProvider(0.2m))
            .GetAllocationBreakdownAsync(displayCurrency: Currency.GBP);

        using var _ = new AssertionScope();
        result.DisplayCurrency.Should().Be("GBP");
        result.ByBroker.Single(entry => entry.BrokerName == "Alpha").MarketValue.Should().Be(700m, "GBP holdings convert at identity (from == to)");
        result.ByBroker.Single(entry => entry.BrokerName == "Beta").MarketValue.Should().Be(300m * 0.2m, "BRL holdings convert at the stubbed rate");
        PercentageTotals(result).Should().AllSatisfy(total => total.Should().BeApproximately(100m, 0.0001m));
    }

    [Fact]
    public async Task GetAllocationBreakdown_ByCurrency_LabelStaysNativeWhileValueIsConverted()
    {
        SeedThreeDimensionPortfolio();

        var result = await CreateService(exchangeRateProvider: new StubExchangeRateProvider(0.2m))
            .GetAllocationBreakdownAsync(displayCurrency: Currency.GBP);

        using var _ = new AssertionScope();
        result.ByCurrency.Should().Contain(entry => entry.Currency == "BRL" && entry.MarketValue == 300m * 0.2m);
        result.ByCurrency.Should().Contain(entry => entry.Currency == "GBP" && entry.MarketValue == 700m);
    }

    [Fact]
    public async Task GetAllocationBreakdown_WithoutDisplayCurrency_ReproducesTodaysExactNativeSums()
    {
        SeedThreeDimensionPortfolio();

        var result = await CreateService(exchangeRateProvider: new StubExchangeRateProvider(0.2m)).GetAllocationBreakdownAsync();

        using var _ = new AssertionScope();
        result.DisplayCurrency.Should().BeNull();
        result.IsPartial.Should().BeFalse();
        result.IsUnavailable.Should().BeFalse();
        result.ByBroker.Single(entry => entry.BrokerName == "Beta").MarketValue.Should().Be(300m, "no conversion runs without a requested displayCurrency");
    }

    [Fact]
    public async Task GetAllocationBreakdown_WhenOneNativeCurrencyGroupFailsToConvert_ExcludesItAndFlagsPartial()
    {
        SeedThreeDimensionPortfolio();

        var result = await CreateService(exchangeRateProvider: new StubExchangeRateProvider(null))
            .GetAllocationBreakdownAsync(displayCurrency: Currency.GBP);

        using var _ = new AssertionScope();
        result.IsPartial.Should().BeTrue();
        result.IsUnavailable.Should().BeFalse();
        result.ByBroker.Should().ContainSingle(entry => entry.BrokerName == "Alpha", "GBP holdings need no conversion and still succeed");
        result.ByBroker.Should().NotContain(entry => entry.BrokerName == "Beta", "the BRL group's conversion failed and is excluded");
    }

    [Fact]
    public async Task GetAllocationBreakdown_WhenEveryNativeCurrencyGroupFailsToConvert_FlagsUnavailable()
    {
        SeedActive(MakeBroker("Beta", "BRL",
            PricedAsset("B1", GlobalAssetClass.RealEstate, CountryCode.BR, 10m, 2m, 20m)));

        var result = await CreateService(exchangeRateProvider: new StubExchangeRateProvider(null))
            .GetAllocationBreakdownAsync(displayCurrency: Currency.GBP);

        using var _ = new AssertionScope();
        result.IsUnavailable.Should().BeTrue();
        result.IsPartial.Should().BeFalse();
        result.ByClass.Should().BeEmpty();
    }

    private static IEnumerable<decimal> PercentageTotals(AllocationBreakdownDTO result) =>
    [
        result.ByClass.Sum(entry => entry.Percentage),
        result.ByCurrency.Sum(entry => entry.Percentage),
        result.ByCountry.Sum(entry => entry.Percentage),
        result.ByBroker.Sum(entry => entry.Percentage)
    ];

    private AllocationBreakdownService CreateService(IExchangeRateProvider? exchangeRateProvider = null) =>
        new(_repository, TestHoldingValuationService.Create(new FakeTimeProvider(Today)), _tracer, _logger,
            exchangeRateProvider ?? new StubExchangeRateProvider(null), new FakeTimeProvider(Today));

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
