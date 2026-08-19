using System.Reflection;
using Financial.Architecture.Tests.Infrastructure;
using FluentAssertions;

namespace Financial.Architecture.Tests;

/// <summary>
/// Mechanically enforces the observability isolation rule (FR-005/FR-006/SC-008) that was
/// previously verified only by review: every project that records telemetry does so through
/// `Financial.Shared.Abstractions` alone, and never reaches past it into the
/// `Integrations/Observability` project or the OpenTelemetry/Serilog SDKs directly.
/// Flagged as a follow-up in the Phase 1 post-review note of
/// specs/001-application-observability/tasks.md.
/// </summary>
public class ObservabilityIsolationRuleTests
{
    public static TheoryData<string> TelemetryRecordingProjects => new()
    {
        "Financial.CashFlow.Domain",
        "Financial.CashFlow.Application",
        "Financial.CashFlow.Infrastructure",
        "Financial.Investment.Domain",
        "Financial.Investment.Application",
        "Financial.Investment.Infrastructure",
        "Financial.Shared.Infrastructure",
    };

    [Theory]
    [MemberData(nameof(TelemetryRecordingProjects))]
    public void Project_Should_Not_Reference_The_Observability_Integration(string assemblyName)
    {
        var referencedAssemblyNames = ProjectAssembly.GetReferencedAssemblyNames(Assembly.Load(assemblyName));

        referencedAssemblyNames.Should().NotContain(
            name => name.StartsWith("Financial.Integrations.Observability", StringComparison.Ordinal),
            "only the composition roots (Financial.Api, Financial.App) may compose the concrete observability implementation");
    }

    [Theory]
    [MemberData(nameof(TelemetryRecordingProjects))]
    public void Project_Should_Not_Reference_The_OpenTelemetry_Sdk_Or_Serilog(string assemblyName)
    {
        var referencedAssemblyNames = ProjectAssembly.GetReferencedAssemblyNames(Assembly.Load(assemblyName));

        referencedAssemblyNames.Should().NotContain(
            name => name.StartsWith("OpenTelemetry", StringComparison.Ordinal) ||
                    name.StartsWith("Serilog", StringComparison.Ordinal),
            "the OpenTelemetry SDK and the concrete logging provider are confined to Integrations/Observability and the composition roots (FR-005); everything else records telemetry via Financial.Shared.Abstractions and Microsoft.Extensions.Logging.Abstractions");
    }
}
