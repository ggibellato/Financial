namespace Financial.Investment.Application.Configuration;

public sealed class InvestmentRepositorySettingsOptions
{
    public string? Provider { get; set; }
    public string? DataJsonFile { get; set; }
    public string? GoogleDriveCredentialsPath { get; set; }
    public string? GoogleDriveFilePath { get; set; }
}
