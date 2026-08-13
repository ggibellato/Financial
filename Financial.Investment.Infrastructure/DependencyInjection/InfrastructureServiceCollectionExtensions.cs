using Financial.Investment.Application.Configuration;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.Configuration;
using Financial.Investment.Infrastructure.Hosting;
using Financial.Investment.Infrastructure.Interfaces;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.Investment.Infrastructure.Services;
using Financial.Shared.Infrastructure.Configuration;
using Financial.Shared.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Financial.Investment.Infrastructure.DependencyInjection;

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
        services.AddSingleton<GoogleFinanceService>();
        services.AddHttpClient<YahooFinanceService>();
        services.AddSingleton<IFinanceService>(sp => new FallbackFinanceService(
            sp.GetRequiredService<GoogleFinanceService>(),
            sp.GetRequiredService<YahooFinanceService>()));
        services.AddSingleton<StatusInvestFinanceService>();
        services.AddSingleton<IAssetPriceFetcher, StandardAssetPriceFetcher>();
        services.AddSingleton<IAssetPriceFetcher, CryptocurrencyAssetPriceFetcher>();
        services.AddSingleton<IAssetPriceFetcher>(sp =>
            new BondAssetPriceFetcher(sp.GetRequiredService<StatusInvestFinanceService>()));
        services.AddSingleton<IRepository>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<RepositorySettingsOptions>>().Value;
            var options = BuildRepositoryOptions(settings);
            return new RepositoryFactory(
                sp.GetRequiredService<IInvestmentsSerializer>(),
                sp.GetService<IRemoteFileClientFactory>()).Create(options);
        });
        services.AddSingleton<IAssetPriceService, AssetPriceService>();
        services.AddHostedService<InvestmentShutdownFlushHostedService>();

        return services;
    }

    private static RepositorySelectionOptions BuildRepositoryOptions(RepositorySettingsOptions settings)
    {
        var provider = RepositoryProviderResolver.Resolve(settings.Provider, RepositoryProvider.LocalJson);

        return new RepositorySelectionOptions(
            provider,
            settings.DataJsonFile,
            settings.GoogleDriveCredentialsPath,
            settings.GoogleDriveFilePath);
    }
}
