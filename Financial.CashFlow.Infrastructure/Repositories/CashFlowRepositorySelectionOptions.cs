namespace Financial.CashFlow.Infrastructure.Repositories;

public sealed record CashFlowRepositorySelectionOptions(
    CashFlowRepositoryProvider Provider,
    string? LocalDataPath,
    string? GoogleDriveCredentialsPath,
    string? GoogleDriveFilePath);
