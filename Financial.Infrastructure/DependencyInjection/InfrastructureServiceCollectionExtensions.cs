using Financial.Application.Configuration;
using Financial.Application.Interfaces;
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
        services.AddSingleton<IRepositoryFactory, RepositoryFactory>();
        services.AddSingleton<IRepository>(sp => CreateRepository(sp, configuration));
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ITransactionService, TransactionService>();
        services.AddSingleton<ICreditService, CreditService>();
        services.AddSingleton<IAssetPriceService, AssetPriceService>();
        services.AddSingleton<IDividendService, DividendService>();

        return services;
    }

    private static IRepository CreateRepository(IServiceProvider sp, IConfiguration configuration)
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

        return sp.GetRequiredService<IRepositoryFactory>().Create(options);
    }
}
