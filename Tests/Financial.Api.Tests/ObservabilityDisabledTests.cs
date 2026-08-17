using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Financial.Api.Tests;

public class ObservabilityDisabledTests
{
    [Fact]
    public async Task GetSyncStatus_WithObservabilityDisabledAndNoCollectorRunning_SucceedsNormally()
    {
        await using var factory = new ApiTestFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Observability:Enabled"] = "false"
                })));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/sync-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WithObservabilityDisabled_NoTracerOrMeterProviderIsRegistered()
    {
        await using var factory = new ApiTestFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Observability:Enabled"] = "false"
                })));

        factory.Services.GetService<TracerProvider>().Should().BeNull();
        factory.Services.GetService<MeterProvider>().Should().BeNull();
    }
}
