namespace Financial.Shared.Abstractions.Observability;

/// <summary>
/// StartSpan MUST never return null and MUST never throw, regardless of whether observability is
/// enabled — see contracts/telemetry-tracer-interface-contract.md.
/// </summary>
public interface ITelemetryTracer
{
    ITelemetrySpan StartSpan(string name);
}
