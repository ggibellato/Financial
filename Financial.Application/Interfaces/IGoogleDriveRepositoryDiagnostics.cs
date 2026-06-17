namespace Financial.Application.Interfaces;

public interface IGoogleDriveRepositoryDiagnostics : IRepositoryDiagnostics
{
    string? GoogleDriveCredentialsPath { get; }
    string? GoogleDriveFilePath { get; }
}
