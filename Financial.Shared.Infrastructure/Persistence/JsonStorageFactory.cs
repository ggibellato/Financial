using Financial.Shared.Abstractions;

namespace Financial.Shared.Infrastructure.Persistence;

/// <summary>
/// Shared storage-construction flow used by every bounded context's repository factory:
/// a bare LocalJsonStorage for the local provider, or a debounced wrapper around Google Drive
/// storage for the cloud provider (debounce window matches the write-heavy usage pattern of a
/// single-user JSON-document repository).
/// </summary>
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
        ITelemetryTracer? tracer = null)
    {
        var storage = GoogleDriveStorageFactory.Create(
            credentialsPath, driveFilePath, remoteFileClientFactory, credentialsConfigKey, providerName, tracer);

        return new DebouncedJsonStorage(storage, DebounceWindow, tracer: tracer);
    }
}
