using Financial.Shared.Abstractions.Configuration;

namespace Financial.Shared.Abstractions.Persistence;

public static class JsonStorageFactoryExtensions
{
    public static IJsonStorage Create(
        this IJsonStorageFactory storageFactory,
        JsonRepositorySelection selection,
        string defaultDataFileName,
        JsonRepositoryConfigurationKeys configurationKeys)
    {
        ArgumentNullException.ThrowIfNull(storageFactory);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(configurationKeys);

        return selection.Provider switch
        {
            JsonRepositoryProvider.LocalJson =>
                storageFactory.CreateLocal(selection.LocalDataPath, defaultDataFileName),
            JsonRepositoryProvider.GoogleDriveJson =>
                storageFactory.CreateRemote(
                    selection.GoogleDriveCredentialsPath,
                    selection.GoogleDriveFilePath,
                    configurationKeys.GoogleDriveCredentialsPath,
                    nameof(JsonRepositoryProvider.GoogleDriveJson)),
            _ => throw new ArgumentOutOfRangeException(
                    nameof(selection.Provider), selection.Provider, "Unsupported repository provider.")
        };
    }
}
