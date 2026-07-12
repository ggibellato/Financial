using Financial.Application.Interfaces;
using Financial.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialApplication(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ICreditService, CreditService>();
        services.AddSingleton<ICreditQueryService, CreditQueryService>();
        services.AddSingleton<ITransactionService, TransactionService>();
        services.AddSingleton<ITransactionQueryService, TransactionQueryService>();
        services.AddSingleton<IDividendService, DividendService>();
        services.AddSingleton<IBrokerBreakdownQueryService, BrokerBreakdownQueryService>();
        services.AddSingleton<IPortfolioAssetSummaryQueryService, PortfolioAssetSummaryQueryService>();
        services.AddSingleton<ISummaryQueryService, SummaryQueryService>();

        return services;
    }
}
