using Financial.Application.Interfaces;
using Financial.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
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
        services.AddSingleton<IDividendService, DividendService>();
        services.AddSingleton<IActiveBrokerBreakdownService, ActiveBrokerBreakdownService>();
        services.AddSingleton<IHistoricBrokerBreakdownService, HistoricBrokerBreakdownService>();
        services.AddSingleton<IBrokerBreakdownService, BrokerBreakdownService>();
        services.AddSingleton<IActivePortfolioAssetSummaryService, ActivePortfolioAssetSummaryService>();
        services.AddSingleton<IHistoricPortfolioAssetSummaryService, HistoricPortfolioAssetSummaryService>();
        services.AddSingleton<IPortfolioAssetSummaryService, PortfolioAssetSummaryService>();
        services.AddSingleton<ISummaryService, SummaryService>();
        services.AddSingleton<IXirrCalculationService, XirrCalculationService>();

        return services;
    }
}
