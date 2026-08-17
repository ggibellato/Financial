using System.Diagnostics;

namespace Financial.CashFlow.Application.Observability;

/// <summary>
/// BCL ActivitySource for the CashFlow bounded context's Application-layer use cases. Kept
/// dependency-free from any OpenTelemetry SDK package (FR-006) — the OpenTelemetry SDK
/// registered in Financial.Shared.Infrastructure subscribes to this source by its literal name
/// (see ObservabilitySourceNames.CashFlow / contracts/telemetry-semantic-conventions.md).
/// </summary>
public static class CashFlowActivitySource
{
    public const string Name = "Financial.CashFlow";

    public static readonly ActivitySource Instance = new(Name);
}
