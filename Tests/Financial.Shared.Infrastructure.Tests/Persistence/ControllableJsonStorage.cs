using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.Resilience;

namespace Financial.Shared.Infrastructure.Tests.Persistence;

internal sealed class ControllableJsonStorage : IJsonStorage
{
    private readonly object _lock = new();
    private readonly List<string> _writtenJson = new();
    private TaskCompletionSource<bool>? _gate;
    private int _failuresRemaining;

    internal IReadOnlyList<string> WrittenJson
    {
        get
        {
            lock (_lock)
            {
                return _writtenJson.ToArray();
            }
        }
    }

    internal string ReadResult { get; set; } = "{}";

    public Task<string> ReadAsync() => Task.FromResult(ReadResult);

    public async Task WriteAsync(string json)
    {
        TaskCompletionSource<bool>? gate;
        lock (_lock)
        {
            gate = _gate;
        }

        if (gate is not null)
        {
            await gate.Task.ConfigureAwait(false);
        }

        bool shouldFail;
        lock (_lock)
        {
            shouldFail = _failuresRemaining > 0;
            if (shouldFail)
            {
                _failuresRemaining--;
            }
            else
            {
                _writtenJson.Add(json);
            }
        }

        if (shouldFail)
        {
            throw new TransientStorageException(
                "Simulated transient storage failure.",
                new InvalidOperationException("Simulated cause."));
        }
    }

    internal void HoldWritesUntilReleased()
    {
        lock (_lock)
        {
            _gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    internal void Release()
    {
        lock (_lock)
        {
            _gate?.TrySetResult(true);
        }
    }

    internal void FailNextWrites(int count)
    {
        lock (_lock)
        {
            _failuresRemaining = count;
        }
    }
}
