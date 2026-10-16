namespace Financial.Shared.Abstractions.Configuration;

public sealed record JsonRepositoryConfigurationKeys(string Section)
{
    public string Provider => $"{Section}:Repository:Provider";
    public string LocalJsonDataFile => $"{Section}:DataJsonFile";
    public string GoogleDriveCredentialsPath => $"{Section}:GoogleDrive:CredentialsPath";
    public string GoogleDriveFilePath => $"{Section}:GoogleDrive:FilePath";
}
