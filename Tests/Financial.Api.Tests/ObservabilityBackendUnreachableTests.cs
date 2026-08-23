using System.Net;
using Financial.Shared.Abstractions.Observability;
using Financial.Integrations.Observability;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests;

/// <summary>T047 / quickstart Scenario D / SC-006: with observability enabled but no
/// observability container running at all, the app must start and function normally -
/// telemetry export failures are retried silently in the background, never surfaced
/// to the user.</summary>
public class ObservabilityBackendUnreachableTests
{
    // UseSetting (not ConfigureAppConfiguration): AddObservability binds its options inline
    // while Program.cs executes, and factory ConfigureAppConfiguration callbacks are applied
    // too late for inline reads under minimal hosting. Port 4319 has no listener -
    // deliberately NOT 4317, in case a real local Jaeger is up.
    private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> CreateFactory() =>
        new ApiTestFactory().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Observability:Enabled", "true");
            builder.UseSetting("Observability:Backend", "Jaeger");
            builder.UseSetting("Observability:Endpoint", "http://localhost:4319");
        });

    [Fact]
    public async Task ExistingFeatures_StillWork_WithNoCollectorListening()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var banks = await client.GetAsync("/api/v1/financial/banks");
        var syncStatus = await client.GetAsync("/api/v1/financial/sync-status");

        banks.StatusCode.Should().Be(HttpStatusCode.OK);
        syncStatus.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RealTracerIsRegistered_AndSpansNeverThrow_WithNoCollectorListening()
    {
        await using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var tracer = scope.ServiceProvider.GetRequiredService<ITelemetryTracer>();

        // Enabled means the real tracer, not the no-op - the export failure happens later,
        // in the SDK's background pipeline, and must never reach the calling code.
        tracer.Should().BeOfType<OpenTelemetryTracer>();
        var act = () =>
        {
            using var span = tracer.StartSpan("Test.Span");
            span.SetAttribute("key", "value");
            span.RecordException(new InvalidOperationException("test"));
        };
        act.Should().NotThrow();
    }
}
