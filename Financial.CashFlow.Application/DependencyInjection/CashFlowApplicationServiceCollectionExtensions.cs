using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.CashFlow.Application.DependencyInjection;

public static class CashFlowApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialCashFlowApplication(this IServiceCollection services)
    {
        services.AddSingleton<IExpenseService, ExpenseService>();
        services.AddSingleton<IReserveService, ReserveService>();
        services.AddSingleton<IMensaisService, MensaisService>();
        services.AddSingleton<IControleMaeService, ControleMaeService>();
        services.AddSingleton<IInvestmentSnapshotService, InvestmentSnapshotService>();
        services.AddSingleton<ICardStatementService, CardStatementService>();

        return services;
    }
}
