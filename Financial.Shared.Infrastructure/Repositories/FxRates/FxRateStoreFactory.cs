using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.Configuration;
using Financial.Shared.Infrastructure.Persistence.FxRates;

namespace Financial.Shared.Infrastructure.Repositories.FxRates;

public sealed class FxRateStoreFactory
{
    private const string DefaultDataFileName = "data-fx-rates.json";

    private readonly IFxRateSerializer _serializer;
    private readonly IJsonStorageFactory _storageFactory;

    public FxRateStoreFactory(IFxRateSerializer serializer, IJsonStorageFactory storageFactory)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _storageFactory = storageFactory ?? throw new ArgumentNullException(nameof(storageFactory));
    }

    public IFxRateStore Create(FxRateRepositorySelectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var storage = CreateStorage(options);
        var ratesByDate = FxRateLoader.LoadSync(storage, _serializer);
        return new FxRateJsonStore(ratesByDate, storage, _serializer);
    }

    private IJsonStorage CreateStorage(FxRateRepositorySelectionOptions options) =>
        options.Provider switch
        {
            FxRateRepositoryProvider.LocalJson =>
                _storageFactory.CreateLocal(options.LocalDataPath, DefaultDataFileName),
            FxRateRepositoryProvider.GoogleDriveJson =>
                _storageFactory.CreateRemote(
                    options.GoogleDriveCredentialsPath,
                    options.GoogleDriveFilePath,
                    FxRateRepositoryConfigurationKeys.GoogleDriveCredentialsPath,
                    nameof(FxRateRepositoryProvider.GoogleDriveJson)),
            _ => throw new ArgumentOutOfRangeException(
                    nameof(options.Provider), options.Provider, "Unsupported repository provider.")
        };
}
