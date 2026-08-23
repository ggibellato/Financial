using Financial.Shared.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Integrations.GoogleDrive;

public static class GoogleDriveServiceCollectionExtensions
{
    public static IServiceCollection AddGoogleDriveFileClient(this IServiceCollection services)
    {
        services.AddSingleton<IRemoteFileClientFactory, GoogleFileClientFactory>();
        return services;
    }
}
