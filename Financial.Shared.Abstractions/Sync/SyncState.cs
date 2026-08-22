namespace Financial.Shared.Abstractions.Sync;

public enum SyncState
{
    Idle,
    Pending,
    Saving,
    Failed
}
