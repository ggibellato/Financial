using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.Configuration;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;

namespace Financial.Investment.Infrastructure.Repositories;

public sealed class InvestmentRepositoryFactory
{
    private const string DefaultDataFileName = "data-investment.json";
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromSeconds(10);

    private readonly IInvestmentsSerializer _serializer;
    private readonly IRemoteFileClientFactory? _remoteFileClientFactory;

    public InvestmentRepositoryFactory(IInvestmentsSerializer serializer, IRemoteFileClientFactory? remoteFileClientFactory = null)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _remoteFileClientFactory = remoteFileClientFactory;
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
                new LocalJsonStorage(options.LocalDataPath, DefaultDataFileName),
            InvestmentRepositoryProvider.GoogleDriveJson =>
                CreateGoogleDriveStorage(options),
            _ => throw new ArgumentOutOfRangeException(
                    nameof(options.Provider), options.Provider, "Unsupported repository provider.")
        };

    private IJsonStorage CreateGoogleDriveStorage(InvestmentRepositorySelectionOptions options)
    {
        var storage = GoogleDriveStorageFactory.Create(
            options.GoogleDriveCredentialsPath,
            options.GoogleDriveFilePath,
            _remoteFileClientFactory,
            InvestmentRepositoryConfigurationKeys.GoogleDriveCredentialsPath,
            nameof(InvestmentRepositoryProvider.GoogleDriveJson));

        return new DebouncedJsonStorage(storage, DebounceWindow);
    }
}
