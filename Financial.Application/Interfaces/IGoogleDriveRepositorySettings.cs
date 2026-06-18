namespace Financial.Application.Interfaces;

public interface IGoogleDriveRepositorySettings : IRepositorySettings
{
    string? GoogleDriveCredentialsPath { get; }
    string? GoogleDriveFilePath { get; }
}
