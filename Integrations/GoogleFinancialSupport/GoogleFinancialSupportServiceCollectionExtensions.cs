using Financial.Shared.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Integrations.GoogleFinancialSupport;

public static class GoogleFinancialSupportServiceCollectionExtensions
{
    public static IServiceCollection AddGoogleDriveFileClient(this IServiceCollection services)
    {
        services.AddSingleton<IRemoteFileClientFactory, GoogleFileClientFactory>();
        return services;
    }
}
