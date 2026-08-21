using Financial.Api.Controllers;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Financial.Api.Tests;

public class DiagnosticsEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/v1/financial/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("ok");
    }

    /// <summary>
    /// Reporting only Investment left a CashFlow misconfiguration invisible in the one endpoint you
    /// would check it from.
    /// </summary>
    [Fact]
    public async Task GetHealth_ReportsBothContextsProviderAndSyncState()
    {
        var response = await Client.GetAsync("/api/v1/financial/health");

        var contexts = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("contexts");

        using (new AssertionScope())
        {
            foreach (var context in new[] { "investment", "cashFlow" })
            {
                var reported = contexts.GetProperty(context);
                reported.GetProperty("provider").GetString().Should().Be("LocalJson");
                reported.GetProperty("sync").GetString().Should().Be("Idle");
                reported.TryGetProperty("lastSuccessfulSaveUtc", out _).Should().BeTrue();
            }
        }
    }

    /// <summary>
    /// The endpoint is a readiness probe - CI polls it in a boot loop, and a container healthcheck
    /// would too. It must stay 200 for a storage fault, which a restart cannot fix and would in fact
    /// make worse, since startup re-reads from the same failing storage.
    /// </summary>
    [Fact]
    public async Task GetHealth_AnswersRepeatedProbesWithOk()
    {
        var first = await Client.GetAsync("/api/v1/financial/health");
        var second = await Client.GetAsync("/api/v1/financial/health");

        using (new AssertionScope())
        {
            first.StatusCode.Should().Be(HttpStatusCode.OK);
            second.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    /// <summary>
    /// The repository-config endpoint was removed rather than gated: its only guard was
    /// ASPNETCORE_ENVIRONMENT, which is a runtime setting that can be - and in docker-compose.yml
    /// is - Development. This pins the route as gone in both environments, so no setting can bring
    /// it back.
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task GetRepositoryConfig_IsNoLongerServed(string environment)
    {
        await using var factory = CreateFactory(environment);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/config/repository");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static WebApplicationFactory<Program> CreateFactory(string environment)
    {
        return new ApiTestFactory().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
        });
    }
}
