using Financial.Shared.Abstractions.Configuration;
using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.Configuration;
using Financial.Shared.Infrastructure.Persistence.FxRates;
using Microsoft.Extensions.Logging;

namespace Financial.Shared.Infrastructure.Repositories.FxRates;

public sealed class FxRateStoreFactory
{
    private const string DefaultDataFileName = "data-fx-rates.json";

    private readonly IFxRateSerializer _serializer;
    private readonly IJsonStorageFactory _storageFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FxRateJsonStore> _logger;

    public FxRateStoreFactory(
        IFxRateSerializer serializer,
        IJsonStorageFactory storageFactory,
        TimeProvider timeProvider,
        ILogger<FxRateJsonStore> logger)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _storageFactory = storageFactory ?? throw new ArgumentNullException(nameof(storageFactory));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IFxRateStore Create(JsonRepositorySelection options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var storage = _storageFactory.Create(options, DefaultDataFileName, FxRateRepositorySettingsOptions.ConfigurationKeys);
        var ratesByDate = FxRateLoader.LoadSync(storage, _serializer, _logger);
        return new FxRateJsonStore(ratesByDate, storage, _serializer, _timeProvider, _logger);
    }
}
