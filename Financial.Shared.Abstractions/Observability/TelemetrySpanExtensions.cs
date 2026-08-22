namespace Financial.Shared.Abstractions.Observability;

/// <summary>
/// The two outcome epilogues every instrumented operation ends with, defined once so the
/// semantic convention (result attribute + exception recording travel together, exception
/// type only) cannot be applied half-right at a call site.
/// </summary>
public static class TelemetrySpanExtensions
{
    public static void MarkSuccess(this ITelemetrySpan span) =>
        span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);

    /// <summary>Marks the operation as failed and records the exception (type only - the
    /// implementation never captures the message, which may embed financial values).</summary>
    public static void MarkFailed(this ITelemetrySpan span, Exception exception)
    {
        span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
        span.RecordException(exception);
    }
}
