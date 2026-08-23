using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.Configuration;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Persistence;

namespace Financial.CashFlow.Infrastructure.Repositories;

public sealed class CashFlowRepositoryFactory
{
    private const string DefaultDataFileName = "data-cashflow.json";

    private readonly ICashFlowSerializer _serializer;
    private readonly IJsonStorageFactory _storageFactory;

    public CashFlowRepositoryFactory(ICashFlowSerializer serializer, IJsonStorageFactory storageFactory)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _storageFactory = storageFactory ?? throw new ArgumentNullException(nameof(storageFactory));
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
                _storageFactory.CreateLocal(options.LocalDataPath, DefaultDataFileName),
            CashFlowRepositoryProvider.GoogleDriveJson =>
                _storageFactory.CreateRemote(
                    options.GoogleDriveCredentialsPath,
                    options.GoogleDriveFilePath,
                    CashFlowRepositoryConfigurationKeys.GoogleDriveCredentialsPath,
                    nameof(CashFlowRepositoryProvider.GoogleDriveJson)),
            _ => throw new ArgumentOutOfRangeException(
                    nameof(options.Provider), options.Provider, "Unsupported repository provider.")
        };
}
