namespace Financial.Shared.Abstractions.Sync;

public static class JsonStorageSyncExtensions
{
    public static SyncStatus GetStatusOrIdle(this object target) =>
        target is ISyncStatusProvider syncStatusProvider
            ? syncStatusProvider.GetStatus()
            : new SyncStatus(SyncState.Idle, null, null);

    public static Task FlushIfSupportedAsync(this object target) =>
        target is ISyncStatusProvider syncStatusProvider
            ? syncStatusProvider.FlushAsync()
            : Task.CompletedTask;
}
