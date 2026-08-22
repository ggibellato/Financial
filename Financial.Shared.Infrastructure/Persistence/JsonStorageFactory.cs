using Financial.Shared.Abstractions;
using Financial.Shared.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace Financial.Shared.Infrastructure.Persistence;

public sealed class JsonStorageFactory : IJsonStorageFactory
{
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromSeconds(10);

    private readonly IRemoteFileClientFactory? _remoteFileClientFactory;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<DebouncedJsonStorage>? _logger;

    public JsonStorageFactory(
        IRemoteFileClientFactory? remoteFileClientFactory,
        ITelemetryTracer tracer,
        ILogger<DebouncedJsonStorage>? logger = null)
    {
        _remoteFileClientFactory = remoteFileClientFactory;
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger;
    }

    public IJsonStorage CreateLocal(string? localDataPath, string defaultDataFileName) =>
        new LocalJsonStorage(localDataPath, defaultDataFileName);

    public IJsonStorage CreateGoogleDrive(
        string? credentialsPath,
        string? driveFilePath,
        string credentialsConfigKey,
        string providerName)
    {
        var storage = GoogleDriveStorageFactory.Create(
            credentialsPath, driveFilePath, _remoteFileClientFactory, credentialsConfigKey, providerName, _tracer);

        return new DebouncedJsonStorage(storage, DebounceWindow, tracer: _tracer, logger: _logger);
    }
}
