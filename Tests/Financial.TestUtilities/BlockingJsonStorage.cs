using Financial.Shared.Abstractions.Persistence;

namespace Financial.TestUtilities;

public sealed class BlockingJsonStorage : IJsonStorage
{
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource WriteEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int WriteCount { get; private set; }

    public Task<string> ReadAsync() => throw new NotSupportedException("These tests build the data directly.");

    public void ReleaseWrite() => _release.TrySetResult();

    public async Task WriteAsync(string json)
    {
        WriteCount++;
        WriteEntered.TrySetResult();
        await _release.Task;
    }
}
