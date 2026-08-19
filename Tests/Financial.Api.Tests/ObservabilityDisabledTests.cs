using System.Net;
using Financial.Shared.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests;

public class ObservabilityDisabledTests
{
    [Fact]
    public async Task GetSyncStatus_Succeeds_WithObservabilityDisabledAndNoTelemetryEndpointReachable()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/sync-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TelemetryTracer_ResolvesToUsableNoOp_WithObservabilityDisabled()
    {
        await using var factory = new ApiTestFactory();
        using var scope = factory.Services.CreateScope();

        var tracer = scope.ServiceProvider.GetRequiredService<ITelemetryTracer>();
        using var span = tracer.StartSpan("Test.Span");
        span.SetAttribute("key", "value");
        span.RecordException(new InvalidOperationException("test"));
    }
}
