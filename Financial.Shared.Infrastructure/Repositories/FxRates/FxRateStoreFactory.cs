using Financial.Shared.Abstractions.Configuration;
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
    private readonly TimeProvider _timeProvider;

    public FxRateStoreFactory(IFxRateSerializer serializer, IJsonStorageFactory storageFactory, TimeProvider timeProvider)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _storageFactory = storageFactory ?? throw new ArgumentNullException(nameof(storageFactory));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public IFxRateStore Create(JsonRepositorySelection options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var storage = _storageFactory.Create(options, DefaultDataFileName, FxRateRepositorySettingsOptions.ConfigurationKeys);
        var ratesByDate = FxRateLoader.LoadSync(storage, _serializer);
        return new FxRateJsonStore(ratesByDate, storage, _serializer, _timeProvider);
    }
}
