using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Currencies;

public class InMemoryCachedExchangeRateProviderTests
{
    private sealed class CountingExchangeRateProvider : IExchangeRateProvider
    {
        public int CallCount { get; private set; }
        public decimal? NextResult { get; set; } = 5.1m;

        public Task<decimal?> GetHistoricalRateAsync(DateOnly date, Currency from, Currency to)
        {
            CallCount++;
            return Task.FromResult(NextResult);
        }
    }

    [Fact]
    public async Task GetHistoricalRateAsync_SecondCallForSameKey_DoesNotCallInnerAgain()
    {
        var inner = new CountingExchangeRateProvider();
        var cache = new InMemoryCachedExchangeRateProvider(() => inner);
        var date = new DateOnly(2026, 1, 15);

        var first = await cache.GetHistoricalRateAsync(date, Currency.BRL, Currency.GBP);
        var second = await cache.GetHistoricalRateAsync(date, Currency.BRL, Currency.GBP);

        first.Should().Be(5.1m);
        second.Should().Be(5.1m);
        inner.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetHistoricalRateAsync_DifferentDates_CallsInnerForEach()
    {
        var inner = new CountingExchangeRateProvider();
        var cache = new InMemoryCachedExchangeRateProvider(() => inner);

        await cache.GetHistoricalRateAsync(new DateOnly(2026, 1, 15), Currency.BRL, Currency.GBP);
        await cache.GetHistoricalRateAsync(new DateOnly(2026, 1, 16), Currency.BRL, Currency.GBP);

        inner.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task GetHistoricalRateAsync_NullResult_IsNotCached()
    {
        var inner = new CountingExchangeRateProvider { NextResult = null };
        var cache = new InMemoryCachedExchangeRateProvider(() => inner);
        var date = new DateOnly(2026, 1, 15);

        var first = await cache.GetHistoricalRateAsync(date, Currency.BRL, Currency.GBP);
        var second = await cache.GetHistoricalRateAsync(date, Currency.BRL, Currency.GBP);

        first.Should().BeNull();
        second.Should().BeNull();
        inner.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task GetHistoricalRateAsync_DifferentCurrencyPairSameDate_CallsInnerForEach()
    {
        var inner = new CountingExchangeRateProvider();
        var cache = new InMemoryCachedExchangeRateProvider(() => inner);
        var date = new DateOnly(2026, 1, 15);

        await cache.GetHistoricalRateAsync(date, Currency.BRL, Currency.GBP);
        await cache.GetHistoricalRateAsync(date, Currency.USD, Currency.GBP);

        inner.CallCount.Should().Be(2);
    }

    [Fact]
    public void Constructor_NullInnerFactory_Throws()
    {
        Action act = () => new InMemoryCachedExchangeRateProvider(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
