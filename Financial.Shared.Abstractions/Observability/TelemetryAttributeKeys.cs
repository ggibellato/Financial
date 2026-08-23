namespace Financial.Shared.Abstractions.Observability;

/// <summary>
/// Attribute names for <see cref="ITelemetrySpan.SetAttribute"/>, matching the allow-list in
/// contracts/telemetry-semantic-conventions.md. Shared by every ITelemetryTracer consumer so span
/// attributes stay consistent across the trace viewer.
/// </summary>
public static class TelemetryAttributeKeys
{
    public const string BoundedContext = "bounded_context";
    public const string OperationName = "operation.name";
    public const string OperationResult = "operation.result";
    public const string EntityId = "entity.id";
    public const string EntityType = "entity.type";
    public const string ErrorType = "error.type";
}

/// <summary>Values for <see cref="TelemetryAttributeKeys.OperationResult"/>.</summary>
public static class TelemetryOperationResults
{
    public const string Success = "success";
    public const string Failed = "failed";
    public const string Canceled = "canceled";
    public const string Rejected = "rejected";
}
