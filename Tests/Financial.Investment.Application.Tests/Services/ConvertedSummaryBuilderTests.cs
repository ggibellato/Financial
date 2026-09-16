using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Financial.Investment.Application.Tests.Services;

public class ConvertedSummaryBuilderTests
{
    private static readonly DateTime AsOf = new(2026, 8, 14);
    private static readonly IXirrCalculationService Xirr = new XirrCalculationService();

    private static Asset MakeAsset(string name = "TEST", string ticker = "TEST") =>
        Asset.Create(name, "ISIN", "BVMF", ticker);

    [Fact]
    public async Task BuildAsync_SameCurrency_MakesNoProviderCallsAndReturnsNativeAmounts()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        var provider = new StubExchangeRateProvider(null);

        var result = await ConvertedSummaryBuilder.BuildAsync(
            [asset], Currency.BRL, Currency.BRL, marketValueSum: 60m, unrealisedGainSum: 10m,
            provider, Xirr, AsOf);

        using var _ = new AssertionScope();
        provider.CallCount.Should().Be(0);
        result.ReportingCurrency.Should().Be(Currency.BRL);
        result.ConvertedMarketValue.Should().Be(60m);
        result.ConvertedInvested.Should().Be(50m);
        result.ConvertedUnrealisedGainLoss.Should().Be(10m);
        result.IsPartial.Should().BeFalse();
        result.IsReportingCurrencyUnavailable.Should().BeFalse();
    }

    [Fact]
    public async Task BuildAsync_DifferingCurrency_ConvertsEachContributingRecordIndividually()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2025, 2, 1), Transaction.TransactionType.Sell, 4m, 5m, 0m));
        var provider = new StubExchangeRateProvider(0.2m);

        var result = await ConvertedSummaryBuilder.BuildAsync(
            [asset], Currency.BRL, Currency.GBP, marketValueSum: 30m, unrealisedGainSum: 5m,
            provider, Xirr, AsOf);

        using var _ = new AssertionScope();
        result.ReportingCurrency.Should().Be(Currency.GBP);
        result.ConvertedMarketValue.Should().Be(6m);
        result.ConvertedUnrealisedGainLoss.Should().Be(1m);
        result.ConvertedInvested.Should().Be(6m);
        result.IsPartial.Should().BeFalse();
        result.IsReportingCurrencyUnavailable.Should().BeFalse();
    }

    [Fact]
    public async Task BuildAsync_MarketValueAndUnrealisedGain_ConvertAtTodaysRate_NotEachRecordsDate()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        var provider = new FakeExchangeRateProvider((date, _, _) => date == DateOnly.FromDateTime(AsOf) ? 0.5m : 0.1m);

        var result = await ConvertedSummaryBuilder.BuildAsync(
            [asset], Currency.BRL, Currency.GBP, marketValueSum: 100m, unrealisedGainSum: 20m,
            provider, Xirr, AsOf);

        using var _ = new AssertionScope();
        result.ConvertedMarketValue.Should().Be(50m);
        result.ConvertedUnrealisedGainLoss.Should().Be(10m);
    }

    [Fact]
    public async Task BuildAsync_SomeRecordsFailToConvert_IsPartial_AndTotalsWhatDidConvert()
    {
        var asset = MakeAsset();
        var convertibleDate = new DateTime(2025, 1, 1);
        var unconvertibleDate = new DateTime(2025, 2, 1);
        asset.AddTransaction(Transaction.Create(convertibleDate, Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset.AddTransaction(Transaction.Create(unconvertibleDate, Transaction.TransactionType.Buy, 4m, 5m, 0m));
        var provider = new FakeExchangeRateProvider((date, _, _) => date == DateOnly.FromDateTime(convertibleDate) ? 0.2m : null);

        var result = await ConvertedSummaryBuilder.BuildAsync(
            [asset], Currency.BRL, Currency.GBP, marketValueSum: 30m, unrealisedGainSum: 0m,
            provider, Xirr, AsOf);

        using var _ = new AssertionScope();
        result.IsPartial.Should().BeTrue();
        result.IsReportingCurrencyUnavailable.Should().BeFalse();
        result.ConvertedInvested.Should().Be(10m);
    }

    [Fact]
    public async Task BuildAsync_EveryAttemptedLookupFails_IsReportingCurrencyUnavailable_AndEveryFieldIsNull()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        var provider = new StubExchangeRateProvider(null);

        var result = await ConvertedSummaryBuilder.BuildAsync(
            [asset], Currency.BRL, Currency.GBP, marketValueSum: 30m, unrealisedGainSum: 5m,
            provider, Xirr, AsOf);

        using var _ = new AssertionScope();
        result.IsReportingCurrencyUnavailable.Should().BeTrue();
        result.IsPartial.Should().BeFalse();
        result.ConvertedMarketValue.Should().BeNull();
        result.ConvertedInvested.Should().BeNull();
        result.ConvertedUnrealisedGainLoss.Should().BeNull();
        result.ConvertedTotalReturn.Should().BeNull();
        result.ConvertedTotalReturnNetOfTax.Should().BeNull();
    }

    [Fact]
    public async Task BuildAsync_NullNativeMarketValueAndUnrealisedGain_ConvertedFieldsAreNullTooWithNoProviderCall()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        var provider = new StubExchangeRateProvider(0.2m);

        var result = await ConvertedSummaryBuilder.BuildAsync(
            [asset], Currency.BRL, Currency.GBP, marketValueSum: null, unrealisedGainSum: null,
            provider, Xirr, AsOf);

        using var _ = new AssertionScope();
        result.ConvertedMarketValue.Should().BeNull();
        result.ConvertedUnrealisedGainLoss.Should().BeNull();
        provider.CallCount.Should().Be(1, "only the transaction's own date should be looked up, not today's for the null market value/gain");
    }
}
