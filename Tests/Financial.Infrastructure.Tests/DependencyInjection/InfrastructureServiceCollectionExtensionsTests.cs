using Financial.Investment.Application.Interfaces;
using Financial.Infrastructure.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Infrastructure.Tests.DependencyInjection;

public class InfrastructureServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFinancialInfrastructure_UnsupportedProvider_ThrowsOnRepositoryResolution()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Repository:Provider"] = "NotARealProvider",
            ["DataJsonFile"] = TestDataPaths.DataJsonFile
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
            ["DataJsonFile"] = TestDataPaths.DataJsonFile
        });

        var repository = provider.GetRequiredService<IRepository>();

        repository.Should().NotBeNull();
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
