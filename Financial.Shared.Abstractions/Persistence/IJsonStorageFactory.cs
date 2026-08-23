namespace Financial.Shared.Abstractions.Persistence;

public interface IJsonStorageFactory
{
    IJsonStorage CreateLocal(string? localDataPath, string defaultDataFileName);

    IJsonStorage CreateRemote(
        string? credentialsPath,
        string? remoteFilePath,
        string credentialsConfigKey,
        string providerName);
}
