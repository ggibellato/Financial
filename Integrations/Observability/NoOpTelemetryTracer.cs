using Financial.Shared.Abstractions;

namespace Financial.Integrations.Observability;

/// <summary>
/// ITelemetryTracer implementation used when Observability:Enabled is false, so callers never
/// need to check whether observability is on (FR-006a). Returns the same cached no-op span
/// instance every time — no allocation, no behavior, per call.
/// </summary>
public sealed class NoOpTelemetryTracer : ITelemetryTracer
{
    public ITelemetrySpan StartSpan(string name) => NoOpTelemetrySpan.Instance;

    private sealed class NoOpTelemetrySpan : ITelemetrySpan
    {
        public static readonly NoOpTelemetrySpan Instance = new();

        private NoOpTelemetrySpan()
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
