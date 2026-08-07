using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.Configuration;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;

namespace Financial.CashFlow.Infrastructure.Repositories;

public sealed class CashFlowRepositoryFactory
{
    private const string DefaultDataFileName = "data-cashflow.json";

    private readonly ICashFlowSerializer _serializer;
    private readonly IRemoteFileClientFactory? _remoteFileClientFactory;

    public CashFlowRepositoryFactory(ICashFlowSerializer serializer, IRemoteFileClientFactory? remoteFileClientFactory = null)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _remoteFileClientFactory = remoteFileClientFactory;
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
                new LocalJsonStorage(options.LocalDataPath, DefaultDataFileName),
            CashFlowRepositoryProvider.GoogleDriveJson =>
                CreateGoogleDriveStorage(options),
            _ => throw new ArgumentOutOfRangeException(
                    nameof(options.Provider), options.Provider, "Unsupported repository provider.")
        };

    private IJsonStorage CreateGoogleDriveStorage(CashFlowRepositorySelectionOptions options) =>
        GoogleDriveStorageFactory.Create(
            options.GoogleDriveCredentialsPath,
            options.GoogleDriveFilePath,
            _remoteFileClientFactory,
            CashFlowRepositoryConfigurationKeys.GoogleDriveCredentialsPath,
            nameof(CashFlowRepositoryProvider.GoogleDriveJson));
}
