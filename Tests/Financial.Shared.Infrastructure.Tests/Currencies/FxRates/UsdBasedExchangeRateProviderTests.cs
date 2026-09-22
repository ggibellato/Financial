using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Abstractions.Sync;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Shared.Infrastructure.Tests.Currencies.FxRates;

public class UsdBasedExchangeRateProviderTests
{
    private sealed class FakeFxRateStore : IFxRateStore
    {
        private readonly Dictionary<DateOnly, FxRateRecord> _rates = new();

        public int SetRateCallCount { get; private set; }

        public void Seed(DateOnly date, FxRateRecord record) => _rates[date] = record;

        public FxRateRecord? TryGetRate(DateOnly date) => _rates.GetValueOrDefault(date);

        public Task SetRateAsync(DateOnly date, FxRateRecord record)
        {
            SetRateCallCount++;
            _rates[date] = record;
            return Task.CompletedTask;
        }

        public SyncStatus GetStatus() => new(SyncState.Idle, null, null);

        public Task FlushAsync() => Task.CompletedTask;
    }

    private sealed class FakeUsdRateFetcher : IUsdRateFetcher
    {
        public int CallCount { get; private set; }
        public UsdRateFetchResult NextResult { get; set; } = new(null, null);

        public Task<UsdRateFetchResult> FetchAsync(DateOnly date)
        {
            CallCount++;
            return Task.FromResult(NextResult);
        }
    }

    private static readonly DateOnly HistoricalDate = new(2026, 1, 15);

    [Fact]
    public async Task GetHistoricalRateAsync_SameCurrency_ReturnsOneWithoutTouchingStoreOrFetcher()
    {
        var store = new FakeFxRateStore();
        var fetcher = new FakeUsdRateFetcher();
        var provider = CreateProvider(store, fetcher);

        var rate = await provider.GetHistoricalRateAsync(HistoricalDate, Currency.BRL, Currency.BRL);

        rate.Should().Be(1m);
        fetcher.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetHistoricalRateAsync_HistoricalDateAlreadyInStore_ComputesFromStoredRatesOnly()
    {
        var store = new FakeFxRateStore();
        store.Seed(HistoricalDate, new FxRateRecord(5.0m, 0.8m, "frankfurter", DateTimeOffset.UtcNow));
        var fetcher = new FakeUsdRateFetcher();
        var provider = CreateProvider(store, fetcher);

        var rate = await provider.GetHistoricalRateAsync(HistoricalDate, Currency.USD, Currency.BRL);

        rate.Should().Be(5.0m);
        fetcher.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetHistoricalRateAsync_HistoricalDateNotInStore_FetchesComputesAndPersistsBothCurrencies()
    {
        var store = new FakeFxRateStore();
        var fetcher = new FakeUsdRateFetcher { NextResult = new UsdRateFetchResult(5.0m, 0.8m) };
        var provider = CreateProvider(store, fetcher);

        var rate = await provider.GetHistoricalRateAsync(HistoricalDate, Currency.USD, Currency.BRL);

        rate.Should().Be(5.0m);
        store.SetRateCallCount.Should().Be(1);
        store.TryGetRate(HistoricalDate).Should().BeEquivalentTo(
            new { BrlRate = 5.0m, GbpRate = 0.8m, Source = "frankfurter" });
    }

    [Fact]
    public async Task GetHistoricalRateAsync_TodaysDate_ReturnsLiveRateAndNeverPersists()
    {
        var store = new FakeFxRateStore();
        var fetcher = new FakeUsdRateFetcher { NextResult = new UsdRateFetchResult(5.0m, 0.8m) };
        var provider = CreateProvider(store, fetcher);
        var today = DateOnly.FromDateTime(DateTime.Now);

        var rate = await provider.GetHistoricalRateAsync(today, Currency.USD, Currency.BRL);

        rate.Should().Be(5.0m);
        store.SetRateCallCount.Should().Be(0);
        store.TryGetRate(today).Should().BeNull();
    }

    [Fact]
    public async Task GetHistoricalRateAsync_AllSixPairsForSameDate_AreMathematicallyConsistent()
    {
        var store = new FakeFxRateStore();
        store.Seed(HistoricalDate, new FxRateRecord(5.0m, 0.8m, "frankfurter", DateTimeOffset.UtcNow));
        var provider = CreateProvider(store, new FakeUsdRateFetcher());

        var usdToBrl = await provider.GetHistoricalRateAsync(HistoricalDate, Currency.USD, Currency.BRL);
        var usdToGbp = await provider.GetHistoricalRateAsync(HistoricalDate, Currency.USD, Currency.GBP);
        var brlToUsd = await provider.GetHistoricalRateAsync(HistoricalDate, Currency.BRL, Currency.USD);
        var gbpToUsd = await provider.GetHistoricalRateAsync(HistoricalDate, Currency.GBP, Currency.USD);
        var brlToGbp = await provider.GetHistoricalRateAsync(HistoricalDate, Currency.BRL, Currency.GBP);
        var gbpToBrl = await provider.GetHistoricalRateAsync(HistoricalDate, Currency.GBP, Currency.BRL);

        usdToBrl.Should().Be(5.0m);
        usdToGbp.Should().Be(0.8m);
        brlToUsd.Should().Be(1m / 5.0m);
        gbpToUsd.Should().Be(1m / 0.8m);
        brlToGbp.Should().Be(0.8m / 5.0m);
        gbpToBrl.Should().Be(5.0m / 0.8m);
    }

    [Fact]
    public async Task GetHistoricalRateAsync_FetchFailsForBothCurrencies_ReturnsNullAndDoesNotPersist()
    {
        var store = new FakeFxRateStore();
        var fetcher = new FakeUsdRateFetcher { NextResult = new UsdRateFetchResult(null, null) };
        var provider = CreateProvider(store, fetcher);

        var rate = await provider.GetHistoricalRateAsync(HistoricalDate, Currency.USD, Currency.BRL);

        rate.Should().BeNull();
        store.SetRateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetHistoricalRateAsync_FetchResolvesOnlyOneCurrency_SatisfiesThatCurrencyButDoesNotPersist()
    {
        var store = new FakeFxRateStore();
        var fetcher = new FakeUsdRateFetcher { NextResult = new UsdRateFetchResult(5.0m, null) };
        var provider = CreateProvider(store, fetcher);

        var rate = await provider.GetHistoricalRateAsync(HistoricalDate, Currency.USD, Currency.BRL);

        rate.Should().Be(5.0m);
        store.SetRateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetHistoricalRateAsync_FetchResolvesOnlyOneCurrency_ReturnsNullForTheMissingOne()
    {
        var store = new FakeFxRateStore();
        var fetcher = new FakeUsdRateFetcher { NextResult = new UsdRateFetchResult(5.0m, null) };
        var provider = CreateProvider(store, fetcher);

        var rate = await provider.GetHistoricalRateAsync(HistoricalDate, Currency.USD, Currency.GBP);

        rate.Should().BeNull();
    }

    [Fact]
    public async Task GetHistoricalRateAsync_StoredRateIsZero_ReturnsNullAndLogsWarningWithDateOnly()
    {
        var store = new FakeFxRateStore();
        store.Seed(HistoricalDate, new FxRateRecord(0m, 0.8m, "frankfurter", DateTimeOffset.UtcNow));
        var logger = new RecordingLogger<UsdBasedExchangeRateProvider>();
        var provider = new UsdBasedExchangeRateProvider(store, new FakeUsdRateFetcher(), logger);

        var rate = await provider.GetHistoricalRateAsync(HistoricalDate, Currency.USD, Currency.BRL);

        rate.Should().BeNull();
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task GetHistoricalRateAsync_RateFromStore_UsedWithNoRedundantFetcherCall()
    {
        var store = new FakeFxRateStore();
        store.Seed(HistoricalDate, new FxRateRecord(5.0m, 0.8m, "frankfurter", DateTimeOffset.UtcNow));
        var fetcher = new FakeUsdRateFetcher();
        var provider = CreateProvider(store, fetcher);

        var rate = await provider.GetHistoricalRateAsync(HistoricalDate, Currency.BRL, Currency.GBP);

        rate.Should().Be(0.8m / 5.0m);
        fetcher.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetHistoricalRateAsync_RateFromFetcher_HandedToStoreOnlyWhenBothCurrenciesPresent()
    {
        var store = new FakeFxRateStore();
        var partialFetcher = new FakeUsdRateFetcher { NextResult = new UsdRateFetchResult(5.0m, null) };
        var partialProvider = CreateProvider(store, partialFetcher);
        await partialProvider.GetHistoricalRateAsync(HistoricalDate, Currency.USD, Currency.BRL);
        store.SetRateCallCount.Should().Be(0);

        var fullFetcher = new FakeUsdRateFetcher { NextResult = new UsdRateFetchResult(5.0m, 0.8m) };
        var fullProvider = CreateProvider(store, fullFetcher);
        var otherDate = HistoricalDate.AddDays(-1);
        await fullProvider.GetHistoricalRateAsync(otherDate, Currency.USD, Currency.BRL);

        store.SetRateCallCount.Should().Be(1);
    }

    private static UsdBasedExchangeRateProvider CreateProvider(IFxRateStore store, IUsdRateFetcher fetcher) =>
        new(store, fetcher, NullLogger<UsdBasedExchangeRateProvider>.Instance);
}
