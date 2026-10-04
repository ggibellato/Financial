using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests;

public class ApiTestFactoryExchangeRateTests
{
    [Fact]
    public void DefaultFactory_ResolvesDeterministicExchangeRateProvider()
    {
        using var factory = new ApiTestFactory();

        var provider = factory.Services.GetRequiredService<IExchangeRateProvider>();

        provider.Should().BeOfType<DeterministicExchangeRateProvider>();
    }

    [Theory]
    [InlineData(Currency.GBP, Currency.BRL, 6.25)]
    [InlineData(Currency.BRL, Currency.GBP, 0.16)]
    [InlineData(Currency.USD, Currency.GBP, 0.8)]
    [InlineData(Currency.GBP, Currency.GBP, 1)]
    public async Task DefaultFactory_ConvertsWithTheCommittedRates(Currency from, Currency to, double expected)
    {
        using var factory = new ApiTestFactory();
        var provider = factory.Services.GetRequiredService<IExchangeRateProvider>();

        var rate = await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 1), from, to);

        rate.Should().Be((decimal)expected);
    }

    [Fact]
    public void WithRealExchangeRates_ResolvesTheProductionChainWithoutCallingIt()
    {
        using var factory = new ApiTestFactory().WithRealExchangeRates();

        var provider = factory.Services.GetRequiredService<IExchangeRateProvider>();

        provider.Should().NotBeOfType<DeterministicExchangeRateProvider>();
        provider.Should().NotBeOfType<StubExchangeRateProvider>();
    }

    [Fact]
    public void ExplicitProviderOverride_TakesPrecedenceOverTheDefault()
    {
        var stub = new StubExchangeRateProvider(1.5m);
        using var factory = new ApiTestFactory(stub);

        var provider = factory.Services.GetRequiredService<IExchangeRateProvider>();

        provider.Should().BeSameAs(stub);
    }
}
