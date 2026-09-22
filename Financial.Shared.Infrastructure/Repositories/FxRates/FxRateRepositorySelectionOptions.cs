namespace Financial.Shared.Infrastructure.Repositories.FxRates;

public sealed record FxRateRepositorySelectionOptions(
    FxRateRepositoryProvider Provider,
    string? LocalDataPath,
    string? GoogleDriveCredentialsPath,
    string? GoogleDriveFilePath);
