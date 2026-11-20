using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Abstractions.Sync;

namespace Financial.TestUtilities;

public sealed class FakeSyncStatusStorage : IJsonStorage, ISyncStatusProvider
{
    public SyncStatus Status { get; set; } = new(SyncState.Idle, null, null);

    public int FlushAsyncCallCount { get; private set; }

    public Task<string> ReadAsync() => throw new NotSupportedException();

    public Task WriteAsync(string json) => throw new NotSupportedException();

    public SyncStatus GetStatus() => Status;

    public Task FlushAsync()
    {
        FlushAsyncCallCount++;
        return Task.CompletedTask;
    }
}
