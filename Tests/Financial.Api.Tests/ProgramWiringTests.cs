using Financial.Investment.Application.Configuration;
using Financial.Shared.Abstractions.Persistence;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
public class ProgramWiringTests : ApiEndpointTests
{
    private const string AccountsPath = $"{Api}/accounts";
    private const string ClientOrigin = "http://localhost:5173";

    [Fact]
    public async Task Cors_InProductionWithAConfiguredOrigin_AllowsIt()
    {
        var response = await SendInProductionAsync(
            RequestFrom(HttpMethod.Get, ClientOrigin),
            builder => builder.UseSetting("Cors:AllowedOrigins:0", ClientOrigin));

        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal(ClientOrigin);
    }

    [Fact]
    public async Task Cors_InProductionWithAConfiguredOrigin_PreflightAllowsAnyMethodAndHeader()
    {
        var preflight = RequestFrom(HttpMethod.Options, ClientOrigin);
        preflight.Headers.Add("Access-Control-Request-Method", "DELETE");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await SendInProductionAsync(
            preflight,
            builder => builder.UseSetting("Cors:AllowedOrigins:0", ClientOrigin));

        using var _ = new AssertionScope();
        response.Headers.GetValues("Access-Control-Allow-Methods").Should().Equal("DELETE");
        response.Headers.GetValues("Access-Control-Allow-Headers").Should().Equal("content-type");
    }

    [Fact]
    public async Task Cors_InProductionWithNoConfiguredOrigins_AllowsNoOrigin()
    {
        var response = await SendInProductionAsync(RequestFrom(HttpMethod.Get, ClientOrigin), _ => { });

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task ApiResponses_ReportTheSupportedVersions()
    {
        var response = await Client.GetAsync(AccountsPath);

        response.Headers.GetValues("api-supported-versions").Should().Equal("1.0");
    }

    [Fact]
    public async Task Options_AreBoundFromTheirConfigurationSections()
    {
        await using var baseFactory = new ApiTestFactory();
        await using var factory = baseFactory.WithWebHostBuilder(builder => builder
            .UseSetting("Dividends:DefaultExchange", "LSE")
            .UseSetting("Watchlist:Items:0:Group", "Tech")
            .UseSetting("Watchlist:Items:0:Name", "MSFT")
            .UseSetting("AssetPriceFetch:Portfolios:0:BrokerName", "XPI")
            .UseSetting("AssetPriceFetch:Portfolios:0:PortfolioName", "Default"));

        var dividends = factory.Services.GetRequiredService<IOptions<DividendOptions>>().Value;
        var watchlist = factory.Services.GetRequiredService<IOptions<WatchlistOptions>>().Value;
        var priceFetch = factory.Services.GetRequiredService<IOptions<AssetPriceFetchOptions>>().Value;

        using var _ = new AssertionScope();
        dividends.DefaultExchange.Should().Be("LSE");
        watchlist.Items.Should().ContainSingle(item => item.Group == "Tech" && item.Name == "MSFT");
        priceFetch.Portfolios.Should().ContainSingle(portfolio => portfolio.BrokerName == "XPI" && portfolio.PortfolioName == "Default");
    }

    [Fact]
    public void RemoteFileClientFactory_IsRegistered()
    {
        Services.GetService<IRemoteFileClientFactory>().Should().NotBeNull();
    }

    private static async Task<HttpResponseMessage> SendInProductionAsync(HttpRequestMessage request, Action<IWebHostBuilder> configure)
    {
        await using var baseFactory = new ApiTestFactory();
        await using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            configure(builder);
        });
        using var client = factory.CreateClient();

        return await client.SendAsync(request);
    }

    private static HttpRequestMessage RequestFrom(HttpMethod method, string origin)
    {
        var request = new HttpRequestMessage(method, AccountsPath);
        request.Headers.Add("Origin", origin);
        return request;
    }
}
