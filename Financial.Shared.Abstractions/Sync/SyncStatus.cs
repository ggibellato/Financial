namespace Financial.Shared.Abstractions.Sync;

public sealed record SyncStatus(SyncState State, string? LastError, DateTime? LastSuccessfulSaveUtc);
