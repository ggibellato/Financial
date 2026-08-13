using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.Configuration;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;

namespace Financial.Investment.Infrastructure.Repositories;

public sealed class RepositoryFactory
{
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromSeconds(10);

    private readonly IInvestmentsSerializer _serializer;
    private readonly IRemoteFileClientFactory? _remoteFileClientFactory;

    public RepositoryFactory(IInvestmentsSerializer serializer, IRemoteFileClientFactory? remoteFileClientFactory = null)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _remoteFileClientFactory = remoteFileClientFactory;
    }

    public IRepository Create(RepositorySelectionOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var storage = CreateStorage(options);
        var investments = InvestmentsLoader.LoadSync(storage, _serializer);
        return new JSONRepository(investments, storage, _serializer);
    }

    private IJsonStorage CreateStorage(RepositorySelectionOptions options) =>
        options.Provider switch
        {
            RepositoryProvider.LocalJson =>
                new LocalJsonStorage(options.LocalDataPath),
            RepositoryProvider.GoogleDriveJson =>
                CreateGoogleDriveStorage(options),
            _ => throw new ArgumentOutOfRangeException(
                    nameof(options.Provider), options.Provider, "Unsupported repository provider.")
        };

    private IJsonStorage CreateGoogleDriveStorage(RepositorySelectionOptions options)
    {
        var storage = GoogleDriveStorageFactory.Create(
            options.GoogleDriveCredentialsPath,
            options.GoogleDriveFilePath,
            _remoteFileClientFactory,
            RepositoryConfigurationKeys.GoogleDriveCredentialsPath,
            nameof(RepositoryProvider.GoogleDriveJson));

        return new DebouncedJsonStorage(storage, DebounceWindow);
    }
}
