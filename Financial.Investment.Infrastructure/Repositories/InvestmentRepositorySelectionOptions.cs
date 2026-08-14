namespace Financial.Investment.Infrastructure.Repositories;

public sealed record InvestmentRepositorySelectionOptions(
    InvestmentRepositoryProvider Provider,
    string? LocalDataPath,
    string? GoogleDriveCredentialsPath,
    string? GoogleDriveFilePath);
