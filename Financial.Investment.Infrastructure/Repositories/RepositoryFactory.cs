using Financial.Investment.Application.Interfaces;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Configuration;
using Financial.Shared.Infrastructure.Persistence;

namespace Financial.Investment.Infrastructure.Repositories;

public sealed class RepositoryFactory
{
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
        var credentialsPath = ResolveGoogleCredentialsPath(options.GoogleDriveCredentialsPath);

        if (_remoteFileClientFactory is null)
        {
            throw new InvalidOperationException(
                $"Repository provider '{nameof(RepositoryProvider.GoogleDriveJson)}' requires an {nameof(IRemoteFileClientFactory)} " +
                "to be registered (see AddGoogleDriveFileClient).");
        }

        var client = _remoteFileClientFactory.Create(credentialsPath);
        return new GoogleDriveJsonStorage(client, options.GoogleDriveFilePath);
    }

    private static string ResolveGoogleCredentialsPath(string? credentialsPath)
    {
        if (string.IsNullOrWhiteSpace(credentialsPath))
        {
            throw new FileNotFoundException(
                $"Google Drive credentials file path is required. Configure '{RepositoryConfigurationKeys.GoogleDriveCredentialsPath}'.");
        }

        var resolvedPath = credentialsPath;
        if (!Path.IsPathRooted(resolvedPath))
        {
            resolvedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, resolvedPath));
        }

        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException(
                $"Google Drive credentials file not found at '{resolvedPath}'. Configure '{RepositoryConfigurationKeys.GoogleDriveCredentialsPath}'.",
                resolvedPath);
        }

        return resolvedPath;
    }
}
