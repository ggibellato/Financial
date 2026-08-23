using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Persistence;

namespace Financial.Shared.Infrastructure.Persistence;

public static class RemoteStorageFactory
{
    public static IJsonStorage Create(
        string? credentialsPath,
        string? remoteFilePath,
        IRemoteFileClientFactory? remoteFileClientFactory,
        string credentialsConfigKey,
        string providerName,
        ITelemetryTracer? tracer = null)
    {
        var resolvedCredentialsPath = ResolveCredentialsPath(credentialsPath, credentialsConfigKey);

        if (remoteFileClientFactory is null)
        {
            throw new InvalidOperationException(
                $"Repository provider '{providerName}' requires an {nameof(IRemoteFileClientFactory)} " +
                "to be registered by the composition root (Financial.Api / Financial.App).");
        }

        var client = remoteFileClientFactory.Create(resolvedCredentialsPath);
        return new RemoteJsonStorage(client, remoteFilePath, tracer);
    }

    private static string ResolveCredentialsPath(string? credentialsPath, string credentialsConfigKey)
    {
        if (string.IsNullOrWhiteSpace(credentialsPath))
        {
            throw new FileNotFoundException(
                $"Remote storage credentials file path is required. Configure '{credentialsConfigKey}'.");
        }

        var resolvedPath = PathResolution.ResolveRelativeToBaseDirectory(credentialsPath);

        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException(
                $"Remote storage credentials file not found at '{resolvedPath}'. Configure '{credentialsConfigKey}'.",
                resolvedPath);
        }

        return resolvedPath;
    }
}
