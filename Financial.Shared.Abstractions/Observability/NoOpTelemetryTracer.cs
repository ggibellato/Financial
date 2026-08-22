namespace Financial.Shared.Abstractions;

/// <summary>
/// The contract's null object: an ITelemetryTracer whose spans do nothing, so callers never need
/// to check whether observability is available before using it (FR-006a). Lives beside the
/// interface for the same reason NullLogger lives in Microsoft.Extensions.Logging.Abstractions -
/// "safe when absent" is part of the contract itself. Returns the same cached no-op span
/// instance every time: no allocation, no behavior, per call.
/// </summary>
public sealed class NoOpTelemetryTracer : ITelemetryTracer
{
    public static readonly NoOpTelemetryTracer Instance = new();

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
