using System.Diagnostics;

namespace Financial.Presentation.App.Observability;

/// <summary>
/// ActivitySource used to establish a trace root at the start of a WPF ViewModel command
/// (FR-004a) — WPF invokes the Application layer in-process, so it has no inbound HTTP request
/// to anchor a trace to the way Financial.Api does. See contracts/telemetry-semantic-conventions.md.
/// </summary>
public static class AppActivitySource
{
    public const string Name = "Financial.App";

    public static readonly ActivitySource Instance = new(Name);
}
