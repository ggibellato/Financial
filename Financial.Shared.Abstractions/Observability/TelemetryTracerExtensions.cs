namespace Financial.Shared.Abstractions.Observability;

/// <summary>
/// The span-opening convention every Application service uses, defined once: span name
/// "{BoundedContext}.{ServiceName}.{Operation}" plus the standard allow-listed attributes
/// (contracts/telemetry-semantic-conventions.md). Call sites stay explicit per service -
/// this only owns the shape, not the decision to trace.
/// </summary>
public static class TelemetryTracerExtensions
{
    public static ITelemetrySpan StartServiceSpan(
        this ITelemetryTracer tracer,
        string boundedContext,
        string serviceName,
        string operationName,
        string entityType)
    {
        var span = tracer.StartSpan($"{boundedContext}.{serviceName}.{operationName}");
        span.SetAttribute(TelemetryAttributeKeys.BoundedContext, boundedContext);
        span.SetAttribute(TelemetryAttributeKeys.EntityType, entityType);
        span.SetAttribute(TelemetryAttributeKeys.OperationName, operationName);
        return span;
    }
}
