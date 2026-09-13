using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Investment.Application.Tests.Services;

public class FxEntryCaptureHelperTests
{
    private static readonly FixedReportingCurrencyProvider ReportingCurrencyProvider = new();

    [Fact]
    public async Task CaptureAsync_WhenBrokerCurrencyMatchesReportingCurrency_ReturnsNoSnapshot()
    {
        var repository = new StubInvestmentRepository { Brokers = [Broker.Create("Trading212", "GBP")] };
        var provider = new StubExchangeRateProvider(0.15m);

        var result = await FxEntryCaptureHelper.CaptureAsync(
            repository, provider, ReportingCurrencyProvider, TimeProvider.System, "Trading212", new DateTime(2026, 7, 1));

        result.Should().NotBeNull();
        result!.Value.Currency.Should().Be(Currency.GBP);
        result.Value.FxRateSnapshot.Should().BeNull();
        provider.CallCount.Should().Be(0, "no conversion is needed when the currencies already match");
    }

    [Fact]
    public async Task CaptureAsync_WhenCurrenciesDiffer_CapturesSnapshotFromProvider()
    {
        var repository = new StubInvestmentRepository { Brokers = [Broker.Create("XPI", "BRL")] };
        var provider = new StubExchangeRateProvider(0.146m);
        var retrievedAt = new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(retrievedAt);

        var result = await FxEntryCaptureHelper.CaptureAsync(
            repository, provider, ReportingCurrencyProvider, timeProvider, "XPI", new DateTime(2026, 7, 1));

        result.Should().NotBeNull();
        result!.Value.Currency.Should().Be(Currency.BRL);
        result.Value.FxRateSnapshot.Should().NotBeNull();
        result.Value.FxRateSnapshot!.ToCurrency.Should().Be(Currency.GBP);
        result.Value.FxRateSnapshot!.Rate.Should().Be(0.146m);
        result.Value.FxRateSnapshot!.Source.Should().Be(FxRateSource.Frankfurter);
        result.Value.FxRateSnapshot!.RetrievedAt.Should().Be(retrievedAt);
    }

    [Fact]
    public async Task CaptureAsync_WhenProviderReturnsNoRate_LeavesSnapshotNull()
    {
        var repository = new StubInvestmentRepository { Brokers = [Broker.Create("XPI", "BRL")] };
        var provider = new StubExchangeRateProvider(rate: null);

        var result = await FxEntryCaptureHelper.CaptureAsync(
            repository, provider, ReportingCurrencyProvider, TimeProvider.System, "XPI", new DateTime(2026, 7, 1));

        result.Should().NotBeNull();
        result!.Value.Currency.Should().Be(Currency.BRL);
        result.Value.FxRateSnapshot.Should().BeNull();
    }

    [Fact]
    public async Task CaptureAsync_WhenBrokerNotFound_ReturnsNull()
    {
        var repository = new StubInvestmentRepository { Brokers = [] };
        var provider = new StubExchangeRateProvider(0.15m);

        var result = await FxEntryCaptureHelper.CaptureAsync(
            repository, provider, ReportingCurrencyProvider, TimeProvider.System, "Unknown", new DateTime(2026, 7, 1));

        result.Should().BeNull();
    }

    [Fact]
    public async Task CaptureAsync_WhenBrokerCurrencyIsUnrecognized_Throws()
    {
        var repository = new StubInvestmentRepository { Brokers = [Broker.Create("Bad", "Pounds")] };
        var provider = new StubExchangeRateProvider(0.15m);

        var act = async () => await FxEntryCaptureHelper.CaptureAsync(
            repository, provider, ReportingCurrencyProvider, TimeProvider.System, "Bad", new DateTime(2026, 7, 1));

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
