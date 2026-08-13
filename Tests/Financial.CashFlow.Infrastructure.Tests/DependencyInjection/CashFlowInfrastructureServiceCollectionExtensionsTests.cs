using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.DependencyInjection;
using Financial.CashFlow.Infrastructure.Hosting;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
    public void AddFinancialCashFlowInfrastructure_RegistersCashFlowShutdownFlushHostedService()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-di-{Guid.NewGuid()}.json");
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["CashFlow:DataJsonFile"] = missingPath
        });

        var hostedServices = provider.GetServices<IHostedService>();

        hostedServices.Should().ContainSingle(service => service is CashFlowShutdownFlushHostedService);
    }

    private static IServiceProvider BuildServiceProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddFinancialCashFlowInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
