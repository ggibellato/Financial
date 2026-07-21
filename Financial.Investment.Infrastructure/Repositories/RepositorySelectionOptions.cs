namespace Financial.Investment.Infrastructure.Repositories;

public sealed record RepositorySelectionOptions(
    RepositoryProvider Provider,
    string? LocalDataPath,
    string? GoogleDriveCredentialsPath,
    string? GoogleDriveFilePath);
