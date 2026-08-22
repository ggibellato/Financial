using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.DependencyInjection;
using Financial.Shared.Infrastructure.Hosting;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
    public void AddFinancialInfrastructure_RegistersInvestmentShutdownFlushHostedService()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Investment:DataJsonFile"] = TestDataPaths.DataJsonFile
        });

        var hostedServices = provider.GetServices<IHostedService>();

        hostedServices.Should().ContainSingle(service => service is ShutdownFlushHostedService<IInvestmentRepository>);
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
        services.AddFinancialInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
