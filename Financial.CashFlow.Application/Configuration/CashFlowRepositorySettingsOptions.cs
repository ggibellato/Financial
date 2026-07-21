namespace Financial.CashFlow.Application.Configuration;

public sealed class CashFlowRepositorySettingsOptions
{
    public string? Provider { get; set; }
    public string? DataJsonFile { get; set; }
    public string? GoogleDriveCredentialsPath { get; set; }
    public string? GoogleDriveFilePath { get; set; }
}
