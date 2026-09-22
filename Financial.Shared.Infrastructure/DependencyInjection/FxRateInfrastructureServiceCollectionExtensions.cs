using Financial.Shared.Abstractions.Configuration;
using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.Configuration;
using Financial.Shared.Infrastructure.Persistence.FxRates;
using Financial.Shared.Infrastructure.Repositories.FxRates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Financial.Shared.Infrastructure.DependencyInjection;

public static class FxRateInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialFxRateInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FxRateRepositorySettingsOptions>(options =>
        {
            options.Provider = configuration[FxRateRepositoryConfigurationKeys.Provider];
            options.DataJsonFile = configuration[FxRateRepositoryConfigurationKeys.LocalJsonDataFile];
            options.GoogleDriveCredentialsPath = configuration[FxRateRepositoryConfigurationKeys.GoogleDriveCredentialsPath];
            options.GoogleDriveFilePath = configuration[FxRateRepositoryConfigurationKeys.GoogleDriveFilePath];
        });
        services.AddSingleton<IFxRateSerializer, FxRateSerializerAdapter>();
        services.AddSingleton<IFxRateStore>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<FxRateRepositorySettingsOptions>>().Value;
            var options = BuildRepositoryOptions(settings);
            return new FxRateStoreFactory(
                sp.GetRequiredService<IFxRateSerializer>(),
                sp.GetRequiredService<IJsonStorageFactory>()).Create(options);
        });

        return services;
    }

    private static FxRateRepositorySelectionOptions BuildRepositoryOptions(FxRateRepositorySettingsOptions settings)
    {
        var provider = RepositoryProviderResolver.Resolve(settings.Provider, FxRateRepositoryProvider.LocalJson);

        return new FxRateRepositorySelectionOptions(
            provider,
            settings.DataJsonFile,
            settings.GoogleDriveCredentialsPath,
            settings.GoogleDriveFilePath);
    }
}
