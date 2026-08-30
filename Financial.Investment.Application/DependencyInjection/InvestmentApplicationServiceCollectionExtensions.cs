using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Investment.Application.DependencyInjection;

public static class InvestmentApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialApplication(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<CreditService>();
        services.AddSingleton<ICreditService>(sp => sp.GetRequiredService<CreditService>());
        services.AddSingleton<ICreditQueryService>(sp => sp.GetRequiredService<CreditService>());
        services.AddSingleton<TransactionService>();
        services.AddSingleton<ITransactionService>(sp => sp.GetRequiredService<TransactionService>());
        services.AddSingleton<ITransactionQueryService>(sp => sp.GetRequiredService<TransactionService>());
        services.AddSingleton<IAssetMoveService, AssetMoveService>();
        services.AddSingleton<IPortfolioService, PortfolioService>();
        services.AddSingleton<IBrokerService, BrokerService>();
        services.AddSingleton<IAssetPriceHistoryService, AssetPriceHistoryService>();
        services.AddSingleton<IAssetPriceLookupService, AssetPriceLookupService>();
        services.AddSingleton<IDividendService, DividendService>();
        services.AddSingleton<IBrokerBreakdownService, BrokerBreakdownService>();
        services.AddSingleton<IPortfolioAssetSummaryService, PortfolioAssetSummaryService>();
        services.AddSingleton<ISummaryService, SummaryService>();
        services.AddSingleton<IXirrCalculationService, XirrCalculationService>();
        services.AddSingleton<IProfitCalculationService, ProfitCalculationService>();

        return services;
    }
}
