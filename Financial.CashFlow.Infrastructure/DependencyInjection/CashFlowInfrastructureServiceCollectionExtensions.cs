using Financial.CashFlow.Application.Configuration;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.Configuration;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.CashFlow.Infrastructure.Services;
using Financial.Integrations.Frankfurter;
using Financial.Shared.Abstractions.Configuration;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Abstractions.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Financial.CashFlow.Infrastructure.DependencyInjection;

public static class CashFlowInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialCashFlowInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CashFlowRepositorySettingsOptions>(options =>
        {
            options.Provider = configuration[CashFlowRepositoryConfigurationKeys.Provider];
            options.DataJsonFile = configuration[CashFlowRepositoryConfigurationKeys.LocalJsonDataFile];
            options.GoogleDriveCredentialsPath = configuration[CashFlowRepositoryConfigurationKeys.GoogleDriveCredentialsPath];
            options.GoogleDriveFilePath = configuration[CashFlowRepositoryConfigurationKeys.GoogleDriveFilePath];
        });
        services.AddSingleton<ICashFlowSerializer, CashFlowSerializerAdapter>();
        services.AddHttpClient<FrankfurterExchangeRateProvider>();
        // TryAdd: Investment's own AddFinancialInfrastructure registers the same shared
        // IExchangeRateProvider - both bounded contexts are composed together in the same process,
        // so only the first registration to run should win, keeping a single shared cache instance.
        services.TryAddSingleton<IExchangeRateProvider>(sp =>
            new InMemoryCachedExchangeRateProvider(() => new UsdBasedExchangeRateProvider(
                sp.GetRequiredService<IFxRateStore>(),
                sp.GetRequiredService<FrankfurterExchangeRateProvider>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UsdBasedExchangeRateProvider>>())));
        services.AddSingleton<ICashFlowRepository>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<CashFlowRepositorySettingsOptions>>().Value;
            var options = BuildRepositoryOptions(settings);
            return new CashFlowRepositoryFactory(
                sp.GetRequiredService<ICashFlowSerializer>(),
                sp.GetRequiredService<IJsonStorageFactory>()).Create(options);
        });

        services.Configure<GoogleCalendarSettingsOptions>(options =>
        {
            options.ClientId = configuration[CashFlowGoogleCalendarConfigurationKeys.ClientId];
            options.ClientSecret = configuration[CashFlowGoogleCalendarConfigurationKeys.ClientSecret];
            options.RedirectUri = configuration[CashFlowGoogleCalendarConfigurationKeys.RedirectUri];
            options.CredentialsPath = configuration[CashFlowGoogleCalendarConfigurationKeys.CredentialsPath];
        });
        services.AddSingleton<ICalendarConnectionStore>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<GoogleCalendarSettingsOptions>>().Value;
            return new CalendarConnectionStore(settings.CredentialsPath);
        });
        services.AddSingleton<ICalendarProvider, GoogleCalendarProviderAdapter>();

        return services;
    }

    private static CashFlowRepositorySelectionOptions BuildRepositoryOptions(CashFlowRepositorySettingsOptions settings)
    {
        var provider = RepositoryProviderResolver.Resolve(settings.Provider, CashFlowRepositoryProvider.LocalJson);

        return new CashFlowRepositorySelectionOptions(
            provider,
            settings.DataJsonFile,
            settings.GoogleDriveCredentialsPath,
            settings.GoogleDriveFilePath);
    }
}
