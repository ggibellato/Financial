using System.Net;
using FluentAssertions;

namespace Financial.Api.Tests;

public sealed class SpaFallbackFixture : IDisposable
{
    public const string SpaMarker = "spa-shell-marker";

    private readonly ApiTestFactory _factory = new(spaIndexHtml: $"<html><body>{SpaMarker}</body></html>");

    public SpaFallbackFixture()
    {
        Client = _factory.CreateClient();
    }

    public HttpClient Client { get; }

    public void Dispose()
    {
        Client.Dispose();
        _factory.Dispose();
    }
}

[Trait("Category", "Integration")]
public class SpaFallbackTests(SpaFallbackFixture fixture) : IClassFixture<SpaFallbackFixture>
{
    private readonly HttpClient _client = fixture.Client;

    [Theory]
    [InlineData("/api/v1/financial/does-not-exist")]
    [InlineData("/api")]
    [InlineData("/api/")]
    [InlineData("/api/v2/anything")]
    [InlineData("/API/anything")]
    public async Task ApiPaths_Return404_NotTheSpaShell(string path)
    {
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().NotContain(SpaFallbackFixture.SpaMarker);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/some/client/route")]
    [InlineData("/apiary")]
    public async Task ClientRoute_ReturnsSpaShell(string path)
    {
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        (await response.Content.ReadAsStringAsync()).Should().Contain(SpaFallbackFixture.SpaMarker);
    }

    [Fact]
    public async Task RealApiRoute_StillReturnsJson()
    {
        var response = await _client.GetAsync("/api/v1/financial/banks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}
