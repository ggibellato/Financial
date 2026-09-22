using System.Reflection;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.DependencyInjection;
using Financial.Integrations.GoogleCalendar;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.DependencyInjection;
using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.CashFlow.Infrastructure.Tests.DependencyInjection;

public class CashFlowInfrastructureServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFinancialCashFlowInfrastructure_UnsupportedProvider_ThrowsOnRepositoryResolution()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-di-{Guid.NewGuid()}.json");
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["CashFlow:Repository:Provider"] = "NotARealProvider",
            ["CashFlow:DataJsonFile"] = missingPath
        });

        Action act = () => provider.GetRequiredService<ICashFlowRepository>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*NotARealProvider*is not supported*");
    }

    [Fact]
    public void AddFinancialCashFlowInfrastructure_NoProviderConfigured_DefaultsToLocalJson()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-di-{Guid.NewGuid()}.json");
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["CashFlow:DataJsonFile"] = missingPath
        });

        var repository = provider.GetRequiredService<ICashFlowRepository>();

        repository.Should().NotBeNull();
    }

    [Fact]
    public void AddFinancialCashFlowInfrastructure_RegistersCalendarConnectionStoreAndProvider()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-di-{Guid.NewGuid()}.json");
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["CashFlow:DataJsonFile"] = missingPath
        });

        provider.GetRequiredService<ICalendarConnectionStore>().Should().NotBeNull();
        provider.GetRequiredService<ICalendarProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddFinancialCashFlowInfrastructure_RegistersCachedExchangeRateProvider()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-di-{Guid.NewGuid()}.json");
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["CashFlow:DataJsonFile"] = missingPath
        });

        var exchangeRateProvider = provider.GetRequiredService<IExchangeRateProvider>();

        exchangeRateProvider.Should().BeOfType<InMemoryCachedExchangeRateProvider>();
    }

    [Fact]
    public void AddFinancialCashFlowInfrastructure_ExchangeRateProvider_IsBackedByUsdBasedResolver()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-di-{Guid.NewGuid()}.json");
        var missingFxRatesPath = Path.Combine(Path.GetTempPath(), $"fxrates-di-{Guid.NewGuid()}.json");
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["CashFlow:DataJsonFile"] = missingPath,
            ["FxRates:DataJsonFile"] = missingFxRatesPath
        });

        var exchangeRateProvider = provider.GetRequiredService<IExchangeRateProvider>();
        var inner = InvokeInnerFactory(exchangeRateProvider);

        inner.Should().BeOfType<UsdBasedExchangeRateProvider>();
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
        // AddFinancialCashFlowInfrastructure (see Program.cs/App.xaml.cs) - this minimal container
        // mirrors that invariant, matching how ShutdownFlushHostedService's own registration moved
        // out to the composition root too (F06/F08 of the shared-domain-structure refactor).
        services.AddSingleton<IJsonStorageFactory, JsonStorageFactory>();
        // The composition root also registers the Google Calendar OAuth client before calling
        // AddFinancialCashFlowInfrastructure (see Program.cs) - GoogleCalendarProviderAdapter
        // depends on it.
        services.AddGoogleCalendarOAuthClient();
        services.AddFinancialFxRateInfrastructure(configuration);
        services.AddFinancialCashFlowInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
