using System.IO;
using Financial.CashFlow.Application.DependencyInjection;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.DependencyInjection;
using Financial.Integrations.Observability;
using Financial.Shared.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Presentation.Tests.DependencyInjection;

public class ObservabilityServiceRegistrationTests
{
    [Fact]
    public void CashFlowServicesStillResolve_AlongsideObservability()
    {
        var provider = BuildServiceProvider();

        provider.GetRequiredService<IExpenseService>().Should().NotBeNull();
        provider.GetRequiredService<IIncomeService>().Should().NotBeNull();
        provider.GetRequiredService<IBankService>().Should().NotBeNull();
    }

    [Fact]
    public void TelemetryTracer_ResolvesToUsableNoOp()
    {
        var provider = BuildServiceProvider();

        var tracer = provider.GetRequiredService<ITelemetryTracer>();
        using var span = tracer.StartSpan("Test.Span");
        span.SetAttribute("key", "value");
        span.RecordException(new InvalidOperationException("test"));
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"observability-presentation-di-{Guid.NewGuid()}.json");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CashFlow:Repository:Provider"] = "LocalJson",
                ["CashFlow:DataJsonFile"] = missingPath,
                ["Observability:Enabled"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddObservability(configuration, serviceName: "Financial.App");
        services.AddFinancialCashFlowApplication();
        services.AddFinancialCashFlowInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}
