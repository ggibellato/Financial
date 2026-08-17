namespace Financial.Shared.Infrastructure.Observability;

/// <summary>
/// The only observability surface most of the codebase ever references. Implementations are
/// supplied exclusively by Integrations/Observability (never referenced directly here) so that
/// Domain, Application, and every other Infrastructure project stay free of any OpenTelemetry
/// SDK dependency. StartSpan MUST never return null and MUST never throw, regardless of whether
/// observability is enabled — see contracts/telemetry-tracer-interface-contract.md.
/// </summary>
public interface ITelemetryTracer
{
    ITelemetrySpan StartSpan(string name);
}
