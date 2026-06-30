namespace Financial.Application.Interfaces;

public interface IRepositorySettings
{
    string? Provider { get; }
    string? DataJsonFile { get; }
    string? GoogleDriveCredentialsPath { get; }
    string? GoogleDriveFilePath { get; }
}
