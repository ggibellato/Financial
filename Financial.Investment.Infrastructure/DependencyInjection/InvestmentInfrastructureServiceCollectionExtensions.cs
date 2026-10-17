using Financial.Investment.Application.Configuration;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.Configuration;
using Financial.Investment.Infrastructure.Interfaces;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.Investment.Infrastructure.Services;
using Financial.Shared.Abstractions.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Financial.Investment.Infrastructure.DependencyInjection;

public static class InvestmentInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<InvestmentRepositorySettingsOptions>(options =>
            options.Load(InvestmentRepositorySettingsOptions.ConfigurationKeys, key => configuration[key]));
        services.Configure<PriceQuoteOptions>(options => options.Load(key => configuration[key]));
        services.AddSingleton<IInvestmentSerializer, InvestmentSerializerAdapter>();
        services.AddSingleton<IDividendDataSource, DividendDataSourceAdapter>();
        services.AddSingleton<IAssetSnapshotSource, AssetSnapshotSourceAdapter>();
        services.AddSingleton<GoogleFinanceService>();
        services.AddHttpClient<YahooFinanceService>();
        services.AddSingleton<IFinanceService>(sp => CachedQuotes(sp, Fallback(
            sp,
            sp.GetRequiredService<GoogleFinanceService>(),
            sp.GetRequiredService<YahooFinanceService>())));
        services.AddSingleton<StatusInvestFinanceService>();
        services.AddSingleton<TesouroTransparenteFinanceService>();
        services.AddSingleton<RedentiaFinanceService>();
        services.AddSingleton<IAssetPriceFetcher, StandardAssetPriceFetcher>();
        services.AddSingleton<IAssetPriceFetcher, CryptocurrencyAssetPriceFetcher>();
        services.AddSingleton<IAssetPriceFetcher>(sp =>
        {
            var secondFallback = Fallback(
                sp,
                sp.GetRequiredService<TesouroTransparenteFinanceService>(),
                sp.GetRequiredService<RedentiaFinanceService>());

            return new BondAssetPriceFetcher(CachedQuotes(sp, Fallback(
                sp,
                sp.GetRequiredService<StatusInvestFinanceService>(),
                secondFallback)));
        });
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IInvestmentRepository>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<InvestmentRepositorySettingsOptions>>().Value.ToSelection();
            return new InvestmentRepositoryFactory(
                sp.GetRequiredService<IInvestmentSerializer>(),
                sp.GetRequiredService<IJsonStorageFactory>(),
                sp.GetRequiredService<TimeProvider>()).Create(options);
        });
        services.AddSingleton<IAssetPriceService, AssetPriceService>();

        return services;
    }

    private static FallbackFinanceService Fallback(IServiceProvider sp, IFinanceService primary, IFinanceService fallback) =>
        new(
            primary,
            fallback,
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<IOptions<PriceQuoteOptions>>().Value.ProviderFailureCooldown,
            sp.GetRequiredService<ILogger<FallbackFinanceService>>());

    private static CachingFinanceService CachedQuotes(IServiceProvider sp, IFinanceService inner) =>
        new(
            inner,
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<IOptions<PriceQuoteOptions>>().Value.CacheDuration);
}
