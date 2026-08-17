namespace Financial.Shared.Infrastructure.Observability;

/// <summary>
/// Canonical ActivitySource names registered with the OpenTelemetry SDK. Each bounded context's
/// Application project (and Financial.App) declares its own ActivitySource instance using the
/// matching literal string below rather than referencing this class, so those layers keep no
/// compile-time dependency on Financial.Shared.Infrastructure or the OpenTelemetry SDK (FR-006).
/// See contracts/telemetry-semantic-conventions.md.
/// </summary>
public static class ObservabilitySourceNames
{
    public const string CashFlow = "Financial.CashFlow";
    public const string Investment = "Financial.Investment";
    public const string SharedInfrastructure = "Financial.Shared.Infrastructure";
    public const string App = "Financial.App";
}
