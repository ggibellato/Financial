using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Financial.Investment.Application.Tests.Services;

public class PortfolioDashboardConvertedBuilderTests
{
    private static readonly DateOnly AsOf = new(2026, 8, 14);
    private static readonly DateTime PurchaseDate = new(2025, 1, 1);
    private static readonly DateTime CreditDate = new(2026, 3, 1);

    [Fact]
    public async Task BuildAsync_ConvertsEachHoldingUsingItsOwnBrokerCurrency()
    {
        var brazilian = BoughtAsset("AAAA", quantity: 10m, unitPrice: 5m);
        var british = BoughtAsset("BBBB", quantity: 4m, unitPrice: 2m);
        var provider = new FakeExchangeRateProvider((_, from, _) => from == Currency.BRL ? 0.2m : 1.5m);

        var result = await PortfolioDashboardConvertedBuilder.BuildAsync(
            [ActiveHolding(brazilian, Currency.BRL, 80m, 30m), ActiveHolding(british, Currency.GBP, 12m, 4m)],
            Currency.USD, provider, AsOf);

        using var _ = new AssertionScope();
        result.MarketValue.Should().Be((80m * 0.2m) + (12m * 1.5m));
        result.Invested.Should().Be((50m * 0.2m) + (8m * 1.5m));
        result.UnrealisedGainLoss.Should().Be((30m * 0.2m) + (4m * 1.5m));
    }

    [Fact]
    public async Task BuildAsync_SkipsConversionWhenABrokerCurrencyMatchesTheReportingCurrency()
    {
        var asset = BoughtAsset("AAAA", quantity: 10m, unitPrice: 5m);
        var provider = new StubExchangeRateProvider(0.2m);

        var result = await PortfolioDashboardConvertedBuilder.BuildAsync(
            [ActiveHolding(asset, Currency.GBP, 80m, 30m)], Currency.GBP, provider, AsOf);

        using var _ = new AssertionScope();
        provider.CallCount.Should().Be(0);
        result.MarketValue.Should().Be(80m);
        result.Invested.Should().Be(50m);
        result.IsPartial.Should().BeFalse();
        result.IsUnavailable.Should().BeFalse();
    }

    [Fact]
    public async Task BuildAsync_IsUnavailable_TrueOnlyWhenEveryCurrencyFails()
    {
        var brazilian = BoughtAsset("AAAA", quantity: 10m, unitPrice: 5m);
        var british = BoughtAsset("BBBB", quantity: 4m, unitPrice: 2m);
        var provider = new StubExchangeRateProvider(null);

        var result = await PortfolioDashboardConvertedBuilder.BuildAsync(
            [ActiveHolding(brazilian, Currency.BRL, 80m, 30m), ActiveHolding(british, Currency.GBP, 12m, 4m)],
            Currency.USD, provider, AsOf);

        using var _ = new AssertionScope();
        result.IsUnavailable.Should().BeTrue();
        result.IsPartial.Should().BeFalse();
        result.MarketValue.Should().BeNull();
        result.Invested.Should().BeNull();
        result.UnrealisedGainLoss.Should().BeNull();
        result.RealisedGainLoss.Should().BeNull();
        result.IncomeYtd.Should().BeNull();
        result.IncomeLifetime.Should().BeNull();
        result.GrossXirr.Should().BeNull();
        result.NetXirr.Should().BeNull();
    }

    [Fact]
    public async Task BuildAsync_IsPartial_TrueWhenOneCurrencyFailsAndAnotherSucceeds()
    {
        var brazilian = BoughtAsset("AAAA", quantity: 10m, unitPrice: 5m);
        var british = BoughtAsset("BBBB", quantity: 4m, unitPrice: 2m);
        var provider = new FakeExchangeRateProvider((_, from, _) => from == Currency.BRL ? 0.2m : null);

        var result = await PortfolioDashboardConvertedBuilder.BuildAsync(
            [ActiveHolding(brazilian, Currency.BRL, 80m, 30m), ActiveHolding(british, Currency.GBP, 12m, 4m)],
            Currency.USD, provider, AsOf);

        using var _ = new AssertionScope();
        result.IsPartial.Should().BeTrue();
        result.IsUnavailable.Should().BeFalse();
        result.MarketValue.Should().Be(80m * 0.2m, "only the currency that converted contributes");
    }

    [Fact]
    public async Task BuildAsync_IsUnavailable_FalseWhenAFailingCurrencySitsAlongsideTheReportingCurrency()
    {
        var reporting = BoughtAsset("AAAA", quantity: 10m, unitPrice: 5m);
        var foreign = BoughtAsset("BBBB", quantity: 4m, unitPrice: 2m);
        var provider = new StubExchangeRateProvider(null);

        var result = await PortfolioDashboardConvertedBuilder.BuildAsync(
            [ActiveHolding(reporting, Currency.GBP, 80m, 30m), ActiveHolding(foreign, Currency.BRL, 12m, 4m)],
            Currency.GBP, provider, AsOf);

        using var _ = new AssertionScope();
        result.IsUnavailable.Should().BeFalse("the reporting-currency holding needed no rate and still produced figures");
        result.IsPartial.Should().BeTrue();
        result.MarketValue.Should().Be(80m);
    }

    [Fact]
    public async Task BuildAsync_ConvertsRealisedGainAndIncomeOnEachRecordsOwnDate()
    {
        var asset = DisposedAsset("AAAA");
        asset.AddCredit(Credit.Create(CreditDate, Credit.CreditType.Dividend, 50m, withheld: 10m));
        asset.AddCredit(Credit.Create(new DateTime(2025, 12, 31), Credit.CreditType.Dividend, 100m));
        var provider = new FakeExchangeRateProvider((date, _, _) => date.Year == 2026 ? 2m : 3m);

        var result = await PortfolioDashboardConvertedBuilder.BuildAsync(
            [ActiveHolding(asset, Currency.BRL, marketValue: null, unrealisedGain: null)], Currency.GBP, provider, AsOf);

        using var _ = new AssertionScope();
        result.RealisedGainLoss.Should().Be(20m * 3m, "the disposal is dated 2025 and converts at that year's rate");
        result.IncomeYtd.Should().Be(40m * 2m);
        result.IncomeLifetime.Should().Be((40m * 2m) + (100m * 3m));
    }

    [Fact]
    public async Task BuildAsync_ConvertedXirr_IsSolvedFromTheConvertedSeries()
    {
        var asset = BoughtAsset("AAAA", quantity: 10m, unitPrice: 10m);
        var provider = new StubExchangeRateProvider(0.5m);

        var result = await PortfolioDashboardConvertedBuilder.BuildAsync(
            [ActiveHolding(asset, Currency.BRL, 110m, 10m)], Currency.GBP, provider, new DateOnly(2026, 1, 1));

        using var _ = new AssertionScope();
        result.GrossXirr.Should().BeApproximately(0.1m, 0.001m, "a uniform rate rescales every flow, leaving the solved rate unchanged");
        result.NetXirr.Should().Be(result.GrossXirr);
    }

    [Fact]
    public async Task BuildAsync_HistoricHoldingContributesNoTerminalValueOrInvestedAmount()
    {
        var active = BoughtAsset("AAAA", quantity: 10m, unitPrice: 5m);
        var closed = DisposedAsset("CLOSED");
        var provider = new StubExchangeRateProvider(0.2m);

        var result = await PortfolioDashboardConvertedBuilder.BuildAsync(
            [ActiveHolding(active, Currency.BRL, 80m, 30m), HistoricHolding(closed, Currency.BRL)],
            Currency.GBP, provider, AsOf);

        using var _ = new AssertionScope();
        result.MarketValue.Should().Be(80m * 0.2m);
        result.Invested.Should().Be(50m * 0.2m);
        result.RealisedGainLoss.Should().Be(20m * 0.2m, "a closed position's realised gain still counts");
    }

    private static Asset BoughtAsset(string name, decimal quantity, decimal unitPrice)
    {
        var asset = Asset.Create(name, $"ISIN-{name}", "BVMF", name);
        asset.AddTransaction(Transaction.Create(PurchaseDate, Transaction.TransactionType.Buy, quantity, unitPrice, 0m));
        return asset;
    }

    private static Asset DisposedAsset(string name)
    {
        var asset = Asset.Create(name, $"ISIN-{name}", "BVMF", name);
        asset.RecordTransaction(Transaction.Create(PurchaseDate, Transaction.TransactionType.Buy, 10m, 10m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2025, 6, 1), Transaction.TransactionType.Sell, 4m, 15m, 0m));
        return asset;
    }

    private static PortfolioHolding ActiveHolding(Asset asset, Currency currency, decimal? marketValue, decimal? unrealisedGain) =>
        new(asset, currency, IsActive: true, marketValue, unrealisedGain);

    private static PortfolioHolding HistoricHolding(Asset asset, Currency currency) =>
        new(asset, currency, IsActive: false, MarketValue: null, UnrealisedGain: null);
}
