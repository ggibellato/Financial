using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
public class SwaggerEndpointsTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task GetSwaggerUI_ReturnsOk(string environment)
    {
        await using var factory = CreateFactory(environment);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOpenApiDocument_InDevelopment_ReturnsValidJson()
    {
        await using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("openapi", out _).Should().BeTrue();
    }

    private static WebApplicationFactory<Program> CreateFactory(string environment)
    {
        return new ApiTestFactory().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
        });
    }
}
