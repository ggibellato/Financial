using System.IO;
using Financial.CashFlow.Application.DependencyInjection;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.DependencyInjection;
using Financial.Shared.Infrastructure.Observability;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Financial.Presentation.Tests.DependencyInjection;

public class ObservabilityServiceRegistrationTests
{
    [Fact]
    public void WithObservabilityDisabled_AppHostServicesStillResolve()
    {
        var provider = BuildServiceProvider(enabled: false);

        provider.GetRequiredService<IExpenseService>().Should().NotBeNull();
        provider.GetService<TracerProvider>().Should().BeNull();
        provider.GetService<MeterProvider>().Should().BeNull();
    }

    private static IServiceProvider BuildServiceProvider(bool enabled)
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"observability-presentation-di-{Guid.NewGuid()}.json");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CashFlow:Repository:Provider"] = "LocalJson",
                ["CashFlow:DataJsonFile"] = missingPath,
                ["Observability:Enabled"] = enabled ? "true" : "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddFinancialCashFlowApplication();
        services.AddFinancialCashFlowInfrastructure(configuration);
        services.AddFinancialObservability(configuration, serviceName: "Financial.App");

        return services.BuildServiceProvider();
    }
}
