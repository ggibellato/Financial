using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Financial.TestUtilities;

/// <summary>A single log entry captured by <see cref="RecordingLogger{T}"/>.</summary>
public sealed record RecordedLogEntry(string Category, LogLevel Level, string Message, Exception? Exception);

/// <summary>Hand-written ILogger test double that records every entry written, for asserting
/// what is - and just as importantly, what is not - logged.</summary>
public sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<RecordedLogEntry> _entries = new();

    public IReadOnlyCollection<RecordedLogEntry> Entries => _entries.ToArray();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Enqueue(new RecordedLogEntry(typeof(T).FullName ?? typeof(T).Name, logLevel, formatter(state, exception), exception));
    }
}
