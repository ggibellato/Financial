using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Financial.CashFlow.Application.DependencyInjection;

public static class CashFlowApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialCashFlowApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IExpenseService, ExpenseService>();
        services.AddSingleton<IReserveService, ReserveService>();
        services.AddSingleton<IPotsService, PotsService>();
        services.AddSingleton<IMensaisService, MensaisService>();
        services.AddSingleton<ISettlementService, SettlementService>();
        services.AddSingleton<IInvestmentSnapshotService, InvestmentSnapshotService>();
        services.AddSingleton<ICardStatementService, CardStatementService>();
        services.AddSingleton<IStatementDueDateService, StatementDueDateService>();
        services.AddSingleton<IIncomeSummaryService, IncomeSummaryService>();
        services.AddSingleton<ICategorySummaryService, CategorySummaryService>();
        services.AddSingleton<IInvestmentAnnualResultService, InvestmentAnnualResultService>();
        services.AddSingleton<IHistoricAverageService, HistoricAverageService>();
        services.AddSingleton<IIncomeSourceService, IncomeSourceService>();
        services.AddSingleton<ICategoryService, CategoryService>();
        services.AddSingleton<IInvestmentAccountService, InvestmentAccountService>();
        services.AddSingleton<IIncomeService, IncomeService>();
        services.AddSingleton<ITitheService, TitheService>();
        services.AddSingleton<IAccountTransferService, AccountTransferService>();
        services.AddSingleton<IAccountGroupSummaryService, AccountGroupSummaryService>();
        services.AddSingleton<IAccountQueryService, AccountQueryService>();
        services.AddSingleton<IAccountAdminService, AccountAdminService>();
        services.AddSingleton<IBalanceAdjustmentService, BalanceAdjustmentService>();
        services.AddSingleton<IPaymentsDueService, PaymentsDueService>();
        services.AddSingleton<ICalendarIntegrationService, CalendarIntegrationService>();
        services.AddSingleton<ICreditCardCalendarSyncStatusStore, CreditCardCalendarSyncStatusStore>();
        services.AddSingleton<ICreditCardCalendarSyncService, CreditCardCalendarSyncService>();

        return services;
    }
}
