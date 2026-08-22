using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.DependencyInjection;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        // AddFinancialInfrastructure (see Program.cs/App.xaml.cs) - this minimal container mirrors
        // that invariant, matching how ShutdownFlushHostedService's own registration moved out to
        // the composition root too (F06/F07/F08 of the shared-domain-structure refactor).
        services.AddSingleton<IJsonStorageFactory, JsonStorageFactory>();
        services.AddFinancialInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
