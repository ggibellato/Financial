using Financial.Application.Interfaces;
using Financial.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialApplication(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ICreditQueryService, CreditQueryService>();
        services.AddSingleton<ITransactionService, TransactionService>();
        services.AddSingleton<ICreditService, CreditService>();
        services.AddSingleton<IDividendService, DividendService>();

        return services;
    }
}
