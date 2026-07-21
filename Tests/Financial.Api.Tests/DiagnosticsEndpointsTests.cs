using Financial.Api.Controllers;
using Financial.Investment.Application.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Financial.Api.Tests;

public class DiagnosticsEndpointsTests
{
    [Fact]
    public void Constructor_NullRepositorySettings_Throws()
    {
        Action act = () => new DiagnosticsController(null!, new StubHostEnvironment());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullEnvironment_Throws()
    {
        Action act = () => new DiagnosticsController(Options.Create(new RepositorySettingsOptions()), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("environment");
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("ok");
    }

    [Fact]
    public async Task GetRepositoryConfig_InDevelopment_ReturnsOk()
    {
        await using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/config/repository");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("provider").GetString().Should().Be("LocalJson");
    }

    [Fact]
    public async Task GetRepositoryConfig_NotInDevelopment_ReturnsNotFound()
    {
        await using var factory = CreateFactory("Production");
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

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Financial.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
