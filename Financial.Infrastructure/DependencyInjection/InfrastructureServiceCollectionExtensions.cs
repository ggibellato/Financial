using Financial.Application.Interfaces;
using Financial.Application.Services;
using Financial.Infrastructure.Configuration;
using Financial.Infrastructure.Persistence;
using Financial.Infrastructure.Repositories;
using Financial.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

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
        services.AddSingleton<IRepository>(sp => CreateRepository(configuration));
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ITransactionService, TransactionService>();
        services.AddSingleton<ICreditService, CreditService>();
        services.AddSingleton<IAssetPriceService, AssetPriceService>();
        services.AddSingleton<IDividendService, DividendService>();

        return services;
    }

    private static IRepository CreateRepository(IConfiguration configuration)
    {
        var providerValue = configuration[RepositoryConfigurationKeys.Provider]
            ?? nameof(RepositoryProvider.LocalJson);

        if (!Enum.TryParse(providerValue, true, out RepositoryProvider provider))
        {
            throw new InvalidOperationException(
                $"Repository provider '{providerValue}' is not supported. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<RepositoryProvider>())}.");
        }

        var options = new RepositorySelectionOptions(
            provider,
            configuration[RepositoryConfigurationKeys.LocalJsonDataFile],
            configuration[RepositoryConfigurationKeys.GoogleDriveCredentialsPath],
            configuration[RepositoryConfigurationKeys.GoogleDriveFilePath]);

        return new RepositoryFactory().Create(options);
    }
}
