using Financial.CashFlow.Application.Configuration;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.Configuration;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.Shared.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Financial.CashFlow.Infrastructure.DependencyInjection;

public static class CashFlowInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddFinancialCashFlowInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CashFlowRepositorySettingsOptions>(options =>
        {
            options.Provider = configuration[CashFlowRepositoryConfigurationKeys.Provider];
            options.DataJsonFile = configuration[CashFlowRepositoryConfigurationKeys.LocalJsonDataFile];
            options.GoogleDriveCredentialsPath = configuration[CashFlowRepositoryConfigurationKeys.GoogleDriveCredentialsPath];
            options.GoogleDriveFilePath = configuration[CashFlowRepositoryConfigurationKeys.GoogleDriveFilePath];
        });
        services.AddSingleton<ICashFlowSerializer, CashFlowSerializerAdapter>();
        services.AddSingleton<ICashFlowRepository>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<CashFlowRepositorySettingsOptions>>().Value;
            var options = BuildRepositoryOptions(settings);
            return new CashFlowRepositoryFactory(
                sp.GetRequiredService<ICashFlowSerializer>(),
                sp.GetService<IRemoteFileClientFactory>()).Create(options);
        });

        return services;
    }

    private static CashFlowRepositorySelectionOptions BuildRepositoryOptions(CashFlowRepositorySettingsOptions settings)
    {
        var providerValue = settings.Provider ?? nameof(CashFlowRepositoryProvider.LocalJson);

        if (!Enum.TryParse(providerValue, ignoreCase: true, out CashFlowRepositoryProvider provider))
        {
            throw new InvalidOperationException(
                $"Repository provider '{providerValue}' is not supported. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<CashFlowRepositoryProvider>())}.");
        }

        return new CashFlowRepositorySelectionOptions(
            provider,
            settings.DataJsonFile,
            settings.GoogleDriveCredentialsPath,
            settings.GoogleDriveFilePath);
    }
}
