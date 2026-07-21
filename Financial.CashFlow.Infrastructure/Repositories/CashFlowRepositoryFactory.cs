using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Infrastructure.Configuration;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;

namespace Financial.CashFlow.Infrastructure.Repositories;

public sealed class CashFlowRepositoryFactory
{
    private readonly ICashFlowSerializer _serializer;
    private readonly IRemoteFileClientFactory? _remoteFileClientFactory;

    public CashFlowRepositoryFactory(ICashFlowSerializer serializer, IRemoteFileClientFactory? remoteFileClientFactory = null)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _remoteFileClientFactory = remoteFileClientFactory;
    }

    public ICashFlowRepository Create(CashFlowRepositorySelectionOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var storage = CreateStorage(options);
        var data = CashFlowLoader.LoadSync(storage, _serializer);
        return new CashFlowJsonRepository(data, storage, _serializer);
    }

    private IJsonStorage CreateStorage(CashFlowRepositorySelectionOptions options) =>
        options.Provider switch
        {
            CashFlowRepositoryProvider.LocalJson =>
                new LocalJsonStorage(options.LocalDataPath),
            CashFlowRepositoryProvider.GoogleDriveJson =>
                CreateGoogleDriveStorage(options),
            _ => throw new ArgumentOutOfRangeException(
                    nameof(options.Provider), options.Provider, "Unsupported repository provider.")
        };

    private IJsonStorage CreateGoogleDriveStorage(CashFlowRepositorySelectionOptions options)
    {
        var credentialsPath = ResolveGoogleCredentialsPath(options.GoogleDriveCredentialsPath);

        if (_remoteFileClientFactory is null)
        {
            throw new InvalidOperationException(
                $"Repository provider '{nameof(CashFlowRepositoryProvider.GoogleDriveJson)}' requires an {nameof(IRemoteFileClientFactory)} " +
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
                $"Google Drive credentials file path is required. Configure '{CashFlowRepositoryConfigurationKeys.GoogleDriveCredentialsPath}'.");
        }

        var resolvedPath = credentialsPath;
        if (!Path.IsPathRooted(resolvedPath))
        {
            resolvedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, resolvedPath));
        }

        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException(
                $"Google Drive credentials file not found at '{resolvedPath}'. Configure '{CashFlowRepositoryConfigurationKeys.GoogleDriveCredentialsPath}'.",
                resolvedPath);
        }

        return resolvedPath;
    }
}
