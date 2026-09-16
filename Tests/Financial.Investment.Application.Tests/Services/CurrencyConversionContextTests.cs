using Financial.Investment.Application.Services;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Financial.Investment.Application.Tests.Services;

public class CurrencyConversionContextTests
{
    private static readonly DateOnly FirstDate = new(2025, 1, 1);
    private static readonly DateOnly SecondDate = new(2025, 2, 1);

    [Fact]
    public async Task ConvertAsync_SameCurrency_ReturnsNativeAmountAndMakesNoProviderCall()
    {
        var provider = new StubExchangeRateProvider(null);
        var context = new CurrencyConversionContext(Currency.BRL, Currency.BRL, provider);

        var result = await context.ConvertAsync(50m, FirstDate);

        using var _ = new AssertionScope();
        result.Should().Be(50m);
        provider.CallCount.Should().Be(0);
        context.AttemptCount.Should().Be(0);
        context.IsPartial.Should().BeFalse();
        context.IsUnavailable.Should().BeFalse();
    }

    [Fact]
    public async Task ConvertAsync_CachesOneRateCallPerDistinctDate()
    {
        var provider = new StubExchangeRateProvider(0.2m);
        var context = new CurrencyConversionContext(Currency.BRL, Currency.GBP, provider);

        await context.ConvertAsync(50m, FirstDate);
        await context.ConvertAsync(20m, FirstDate);
        await context.ConvertAsync(10m, SecondDate);

        using var _ = new AssertionScope();
        provider.CallCount.Should().Be(2);
        context.AttemptCount.Should().Be(2);
    }

    [Fact]
    public async Task ConvertAsync_AppliesTheRateOfEachRecordsOwnDate()
    {
        var provider = new FakeExchangeRateProvider((date, _, _) => date == FirstDate ? 0.2m : 0.5m);
        var context = new CurrencyConversionContext(Currency.BRL, Currency.GBP, provider);

        var first = await context.ConvertAsync(100m, FirstDate);
        var second = await context.ConvertAsync(100m, SecondDate);

        using var _ = new AssertionScope();
        first.Should().Be(20m);
        second.Should().Be(50m);
    }

    [Fact]
    public async Task ConvertAsync_WhenTheLookupFails_ReturnsNull()
    {
        var provider = new StubExchangeRateProvider(null);
        var context = new CurrencyConversionContext(Currency.BRL, Currency.GBP, provider);

        var result = await context.ConvertAsync(50m, FirstDate);

        result.Should().BeNull();
    }

    [Fact]
    public async Task IsPartial_TrueWhenSomeLookupsFailAndOthersSucceed()
    {
        var provider = new FakeExchangeRateProvider((date, _, _) => date == FirstDate ? 0.2m : null);
        var context = new CurrencyConversionContext(Currency.BRL, Currency.GBP, provider);

        await context.ConvertAsync(50m, FirstDate);
        await context.ConvertAsync(50m, SecondDate);

        using var _ = new AssertionScope();
        context.IsPartial.Should().BeTrue();
        context.IsUnavailable.Should().BeFalse();
    }

    [Fact]
    public async Task IsUnavailable_TrueOnlyWhenEveryAttemptFails()
    {
        var provider = new StubExchangeRateProvider(null);
        var context = new CurrencyConversionContext(Currency.BRL, Currency.GBP, provider);

        await context.ConvertAsync(50m, FirstDate);
        await context.ConvertAsync(50m, SecondDate);

        using var _ = new AssertionScope();
        context.IsUnavailable.Should().BeTrue();
        context.IsPartial.Should().BeTrue();
    }

    [Fact]
    public void IsUnavailable_FalseBeforeAnyConversionIsAttempted()
    {
        var context = new CurrencyConversionContext(Currency.BRL, Currency.GBP, new StubExchangeRateProvider(null));

        using var _ = new AssertionScope();
        context.IsUnavailable.Should().BeFalse();
        context.IsPartial.Should().BeFalse();
    }
}
