using System.Collections.Concurrent;
using System.Diagnostics;
using Financial.Shared.Abstractions.Observability;

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

        internal RecordedSpan(string name)
        {
            Name = name;
            AmbientTraceId = Activity.Current?.TraceId.ToString();
        }

        public string Name { get; }

        /// <summary>The trace id of the ambient <see cref="Activity"/> at the moment the span was
        /// started (e.g. ASP.NET Core's per-request activity), or null when there was none. Two
        /// recorded spans sharing a non-null value belong to the same trace.</summary>
        public string? AmbientTraceId { get; }

        public IReadOnlyDictionary<string, string> Attributes => _attributes;

        public Exception? RecordedException { get; private set; }

        public bool Disposed { get; private set; }

        public void SetAttribute(string key, string value) => _attributes[key] = value;

        public void RecordException(Exception exception) => RecordedException = exception;

        public void Dispose() => Disposed = true;
    }
}
