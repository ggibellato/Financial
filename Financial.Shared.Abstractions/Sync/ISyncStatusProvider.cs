namespace Financial.Shared.Abstractions.Sync;

public interface ISyncStatusProvider
{
    SyncStatus GetStatus();

    Task FlushAsync();
}
