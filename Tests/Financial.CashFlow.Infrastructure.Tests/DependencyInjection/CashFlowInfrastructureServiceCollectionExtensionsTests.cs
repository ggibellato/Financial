using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.DependencyInjection;
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
