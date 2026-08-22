using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.Configuration;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Persistence;

namespace Financial.Investment.Infrastructure.Repositories;

public sealed class InvestmentRepositoryFactory
{
    private const string DefaultDataFileName = "data-investment.json";

    private readonly IInvestmentsSerializer _serializer;
    private readonly IJsonStorageFactory _storageFactory;

    public InvestmentRepositoryFactory(IInvestmentsSerializer serializer, IJsonStorageFactory storageFactory)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _storageFactory = storageFactory ?? throw new ArgumentNullException(nameof(storageFactory));
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

    private IJsonStorage CreateStorage(InvestmentRepositorySelectionOptions options) =>
        options.Provider switch
        {
            InvestmentRepositoryProvider.LocalJson =>
                _storageFactory.CreateLocal(options.LocalDataPath, DefaultDataFileName),
            InvestmentRepositoryProvider.GoogleDriveJson =>
                _storageFactory.CreateGoogleDrive(
                    options.GoogleDriveCredentialsPath,
                    options.GoogleDriveFilePath,
                    InvestmentRepositoryConfigurationKeys.GoogleDriveCredentialsPath,
                    nameof(InvestmentRepositoryProvider.GoogleDriveJson)),
            _ => throw new ArgumentOutOfRangeException(
                    nameof(options.Provider), options.Provider, "Unsupported repository provider.")
        };
}
