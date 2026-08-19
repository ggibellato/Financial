using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.Configuration;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Abstractions;
using Financial.Shared.Infrastructure.Persistence;

namespace Financial.CashFlow.Infrastructure.Repositories;

public sealed class CashFlowRepositoryFactory
{
    private const string DefaultDataFileName = "data-cashflow.json";

    private readonly ICashFlowSerializer _serializer;
    private readonly IRemoteFileClientFactory? _remoteFileClientFactory;
    private readonly ITelemetryTracer? _tracer;

    public CashFlowRepositoryFactory(
        ICashFlowSerializer serializer,
        IRemoteFileClientFactory? remoteFileClientFactory = null,
        ITelemetryTracer? tracer = null)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _remoteFileClientFactory = remoteFileClientFactory;
        _tracer = tracer;
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

    private IJsonStorage CreateStorage(CashFlowRepositorySelectionOptions options) =>
        options.Provider switch
        {
            CashFlowRepositoryProvider.LocalJson =>
                JsonStorageFactory.CreateLocal(options.LocalDataPath, DefaultDataFileName),
            CashFlowRepositoryProvider.GoogleDriveJson =>
                JsonStorageFactory.CreateGoogleDrive(
                    options.GoogleDriveCredentialsPath,
                    options.GoogleDriveFilePath,
                    _remoteFileClientFactory,
                    CashFlowRepositoryConfigurationKeys.GoogleDriveCredentialsPath,
                    nameof(CashFlowRepositoryProvider.GoogleDriveJson),
                    _tracer),
            _ => throw new ArgumentOutOfRangeException(
                    nameof(options.Provider), options.Provider, "Unsupported repository provider.")
        };
}
