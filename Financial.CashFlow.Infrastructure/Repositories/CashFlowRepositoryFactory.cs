using Financial.CashFlow.Application.Configuration;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Configuration;
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

    public ICashFlowRepository Create(JsonRepositorySelection options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var storage = _storageFactory.Create(options, DefaultDataFileName, CashFlowRepositorySettingsOptions.ConfigurationKeys);
        var data = CashFlowLoader.LoadSync(storage, _serializer);
        return new CashFlowJsonRepository(data, storage, _serializer);
    }
}
