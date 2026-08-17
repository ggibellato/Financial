using System.Diagnostics;

namespace Financial.Shared.Infrastructure.Observability;

/// <summary>
/// ActivitySource for storage operations (JSON load/save, Google Drive I/O) shared by both
/// bounded contexts. See contracts/telemetry-semantic-conventions.md.
/// </summary>
public static class SharedInfrastructureActivitySource
{
    public static readonly ActivitySource Instance = new(ObservabilitySourceNames.SharedInfrastructure);
}
