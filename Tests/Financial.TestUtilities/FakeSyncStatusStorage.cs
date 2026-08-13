using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Sync;

namespace Financial.TestUtilities;

/// <summary>
/// An <see cref="IJsonStorage"/> that also implements <see cref="ISyncStatusProvider"/>, for tests
/// that need a repository's <c>_storage</c> field to delegate GetStatus/FlushAsync to something
/// observable. <see cref="ReadAsync"/>/<see cref="WriteAsync"/> are no-ops - this double exists only
/// to control/observe sync status.
/// </summary>
public sealed class FakeSyncStatusStorage : IJsonStorage, ISyncStatusProvider
{
    public SyncStatus Status { get; set; } = new(SyncState.Idle, null, null);

    public int FlushAsyncCallCount { get; private set; }

    public Task<string> ReadAsync() => Task.FromResult("{}");

    public Task WriteAsync(string json) => Task.CompletedTask;

    public SyncStatus GetStatus() => Status;

    public Task FlushAsync()
    {
        FlushAsyncCallCount++;
        return Task.CompletedTask;
    }
}
