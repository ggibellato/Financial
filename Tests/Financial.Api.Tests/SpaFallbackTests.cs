using System.Net;
using FluentAssertions;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
public class SpaFallbackTests : IDisposable
{
    private const string SpaMarker = "spa-shell-marker";

    private readonly ApiTestFactory _factory = new(spaIndexHtml: $"<html><body>{SpaMarker}</body></html>");
    private readonly HttpClient _client;

    public SpaFallbackTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task UnknownApiRoute_Returns404_NotHtml()
    {
        var response = await _client.GetAsync("/api/v1/financial/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().NotBe("text/html");
        (await response.Content.ReadAsStringAsync()).Should().NotContain(SpaMarker);
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/")]
    [InlineData("/api/v2/anything")]
    public async Task ApiPaths_Return404(string path)
    {
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().NotContain(SpaMarker);
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
        (await response.Content.ReadAsStringAsync()).Should().Contain(SpaMarker);
    }

    [Fact]
    public async Task WrongMethodOnRealRoute_StillReturns405()
    {
        var response = await _client.PostAsync("/api/v1/financial/health", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task RealApiRoute_StillReturnsJson()
    {
        var response = await _client.GetAsync("/api/v1/financial/banks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}
