namespace Financial.Shared.Abstractions.Configuration;

public abstract class JsonRepositorySettings
{
    public string? Provider { get; set; }
    public string? DataJsonFile { get; set; }
    public string? GoogleDriveCredentialsPath { get; set; }
    public string? GoogleDriveFilePath { get; set; }

    public void Load(JsonRepositoryConfigurationKeys keys, Func<string, string?> readValue)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(readValue);

        Provider = readValue(keys.Provider);
        DataJsonFile = readValue(keys.LocalJsonDataFile);
        GoogleDriveCredentialsPath = readValue(keys.GoogleDriveCredentialsPath);
        GoogleDriveFilePath = readValue(keys.GoogleDriveFilePath);
    }

    public JsonRepositorySelection ToSelection() =>
        new(
            RepositoryProviderResolver.Resolve(Provider, JsonRepositoryProvider.LocalJson),
            DataJsonFile,
            GoogleDriveCredentialsPath,
            GoogleDriveFilePath);
}
