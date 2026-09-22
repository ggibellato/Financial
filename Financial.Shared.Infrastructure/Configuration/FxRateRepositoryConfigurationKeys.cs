namespace Financial.Shared.Infrastructure.Configuration;

public static class FxRateRepositoryConfigurationKeys
{
    public const string Provider = "FxRates:Repository:Provider";
    public const string LocalJsonDataFile = "FxRates:DataJsonFile";
    public const string GoogleDriveCredentialsPath = "FxRates:GoogleDrive:CredentialsPath";
    public const string GoogleDriveFilePath = "FxRates:GoogleDrive:FilePath";
}
