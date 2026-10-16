using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.DependencyInjection;
using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Shared.Infrastructure.Tests.DependencyInjection;

[Trait("Category", "Integration")]
public class FxRateInfrastructureServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFinancialFxRateInfrastructure_UnsupportedProvider_ThrowsOnResolution()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"fxrates-di-{Guid.NewGuid()}.json");
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["FxRates:Repository:Provider"] = "NotARealProvider",
            ["FxRates:DataJsonFile"] = missingPath
        });

        Action act = () => provider.GetRequiredService<IFxRateStore>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*NotARealProvider*is not supported*");
    }

    [Fact]
    public void AddFinancialFxRateInfrastructure_NoProviderConfigured_DefaultsToLocalJson()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"fxrates-di-{Guid.NewGuid()}.json");
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["FxRates:DataJsonFile"] = missingPath
        });

        var store = provider.GetRequiredService<IFxRateStore>();

        store.Should().NotBeNull();
        store.TryGetRate(new DateOnly(2026, 9, 18)).Should().BeNull();
    }

    [Fact]
    public void AddFinancialFxRateInfrastructure_RegistersSingleton()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"fxrates-di-{Guid.NewGuid()}.json");
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["FxRates:DataJsonFile"] = missingPath
        });

        var first = provider.GetRequiredService<IFxRateStore>();
        var second = provider.GetRequiredService<IFxRateStore>();

        first.Should().BeSameAs(second);
    }

    [Fact]
    [Trait("AC", "P49-F01-shared-exchange-rate-provider-01")]
    public void AddFinancialFxRateInfrastructure_RegistersSharedCachedExchangeRateProvider()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"fxrates-di-{Guid.NewGuid()}.json");
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["FxRates:DataJsonFile"] = missingPath
        });

        var first = provider.GetRequiredService<IExchangeRateProvider>();
        var second = provider.GetRequiredService<IExchangeRateProvider>();

        first.Should().BeOfType<InMemoryCachedExchangeRateProvider>();
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void AddFinancialFxRateInfrastructure_ExchangeRateProvider_IsBackedByUsdBasedResolver()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"fxrates-di-{Guid.NewGuid()}.json");
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["FxRates:DataJsonFile"] = missingPath
        });

        var exchangeRateProvider = provider.GetRequiredService<IExchangeRateProvider>();
        var inner = InMemoryCachedExchangeRateProviderInspector.InvokeInnerFactory(exchangeRateProvider);

        inner.Should().BeOfType<UsdBasedExchangeRateProvider>();
    }

    [Fact]
    public async Task AddFinancialFxRateInfrastructure_RateResolvedThroughFullChain_ReturnsStoredRate()
    {
        var fxRatesPath = Path.Combine(Path.GetTempPath(), $"fxrates-di-{Guid.NewGuid()}.json");
        File.WriteAllText(fxRatesPath, """
            {"Version":1,"ratesByDate":{"2026-01-15":{"base":"USD","rates":{"BRL":5.0,"GBP":0.8},"source":"frankfurter","storedAt":"2026-01-15T23:59:59Z"}}}
            """);
        try
        {
            var provider = BuildServiceProvider(new Dictionary<string, string?>
            {
                ["FxRates:DataJsonFile"] = fxRatesPath
            });
            var exchangeRateProvider = provider.GetRequiredService<IExchangeRateProvider>();

            var rate = await exchangeRateProvider.GetHistoricalRateAsync(
                new DateOnly(2026, 1, 15), Currency.USD, Currency.BRL);

            rate.Should().Be(5.0m);
        }
        finally
        {
            File.Delete(fxRatesPath);
        }
    }

    private static IServiceProvider BuildServiceProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<ITelemetryTracer>(NoOpTelemetryTracer.Instance);
        services.AddSingleton<IJsonStorageFactory, JsonStorageFactory>();
        services.AddFinancialFxRateInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
