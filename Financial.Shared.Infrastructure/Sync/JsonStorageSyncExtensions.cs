using Financial.Shared.Abstractions.Persistence;

namespace Financial.Shared.Infrastructure.Sync;

public static class JsonStorageSyncExtensions
{
    public static SyncStatus GetStatusOrIdle(this IJsonStorage storage) =>
        storage is ISyncStatusProvider syncStatusProvider
            ? syncStatusProvider.GetStatus()
            : new SyncStatus(SyncState.Idle, null, null);

    public static Task FlushIfSupportedAsync(this IJsonStorage storage) =>
        storage is ISyncStatusProvider syncStatusProvider
            ? syncStatusProvider.FlushAsync()
            : Task.CompletedTask;
}
