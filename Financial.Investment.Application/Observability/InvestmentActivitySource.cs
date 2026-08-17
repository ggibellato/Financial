using System.Diagnostics;

namespace Financial.Investment.Application.Observability;

/// <summary>
/// BCL ActivitySource for the Investment bounded context's Application-layer use cases. Kept
/// dependency-free from any OpenTelemetry SDK package (FR-006) — the OpenTelemetry SDK
/// registered in Financial.Shared.Infrastructure subscribes to this source by its literal name
/// (see ObservabilitySourceNames.Investment / contracts/telemetry-semantic-conventions.md).
/// </summary>
public static class InvestmentActivitySource
{
    public const string Name = "Financial.Investment";

    public static readonly ActivitySource Instance = new(Name);
}
