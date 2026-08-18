using System.Diagnostics;
using Financial.Shared.Abstractions;

namespace Financial.Integrations.Observability;

/// <summary>
/// ITelemetryTracer implementation used when Observability:Enabled is true. Wraps a
/// System.Diagnostics.ActivitySource; the OpenTelemetry SDK registered alongside this class
/// (ObservabilityServiceCollectionExtensions) subscribes to the same source name and does the
/// actual export.
/// </summary>
public sealed class OpenTelemetryTracer : ITelemetryTracer
{
    internal const string ActivitySourceName = "Financial";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public ITelemetrySpan StartSpan(string name) => new OpenTelemetrySpan(ActivitySource.StartActivity(name));

    private sealed class OpenTelemetrySpan : ITelemetrySpan
    {
        private readonly Activity? _activity;

        public OpenTelemetrySpan(Activity? activity)
        {
            _activity = activity;
        }

        public void SetAttribute(string key, string value) => _activity?.SetTag(key, value);

        public void RecordException(Exception exception)
        {
            // Only the exception type is recorded, never exception.Message, which may embed a
            // financial value (see contracts/telemetry-tracer-interface-contract.md rule 4).
            _activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            _activity?.SetTag(TelemetryAttributeKeys.ErrorType, exception.GetType().Name);
        }

        public void Dispose() => _activity?.Dispose();
    }
}
