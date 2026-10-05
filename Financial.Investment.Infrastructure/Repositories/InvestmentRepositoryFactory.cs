using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.Configuration;
using Financial.Investment.Infrastructure.Persistence;
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

    public IInvestmentRepository Create(InvestmentRepositorySelectionOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var storage = CreateStorage(options);
        var investments = InvestmentLoader.LoadSync(storage, _serializer, _timeProvider);
        return new InvestmentJsonRepository(investments, storage, _serializer);
    }

    private IJsonStorage CreateStorage(InvestmentRepositorySelectionOptions options) =>
        options.Provider switch
        {
            InvestmentRepositoryProvider.LocalJson =>
                _storageFactory.CreateLocal(options.LocalDataPath, DefaultDataFileName),
            InvestmentRepositoryProvider.GoogleDriveJson =>
                _storageFactory.CreateRemote(
                    options.GoogleDriveCredentialsPath,
                    options.GoogleDriveFilePath,
                    InvestmentRepositoryConfigurationKeys.GoogleDriveCredentialsPath,
                    nameof(InvestmentRepositoryProvider.GoogleDriveJson)),
            _ => throw new ArgumentOutOfRangeException(
                    nameof(options.Provider), options.Provider, "Unsupported repository provider.")
        };
}
