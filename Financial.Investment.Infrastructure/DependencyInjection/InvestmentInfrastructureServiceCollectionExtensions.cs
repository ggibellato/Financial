using Financial.Investment.Application.Configuration;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.Configuration;
using Financial.Investment.Infrastructure.Interfaces;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.Investment.Infrastructure.Services;
using Financial.Shared.Abstractions.Configuration;
using Financial.Shared.Abstractions.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Financial.Investment.Infrastructure.DependencyInjection;

public static class InvestmentInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<InvestmentRepositorySettingsOptions>(options =>
        {
            options.Provider = configuration[InvestmentRepositoryConfigurationKeys.Provider];
            options.DataJsonFile = configuration[InvestmentRepositoryConfigurationKeys.LocalJsonDataFile];
            options.GoogleDriveCredentialsPath = configuration[InvestmentRepositoryConfigurationKeys.GoogleDriveCredentialsPath];
            options.GoogleDriveFilePath = configuration[InvestmentRepositoryConfigurationKeys.GoogleDriveFilePath];
        });
        services.AddSingleton<IInvestmentsSerializer, InvestmentsSerializerAdapter>();
        services.AddSingleton<IDividendDataSource, DividendDataSourceAdapter>();
        services.AddSingleton<IAssetSnapshotSource, AssetSnapshotSourceAdapter>();
        services.AddSingleton<GoogleFinanceService>();
        services.AddHttpClient<YahooFinanceService>();
        services.AddSingleton<IFinanceService>(sp => new FallbackFinanceService(
            sp.GetRequiredService<GoogleFinanceService>(),
            sp.GetRequiredService<YahooFinanceService>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FallbackFinanceService>>()));
        services.AddSingleton<StatusInvestFinanceService>();
        services.AddSingleton<IAssetPriceFetcher, StandardAssetPriceFetcher>();
        services.AddSingleton<IAssetPriceFetcher, CryptocurrencyAssetPriceFetcher>();
        services.AddSingleton<IAssetPriceFetcher>(sp =>
            new BondAssetPriceFetcher(sp.GetRequiredService<StatusInvestFinanceService>()));
        services.AddSingleton<IInvestmentRepository>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<InvestmentRepositorySettingsOptions>>().Value;
            var options = BuildRepositoryOptions(settings);
            return new InvestmentRepositoryFactory(
                sp.GetRequiredService<IInvestmentsSerializer>(),
                sp.GetRequiredService<IJsonStorageFactory>()).Create(options);
        });
        services.AddSingleton<IAssetPriceService, AssetPriceService>();

        return services;
    }

    private static InvestmentRepositorySelectionOptions BuildRepositoryOptions(InvestmentRepositorySettingsOptions settings)
    {
        var provider = RepositoryProviderResolver.Resolve(settings.Provider, InvestmentRepositoryProvider.LocalJson);

        return new InvestmentRepositorySelectionOptions(
            provider,
            settings.DataJsonFile,
            settings.GoogleDriveCredentialsPath,
            settings.GoogleDriveFilePath);
    }
}
