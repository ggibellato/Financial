using Financial.Shared.Abstractions;

namespace Financial.Shared.Infrastructure.Persistence;

/// <summary>
/// Fallback ITelemetryTracer used by persistence classes when no tracer is supplied (e.g. tests
/// constructing storage directly). Real callers (JsonStorageFactory, wired from each bounded
/// context's repository factory) always pass the DI-resolved tracer instead.
/// </summary>
internal sealed class NullTelemetryTracer : ITelemetryTracer
{
    public static readonly NullTelemetryTracer Instance = new();

    private NullTelemetryTracer()
    {
    }

    public ITelemetrySpan StartSpan(string name) => NullTelemetrySpan.Instance;

    private sealed class NullTelemetrySpan : ITelemetrySpan
    {
        public static readonly NullTelemetrySpan Instance = new();

        private NullTelemetrySpan()
        {
        }

        public void SetAttribute(string key, string value)
        {
        }

        public void RecordException(Exception exception)
        {
        }

        public void Dispose()
        {
        }
    }
}
