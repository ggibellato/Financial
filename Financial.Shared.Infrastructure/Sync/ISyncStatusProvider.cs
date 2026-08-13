namespace Financial.Shared.Infrastructure.Sync;

public interface ISyncStatusProvider
{
    SyncStatus GetStatus();

    Task FlushAsync();
}
