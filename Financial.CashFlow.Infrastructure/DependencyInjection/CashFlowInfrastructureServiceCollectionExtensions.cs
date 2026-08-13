using Financial.CashFlow.Application.Configuration;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.Configuration;
using Financial.CashFlow.Infrastructure.Hosting;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.CashFlow.Infrastructure.Services;
using Financial.Shared.Infrastructure.Configuration;
using Financial.Shared.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Financial.CashFlow.Infrastructure.DependencyInjection;

public static class CashFlowInfrastructureServiceCollectionExtensions
{
    private const string FrankfurterBaseAddress = "https://api.frankfurter.app/";

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
        services.AddHttpClient<IExchangeRateProvider, FrankfurterExchangeRateProvider>(client =>
        {
            client.BaseAddress = new Uri(FrankfurterBaseAddress);
        });
        services.AddSingleton<ICashFlowRepository>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<CashFlowRepositorySettingsOptions>>().Value;
            var options = BuildRepositoryOptions(settings);
            return new CashFlowRepositoryFactory(
                sp.GetRequiredService<ICashFlowSerializer>(),
                sp.GetService<IRemoteFileClientFactory>()).Create(options);
        });
        services.AddHostedService<CashFlowShutdownFlushHostedService>();

        return services;
    }

    private static CashFlowRepositorySelectionOptions BuildRepositoryOptions(CashFlowRepositorySettingsOptions settings)
    {
        var provider = RepositoryProviderResolver.Resolve(settings.Provider, CashFlowRepositoryProvider.LocalJson);

        return new CashFlowRepositorySelectionOptions(
            provider,
            settings.DataJsonFile,
            settings.GoogleDriveCredentialsPath,
            settings.GoogleDriveFilePath);
    }
}
