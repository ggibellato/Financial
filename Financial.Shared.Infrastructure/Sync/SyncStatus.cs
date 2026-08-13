namespace Financial.Shared.Infrastructure.Sync;

public sealed record SyncStatus(SyncState State, string? LastError, DateTime? LastSuccessfulSaveUtc);
