using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.DependencyInjection;
using Financial.Investment.Infrastructure.Hosting;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Financial.Investment.Infrastructure.Tests.DependencyInjection;

public class InfrastructureServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFinancialInfrastructure_UnsupportedProvider_ThrowsOnRepositoryResolution()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Investment:Repository:Provider"] = "NotARealProvider",
            ["Investment:DataJsonFile"] = TestDataPaths.DataJsonFile
        });

        Action act = () => provider.GetRequiredService<IRepository>();

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

        var repository = provider.GetRequiredService<IRepository>();

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

        hostedServices.Should().ContainSingle(service => service is InvestmentShutdownFlushHostedService);
    }

    private static IServiceProvider BuildServiceProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddFinancialInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
