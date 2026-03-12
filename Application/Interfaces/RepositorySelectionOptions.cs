namespace Financial.Application.Interfaces;

public sealed record RepositorySelectionOptions(
    RepositoryProvider Provider,
    string? LocalDataPath,
    string? GoogleDriveCredentialsPath,
    string? GoogleDriveFilePath);
