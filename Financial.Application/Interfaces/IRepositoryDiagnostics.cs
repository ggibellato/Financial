namespace Financial.Application.Interfaces;

public interface IRepositoryDiagnostics
{
    string? Provider { get; }
    string? DataJsonFile { get; }
    string? GoogleDriveCredentialsPath { get; }
    string? GoogleDriveFilePath { get; }
}
