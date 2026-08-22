namespace Financial.Shared.Abstractions.Persistence;

public interface IJsonStorageFactory
{
    IJsonStorage CreateLocal(string? localDataPath, string defaultDataFileName);

    IJsonStorage CreateGoogleDrive(
        string? credentialsPath,
        string? driveFilePath,
        string credentialsConfigKey,
        string providerName);
}
