using Financial.CashFlow.Application.Configuration;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.DependencyInjection;
using Financial.Integrations.GoogleCalendar;
using Financial.Shared.Abstractions.Configuration;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.DependencyInjection;
using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Financial.CashFlow.Infrastructure.Configuration;

namespace Financial.CashFlow.Infrastructure.Tests.DependencyInjection;

[Trait("Category", "Integration")]
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

        var selection = provider.GetRequiredService<IOptions<CashFlowRepositorySettingsOptions>>().Value.ToSelection();

        selection.Should().Be(new JsonRepositorySelection(JsonRepositoryProvider.LocalJson, missingPath, null, null));
        provider.GetRequiredService<ICashFlowRepository>();
    }

    [Fact]
    public void AddFinancialCashFlowInfrastructure_GoogleDriveProviderWithoutCredentials_FailsNamingTheCashFlowCredentialsKey()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["CashFlow:Repository:Provider"] = "GoogleDriveJson"
        });

        Action act = () => provider.GetRequiredService<ICashFlowRepository>();

        act.Should().Throw<FileNotFoundException>().WithMessage("*CashFlow:GoogleDrive:CredentialsPath*");
    }

    [Fact]
    public void AddFinancialCashFlowInfrastructure_RegistersCalendarConnectionStoreAndProvider()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-di-{Guid.NewGuid()}.json");
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["CashFlow:DataJsonFile"] = missingPath
        });

        provider.GetRequiredService<ICalendarConnectionStore>().Should().NotBeNull();
        provider.GetRequiredService<ICalendarProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddFinancialCashFlowInfrastructure_BindsTheGoogleCalendarSettings()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["CashFlow:DataJsonFile"] = Path.Combine(Path.GetTempPath(), $"cashflow-di-{Guid.NewGuid()}.json"),
            [CashFlowGoogleCalendarConfigurationKeys.ClientId] = "client-id",
            [CashFlowGoogleCalendarConfigurationKeys.ClientSecret] = "client-secret",
            [CashFlowGoogleCalendarConfigurationKeys.RedirectUri] = "http://localhost/callback",
            [CashFlowGoogleCalendarConfigurationKeys.CredentialsPath] = "calendar.json"
        });

        var settings = provider.GetRequiredService<IOptions<GoogleCalendarSettingsOptions>>().Value;

        settings.Should().BeEquivalentTo(new
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            RedirectUri = "http://localhost/callback",
            CredentialsPath = "calendar.json"
        });
    }

    [Fact]
    public void AddFinancialCashFlowInfrastructure_OnItsOwn_RegistersTheSystemClock()
    {
        var services = new ServiceCollection();

        services.AddFinancialCashFlowInfrastructure(new ConfigurationBuilder().Build());

        services.Should().ContainSingle(d => d.ServiceType == typeof(TimeProvider)).Which.ImplementationInstance.Should().BeSameAs(TimeProvider.System);
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
        // AddFinancialCashFlowInfrastructure (see Program.cs/App.xaml.cs) - this minimal container
        // mirrors that invariant, matching how ShutdownFlushHostedService's own registration moved
        // out to the composition root too (F06/F08 of the shared-domain-structure refactor).
        services.AddSingleton<IJsonStorageFactory, JsonStorageFactory>();
        // The composition root also registers the Google Calendar OAuth client before calling
        // AddFinancialCashFlowInfrastructure (see Program.cs) - GoogleCalendarProviderAdapter
        // depends on it.
        services.AddGoogleCalendarOAuthClient();
        services.AddFinancialFxRateInfrastructure(configuration);
        services.AddFinancialCashFlowInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
