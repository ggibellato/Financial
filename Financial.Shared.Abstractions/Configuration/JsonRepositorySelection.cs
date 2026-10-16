namespace Financial.Shared.Abstractions.Configuration;

public sealed record JsonRepositorySelection(
    JsonRepositoryProvider Provider,
    string? LocalDataPath,
    string? GoogleDriveCredentialsPath,
    string? GoogleDriveFilePath);
