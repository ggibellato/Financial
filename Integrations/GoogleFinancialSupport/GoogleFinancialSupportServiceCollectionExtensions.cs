using Financial.Shared.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;

public static class GoogleFinancialSupportServiceCollectionExtensions
{
    public static IServiceCollection AddGoogleDriveFileClient(this IServiceCollection services)
    {
        services.AddSingleton<IRemoteFileClientFactory, GoogleFileClientFactory>();
        return services;
    }
}
