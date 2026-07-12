using Financial.Application.Configuration;
using Financial.Application.Interfaces;
using Financial.Infrastructure.Configuration;
using Financial.Infrastructure.Interfaces;
using Financial.Infrastructure.Persistence;
using Financial.Infrastructure.Repositories;
using Financial.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Financial.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RepositorySettingsOptions>(options =>
        {
            options.Provider = configuration[RepositoryConfigurationKeys.Provider];
            options.DataJsonFile = configuration[RepositoryConfigurationKeys.LocalJsonDataFile];
            options.GoogleDriveCredentialsPath = configuration[RepositoryConfigurationKeys.GoogleDriveCredentialsPath];
            options.GoogleDriveFilePath = configuration[RepositoryConfigurationKeys.GoogleDriveFilePath];
        });
        services.AddSingleton<IInvestmentsSerializer, InvestmentsSerializerAdapter>();
        services.AddSingleton<IDividendDataSource, DividendDataSourceAdapter>();
        services.AddSingleton<IAssetSnapshotSource, AssetSnapshotSourceAdapter>();
        services.AddSingleton<IAssetPriceFetcher, StandardAssetPriceFetcher>();
        services.AddSingleton<IAssetPriceFetcher, CryptocurrencyAssetPriceFetcher>();
        services.AddSingleton<IRepository>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<RepositorySettingsOptions>>().Value;
            var options = BuildRepositoryOptions(settings);
            return new RepositoryFactory(sp.GetRequiredService<IInvestmentsSerializer>()).Create(options);
        });
        services.AddSingleton<IAssetPriceService, AssetPriceService>();

        return services;
    }

    private static RepositorySelectionOptions BuildRepositoryOptions(RepositorySettingsOptions settings)
    {
        var providerValue = settings.Provider ?? nameof(RepositoryProvider.LocalJson);

        if (!Enum.TryParse(providerValue, ignoreCase: true, out RepositoryProvider provider))
        {
            throw new InvalidOperationException(
                $"Repository provider '{providerValue}' is not supported. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<RepositoryProvider>())}.");
        }

        return new RepositorySelectionOptions(
            provider,
            settings.DataJsonFile,
            settings.GoogleDriveCredentialsPath,
            settings.GoogleDriveFilePath);
    }
}
