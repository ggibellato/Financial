using Financial.Investment.Application.Configuration;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.Configuration;
using Financial.Investment.Infrastructure.Interfaces;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.Investment.Infrastructure.Services;
using Financial.Integrations.Frankfurter;
using Financial.Shared.Abstractions.Configuration;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Abstractions.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        services.AddSingleton<IInvestmentSerializer, InvestmentSerializerAdapter>();
        services.AddSingleton<IDividendDataSource, DividendDataSourceAdapter>();
        services.AddSingleton<IAssetSnapshotSource, AssetSnapshotSourceAdapter>();
        services.AddSingleton<GoogleFinanceService>();
        services.AddHttpClient<YahooFinanceService>();
        services.AddSingleton<IFinanceService>(sp => new FallbackFinanceService(
            sp.GetRequiredService<GoogleFinanceService>(),
            sp.GetRequiredService<YahooFinanceService>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FallbackFinanceService>>()));
        services.AddSingleton<StatusInvestFinanceService>();
        services.AddSingleton<DicionarioDoInvestidorFinanceService>();
        services.AddSingleton<RedentiaFinanceService>();
        services.AddSingleton<IAssetPriceFetcher, StandardAssetPriceFetcher>();
        services.AddSingleton<IAssetPriceFetcher, CryptocurrencyAssetPriceFetcher>();
        services.AddSingleton<IAssetPriceFetcher>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FallbackFinanceService>>();

            // dicionariodoinvestidor.com only lists bonds Tesouro Direto currently offers for new
            // purchase, so redentia.com.br - which covers every series, matured or not - sits behind
            // it as a second fallback rather than a replacement.
            var secondFallback = new FallbackFinanceService(
                sp.GetRequiredService<DicionarioDoInvestidorFinanceService>(),
                sp.GetRequiredService<RedentiaFinanceService>(),
                logger);

            return new BondAssetPriceFetcher(new FallbackFinanceService(
                sp.GetRequiredService<StatusInvestFinanceService>(),
                secondFallback,
                logger));
        });
        services.AddSingleton<IInvestmentRepository>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<InvestmentRepositorySettingsOptions>>().Value;
            var options = BuildRepositoryOptions(settings);
            return new InvestmentRepositoryFactory(
                sp.GetRequiredService<IInvestmentSerializer>(),
                sp.GetRequiredService<IJsonStorageFactory>()).Create(options);
        });
        services.AddSingleton<IAssetPriceService, AssetPriceService>();
        services.AddHttpClient<FrankfurterExchangeRateProvider>();
        // TryAdd: CashFlow's own AddFinancialCashFlowInfrastructure registers the same shared
        // IExchangeRateProvider - both bounded contexts are composed together in the same process,
        // so only the first registration to run should win, keeping a single shared cache instance.
        services.TryAddSingleton<IExchangeRateProvider>(sp =>
            new InMemoryCachedExchangeRateProvider(() => new UsdBasedExchangeRateProvider(
                sp.GetRequiredService<IFxRateStore>(),
                sp.GetRequiredService<FrankfurterExchangeRateProvider>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UsdBasedExchangeRateProvider>>())));

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
