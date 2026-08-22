using Financial.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Financial.Shared.Infrastructure.Persistence;

public static class JsonStorageFactory
{
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromSeconds(10);

    public static IJsonStorage CreateLocal(string? localDataPath, string defaultDataFileName) =>
        new LocalJsonStorage(localDataPath, defaultDataFileName);

    public static IJsonStorage CreateGoogleDrive(
        string? credentialsPath,
        string? driveFilePath,
        IRemoteFileClientFactory? remoteFileClientFactory,
        string credentialsConfigKey,
        string providerName,
        ITelemetryTracer? tracer = null,
        ILogger? logger = null)
    {
        var storage = GoogleDriveStorageFactory.Create(
            credentialsPath, driveFilePath, remoteFileClientFactory, credentialsConfigKey, providerName, tracer);

        return new DebouncedJsonStorage(storage, DebounceWindow, tracer: tracer, logger: logger);
    }
}
