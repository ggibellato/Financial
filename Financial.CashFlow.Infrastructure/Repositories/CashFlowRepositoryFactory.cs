using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.Configuration;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Financial.CashFlow.Infrastructure.Repositories;

public sealed class CashFlowRepositoryFactory
{
    private const string DefaultDataFileName = "data-cashflow.json";

    private readonly ICashFlowSerializer _serializer;
    private readonly IRemoteFileClientFactory? _remoteFileClientFactory;
    private readonly ITelemetryTracer? _tracer;
    private readonly ILogger? _storageLogger;

    public CashFlowRepositoryFactory(
        ICashFlowSerializer serializer,
        IRemoteFileClientFactory? remoteFileClientFactory = null,
        ITelemetryTracer? tracer = null,
        ILogger? storageLogger = null)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _remoteFileClientFactory = remoteFileClientFactory;
        _tracer = tracer;
        _storageLogger = storageLogger;
    }

    public ICashFlowRepository Create(CashFlowRepositorySelectionOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var storage = CreateStorage(options);
        var data = CashFlowLoader.LoadSync(storage, _serializer);
        return new CashFlowJsonRepository(data, storage, _serializer);
    }

    private IJsonStorage CreateStorage(CashFlowRepositorySelectionOptions options)
    {
        // Instantiated per call rather than DI-injected: F06 (Wave 2 of the shared-domain-structure
        // refactor) moves this to constructor injection once IJsonStorageFactory is registered at
        // the composition root (F08). Until then this keeps CashFlow.Infrastructure buildable
        // against the now-instance-based JsonStorageFactory.
        var storageFactory = new JsonStorageFactory(
            _remoteFileClientFactory, _tracer ?? NoOpTelemetryTracer.Instance, _storageLogger as ILogger<DebouncedJsonStorage>);

        return options.Provider switch
        {
            CashFlowRepositoryProvider.LocalJson =>
                storageFactory.CreateLocal(options.LocalDataPath, DefaultDataFileName),
            CashFlowRepositoryProvider.GoogleDriveJson =>
                storageFactory.CreateGoogleDrive(
                    options.GoogleDriveCredentialsPath,
                    options.GoogleDriveFilePath,
                    CashFlowRepositoryConfigurationKeys.GoogleDriveCredentialsPath,
                    nameof(CashFlowRepositoryProvider.GoogleDriveJson)),
            _ => throw new ArgumentOutOfRangeException(
                    nameof(options.Provider), options.Provider, "Unsupported repository provider.")
        };
    }
}
