using Financial.Investment.Application.Configuration;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Configuration;
using Financial.Shared.Abstractions.Persistence;

namespace Financial.Investment.Infrastructure.Repositories;

public sealed class InvestmentRepositoryFactory
{
    private const string DefaultDataFileName = "data-investment.json";

    private readonly IInvestmentSerializer _serializer;
    private readonly IJsonStorageFactory _storageFactory;
    private readonly TimeProvider _timeProvider;

    public InvestmentRepositoryFactory(IInvestmentSerializer serializer, IJsonStorageFactory storageFactory, TimeProvider timeProvider)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _storageFactory = storageFactory ?? throw new ArgumentNullException(nameof(storageFactory));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public IInvestmentRepository Create(JsonRepositorySelection options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var storage = _storageFactory.Create(options, DefaultDataFileName, InvestmentRepositorySettingsOptions.ConfigurationKeys);
        var investments = InvestmentLoader.LoadSync(storage, _serializer, _timeProvider);
        return new InvestmentJsonRepository(investments, storage, _serializer);
    }
}
