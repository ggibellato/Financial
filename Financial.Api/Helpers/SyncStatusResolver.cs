using Financial.Shared.Abstractions.Sync;

namespace Financial.Api.Helpers;

/// <summary>
/// Resolves a repository's current sync status for diagnostic/health endpoints. A repository whose
/// storage writes straight through, rather than through the debounced path that tracks status,
/// reports Idle instead of looking unavailable.
/// </summary>
public static class SyncStatusResolver
{
    public static SyncStatus Resolve(object repository) =>
        repository is ISyncStatusProvider syncStatusProvider
            ? syncStatusProvider.GetStatus()
            : new SyncStatus(SyncState.Idle, null, null);
}
