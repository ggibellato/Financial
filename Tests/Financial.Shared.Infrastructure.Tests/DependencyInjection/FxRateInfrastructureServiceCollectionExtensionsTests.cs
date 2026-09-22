using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.DependencyInjection;
using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Shared.Infrastructure.Tests.DependencyInjection;

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
