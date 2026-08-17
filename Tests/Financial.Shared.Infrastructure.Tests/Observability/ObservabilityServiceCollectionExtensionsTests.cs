using Financial.Shared.Infrastructure.Observability;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Financial.Shared.Infrastructure.Tests.Observability;

public class ObservabilityServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFinancialObservability_WhenDisabled_RegistersNoTracerOrMeterProvider()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "false"
        });

        provider.GetService<TracerProvider>().Should().BeNull();
        provider.GetService<MeterProvider>().Should().BeNull();
    }

    [Fact]
    public void AddFinancialObservability_WhenNoConfigurationSectionPresent_RegistersNoTracerOrMeterProvider()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>());

        provider.GetService<TracerProvider>().Should().BeNull();
        provider.GetService<MeterProvider>().Should().BeNull();
    }

    [Fact]
    public void AddFinancialObservability_WhenEnabled_RegistersTracerAndMeterProvider()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Observability:Enabled"] = "true",
            ["Observability:Backend"] = "Jaeger",
            ["Observability:Endpoint"] = "http://localhost:4317"
        });

        provider.GetService<TracerProvider>().Should().NotBeNull();
        provider.GetService<MeterProvider>().Should().NotBeNull();
    }

    private static IServiceProvider BuildServiceProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddFinancialObservability(configuration, serviceName: "Financial.Tests");

        return services.BuildServiceProvider();
    }
}
