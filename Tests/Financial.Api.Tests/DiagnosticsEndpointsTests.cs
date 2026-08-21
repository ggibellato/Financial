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

    [Fact]
    public async Task GetRepositoryConfig_InDevelopment_ReturnsOk()
    {
        await using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/config/repository");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("investment").GetProperty("provider").GetString().Should().Be("LocalJson");
    }

    [Fact]
    public async Task GetRepositoryConfig_InDevelopment_IncludesTheActualPaths()
    {
        await using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/config/repository");

        var investment = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("investment");

        using (new AssertionScope())
        {
            investment.GetProperty("dataJsonFile").GetString().Should().NotBeNullOrWhiteSpace();
            investment.GetProperty("dataJsonFileConfigured").GetBoolean().Should().BeTrue();
        }
    }

    /// <summary>
    /// Previously a 404, which made the endpoint useless in the only environment whose configuration
    /// you cannot simply read off your own machine.
    /// </summary>
    [Fact]
    public async Task GetRepositoryConfig_OutsideDevelopment_ReturnsOkRatherThanNotFound()
    {
        await using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/config/repository");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// The API has no authentication, so the paths themselves stay out of a production response -
    /// the flags answer "is it configured" without disclosing the filesystem or credential layout.
    /// </summary>
    [Fact]
    public async Task GetRepositoryConfig_OutsideDevelopment_WithholdsPathsButKeepsProviderAndFlags()
    {
        await using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/config/repository");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        using (new AssertionScope())
        {
            foreach (var context in new[] { "investment", "cashFlow" })
            {
                var reported = body.GetProperty(context);
                reported.GetProperty("provider").GetString().Should().Be("LocalJson");
                reported.GetProperty("dataJsonFileConfigured").GetBoolean().Should().BeTrue();
                reported.GetProperty("dataJsonFile").ValueKind.Should().Be(JsonValueKind.Null);
                reported.GetProperty("googleDriveCredentialsPath").ValueKind.Should().Be(JsonValueKind.Null);
                reported.GetProperty("googleDriveFilePath").ValueKind.Should().Be(JsonValueKind.Null);
            }
        }
    }

    [Fact]
    public async Task GetRepositoryConfig_ReportsBothContexts()
    {
        await using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/config/repository");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        using (new AssertionScope())
        {
            body.TryGetProperty("investment", out _).Should().BeTrue();
            body.TryGetProperty("cashFlow", out _).Should().BeTrue("a CashFlow misconfiguration was invisible here");
        }
    }

    private static WebApplicationFactory<Program> CreateFactory(string environment)
    {
        return new ApiTestFactory().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
        });
    }
}
