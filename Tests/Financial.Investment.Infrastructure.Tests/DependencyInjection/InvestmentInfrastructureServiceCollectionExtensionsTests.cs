using System.Reflection;
using System.IO;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.DependencyInjection;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.DependencyInjection;
using Financial.Shared.Infrastructure.Persistence;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Investment.Infrastructure.Tests.DependencyInjection;

public class InvestmentInfrastructureServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFinancialInfrastructure_UnsupportedProvider_ThrowsOnRepositoryResolution()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Investment:Repository:Provider"] = "NotARealProvider",
            ["Investment:DataJsonFile"] = TestDataPaths.DataJsonFile
        });

        Action act = () => provider.GetRequiredService<IInvestmentRepository>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*NotARealProvider*is not supported*");
    }

    [Fact]
    public void AddFinancialInfrastructure_NoProviderConfigured_DefaultsToLocalJson()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Investment:DataJsonFile"] = TestDataPaths.DataJsonFile
        });

        var repository = provider.GetRequiredService<IInvestmentRepository>();

        repository.Should().NotBeNull();
    }

    [Fact]
    [Trait("AC", "P49-F01-shared-exchange-rate-provider-01")]
    public void AddFinancialInfrastructure_RegistersSharedExchangeRateProvider()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Investment:DataJsonFile"] = TestDataPaths.DataJsonFile
        });

        var exchangeRateProvider = provider.GetRequiredService<IExchangeRateProvider>();

        exchangeRateProvider.Should().NotBeNull();
        exchangeRateProvider.Should().BeOfType<InMemoryCachedExchangeRateProvider>();
    }

    [Fact]
    public void AddFinancialInfrastructure_ExchangeRateProvider_IsBackedByUsdBasedResolver()
    {
        var missingFxRatesPath = Path.Combine(Path.GetTempPath(), $"fxrates-di-{Guid.NewGuid()}.json");
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Investment:DataJsonFile"] = TestDataPaths.DataJsonFile,
            ["FxRates:DataJsonFile"] = missingFxRatesPath
        });

        var exchangeRateProvider = provider.GetRequiredService<IExchangeRateProvider>();
        var inner = InvokeInnerFactory(exchangeRateProvider);

        inner.Should().BeOfType<UsdBasedExchangeRateProvider>();
    }

    [Fact]
    public async Task AddFinancialInfrastructure_RateResolvedThroughFullChain_ReachesExistingCallerUnmodified()
    {
        var fxRatesPath = Path.Combine(Path.GetTempPath(), $"fxrates-di-{Guid.NewGuid()}.json");
        File.WriteAllText(fxRatesPath, """
            {"Version":1,"ratesByDate":{"2026-01-15":{"base":"USD","rates":{"BRL":5.0,"GBP":0.8},"source":"frankfurter","storedAt":"2026-01-15T23:59:59Z"}}}
            """);
        try
        {
            var provider = BuildServiceProvider(new Dictionary<string, string?>
            {
                ["Investment:DataJsonFile"] = TestDataPaths.DataJsonFile,
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

    private static IExchangeRateProvider InvokeInnerFactory(IExchangeRateProvider cachedProvider)
    {
        var field = typeof(InMemoryCachedExchangeRateProvider).GetField(
            "_innerFactory", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var factory = (Func<IExchangeRateProvider>)field.GetValue(cachedProvider)!;
        return factory();
    }

    private static IServiceProvider BuildServiceProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        // The composition roots always register a tracer via AddObservability (research.md D5);
        // this minimal container mirrors that invariant with the contract's null object.
        services.AddSingleton<Financial.Shared.Abstractions.Observability.ITelemetryTracer>(
            Financial.Shared.Abstractions.Observability.NoOpTelemetryTracer.Instance);
        // The composition roots also always register IJsonStorageFactory before calling
        // AddFinancialInfrastructure (see Program.cs/App.xaml.cs) - this minimal container mirrors
        // that invariant, matching how ShutdownFlushHostedService's own registration moved out to
        // the composition root too (F06/F07/F08 of the shared-domain-structure refactor).
        services.AddSingleton<IJsonStorageFactory, JsonStorageFactory>();
        services.AddFinancialFxRateInfrastructure(configuration);
        services.AddFinancialInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
