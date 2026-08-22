using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.Configuration;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Abstractions;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Infrastructure.Repositories;

public sealed class InvestmentRepositoryFactory
{
    private const string DefaultDataFileName = "data-investment.json";

    private readonly IInvestmentsSerializer _serializer;
    private readonly IRemoteFileClientFactory? _remoteFileClientFactory;
    private readonly ITelemetryTracer? _tracer;
    private readonly ILogger? _storageLogger;

    public InvestmentRepositoryFactory(
        IInvestmentsSerializer serializer,
        IRemoteFileClientFactory? remoteFileClientFactory = null,
        ITelemetryTracer? tracer = null,
        ILogger? storageLogger = null)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _remoteFileClientFactory = remoteFileClientFactory;
        _tracer = tracer;
        _storageLogger = storageLogger;
    }

    public IInvestmentRepository Create(InvestmentRepositorySelectionOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var storage = CreateStorage(options);
        var investments = InvestmentsLoader.LoadSync(storage, _serializer);
        return new InvestmentJsonRepository(investments, storage, _serializer);
    }

    private IJsonStorage CreateStorage(InvestmentRepositorySelectionOptions options)
    {
        // Instantiated per call rather than DI-injected: F07 (Wave 2 of the shared-domain-structure
        // refactor) moves this to constructor injection once IJsonStorageFactory is registered at
        // the composition root (F08). Until then this keeps Investment.Infrastructure buildable
        // against the now-instance-based JsonStorageFactory.
        var storageFactory = new JsonStorageFactory(
            _remoteFileClientFactory, _tracer ?? NoOpTelemetryTracer.Instance, _storageLogger as ILogger<DebouncedJsonStorage>);

        return options.Provider switch
        {
            InvestmentRepositoryProvider.LocalJson =>
                storageFactory.CreateLocal(options.LocalDataPath, DefaultDataFileName),
            InvestmentRepositoryProvider.GoogleDriveJson =>
                storageFactory.CreateGoogleDrive(
                    options.GoogleDriveCredentialsPath,
                    options.GoogleDriveFilePath,
                    InvestmentRepositoryConfigurationKeys.GoogleDriveCredentialsPath,
                    nameof(InvestmentRepositoryProvider.GoogleDriveJson)),
            _ => throw new ArgumentOutOfRangeException(
                    nameof(options.Provider), options.Provider, "Unsupported repository provider.")
        };
    }
}
