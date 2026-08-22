using System.Net;
using Financial.Shared.Abstractions.Observability;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests;

public class ObservabilityDisabledTests : ApiEndpointTests
{
    [Fact]
    public async Task GetSyncStatus_Succeeds_WithObservabilityDisabledAndNoTelemetryEndpointReachable()
    {
        var response = await Client.GetAsync("/api/v1/financial/sync-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TelemetryTracer_ResolvesToUsableNoOp_WithObservabilityDisabled()
    {
        using var scope = Services.CreateScope();

        var tracer = scope.ServiceProvider.GetRequiredService<ITelemetryTracer>();
        using var span = tracer.StartSpan("Test.Span");
        span.SetAttribute("key", "value");
        span.RecordException(new InvalidOperationException("test"));
    }
}
