using Financial.Shared.Abstractions.Persistence;

namespace Financial.TestUtilities;

public sealed class RecordingJsonStorage : IJsonStorage
{
    private readonly IJsonStorage? _inner;

    public RecordingJsonStorage(IJsonStorage? inner = null)
    {
        _inner = inner;
    }

    public int WriteCount { get; private set; }

    public string? LastJson { get; private set; }

    public bool FailNextWrite { get; set; }

    public bool FailWrites { get; set; }

    public Task<string> ReadAsync() =>
        _inner?.ReadAsync() ?? throw new NotSupportedException("These tests build the data directly.");

    public Task WriteAsync(string json)
    {
        if (FailNextWrite || FailWrites)
        {
            FailNextWrite = false;
            throw new IOException("The process cannot access the file because it is being used by another process.");
        }

        WriteCount++;
        LastJson = json;
        return _inner?.WriteAsync(json) ?? Task.CompletedTask;
    }
}
