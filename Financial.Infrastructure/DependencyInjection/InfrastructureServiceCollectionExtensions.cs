using Financial.Application.Interfaces;
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
        {
            var options = BuildRepositoryOptions(configuration);
            return new RepositoryFactory(sp.GetRequiredService<IInvestmentsSerializer>()).Create(options);
        });
        services.AddSingleton<IAssetPriceService, AssetPriceService>();

        return services;
    }

    private static RepositorySelectionOptions BuildRepositoryOptions(IConfiguration configuration)
    {
        var providerValue = configuration[RepositoryConfigurationKeys.Provider]
            ?? nameof(RepositoryProvider.LocalJson);

        if (!Enum.TryParse(providerValue, ignoreCase: true, out RepositoryProvider provider))
        {
            throw new InvalidOperationException(
                $"Repository provider '{providerValue}' is not supported. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<RepositoryProvider>())}.");
        }

        return new RepositorySelectionOptions(
            provider,
            configuration[RepositoryConfigurationKeys.LocalJsonDataFile],
            configuration[RepositoryConfigurationKeys.GoogleDriveCredentialsPath],
            configuration[RepositoryConfigurationKeys.GoogleDriveFilePath]);
    }
}
