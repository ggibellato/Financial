using System.Collections.Concurrent;
using Financial.Shared.Abstractions;

namespace Financial.TestUtilities;

/// <summary>Hand-written ITelemetryTracer test double that records every span started, for
/// asserting span names/attributes without depending on the real OpenTelemetry SDK.</summary>
public sealed class RecordingTelemetryTracer : ITelemetryTracer
{
    private readonly ConcurrentQueue<RecordedSpan> _spans = new();

    public IReadOnlyCollection<RecordedSpan> Spans => _spans.ToArray();

    public ITelemetrySpan StartSpan(string name)
    {
        var span = new RecordedSpan(name);
        _spans.Enqueue(span);
        return span;
    }

    public sealed class RecordedSpan : ITelemetrySpan
    {
        private readonly Dictionary<string, string> _attributes = new();

        internal RecordedSpan(string name) => Name = name;

        public string Name { get; }

        public IReadOnlyDictionary<string, string> Attributes => _attributes;

        public Exception? RecordedException { get; private set; }

        public bool Disposed { get; private set; }

        public void SetAttribute(string key, string value) => _attributes[key] = value;

        public void RecordException(Exception exception) => RecordedException = exception;

        public void Dispose() => Disposed = true;
    }
}
