using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Persistence;

namespace Financial.Shared.Infrastructure.Persistence;

public static class GoogleDriveStorageFactory
{
    public static IJsonStorage Create(
        string? credentialsPath,
        string? driveFilePath,
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
                "to be registered (see AddGoogleDriveFileClient).");
        }

        var client = remoteFileClientFactory.Create(resolvedCredentialsPath);
        return new GoogleDriveJsonStorage(client, driveFilePath, tracer);
    }

    private static string ResolveCredentialsPath(string? credentialsPath, string credentialsConfigKey)
    {
        if (string.IsNullOrWhiteSpace(credentialsPath))
        {
            throw new FileNotFoundException(
                $"Google Drive credentials file path is required. Configure '{credentialsConfigKey}'.");
        }

        var resolvedPath = PathResolution.ResolveRelativeToBaseDirectory(credentialsPath);

        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException(
                $"Google Drive credentials file not found at '{resolvedPath}'. Configure '{credentialsConfigKey}'.",
                resolvedPath);
        }

        return resolvedPath;
    }
}
