using Financial.Application.Interfaces;
using Financial.Application.Services;
using Financial.Infrastructure.Configuration;
using Financial.Infrastructure.Persistence;
using Financial.Infrastructure.Repositories;
using Financial.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IRepositoryDiagnostics, RepositoryDiagnosticsProvider>();
        services.AddSingleton<IInvestmentsSerializer, InvestmentsSerializerAdapter>();
        services.AddSingleton<IDividendDataSource, DividendDataSourceAdapter>();
        services.AddSingleton<IAssetSnapshotSource, AssetSnapshotSourceAdapter>();
        services.AddSingleton<IRepository>(sp =>
            new RepositoryFactory(sp.GetRequiredService<IInvestmentsSerializer>())
                .CreateFromConfiguration(configuration));
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ITransactionService, TransactionService>();
        services.AddSingleton<ICreditService, CreditService>();
        services.AddSingleton<IAssetPriceService, AssetPriceService>();
        services.AddSingleton<IDividendService, DividendService>();

        return services;
    }
}
