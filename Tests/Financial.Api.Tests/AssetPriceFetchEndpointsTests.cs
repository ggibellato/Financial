using Financial.Investment.Application.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Financial.Api.Tests;

public class AssetPriceFetchEndpointsTests
{
    private static readonly JsonSerializerOptions CaseInsensitiveJson = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Boots the API with the AssetPriceFetchOptions a test needs; the configuration callback is the only
    /// thing that differs between these tests.</summary>
    private static WebApplicationFactory<Program> CreateFactory(Action<AssetPriceFetchOptions> configure) =>
        new ApiTestFactory().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.PostConfigure(configure)));

    [Fact]
    public async Task GetAssetPriceFetch_ReturnsOk_WithConfiguredPortfolios()
    {
        await using var factory = CreateFactory(options =>
        {
            options.Portfolios =
            [
                new AssetPriceFetch { BrokerName = "XPI", PortfolioName = "FII" },
                new AssetPriceFetch { BrokerName = "XPI", PortfolioName = "Acoes" },
            ];
        });

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/asset-price-fetch");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var portfolios = await response.Content.ReadFromJsonAsync<AssetPriceFetch[]>(CaseInsensitiveJson);
        portfolios.Should().HaveCount(2);
        portfolios![0].BrokerName.Should().Be("XPI");
        portfolios[0].PortfolioName.Should().Be("FII");
        portfolios[1].PortfolioName.Should().Be("Acoes");
    }

    [Fact]
    public async Task GetAssetPriceFetch_ReturnsEmptyArray_WhenNoPortfoliosConfigured()
    {
        await using var factory = CreateFactory(options => options.Portfolios.Clear());

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/asset-price-fetch");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var portfolios = await response.Content.ReadFromJsonAsync<AssetPriceFetch[]>(CaseInsensitiveJson);
        portfolios.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAssetPriceFetch_JsonUsesBrokerNameAndPortfolioNameProperties()
    {
        await using var factory = CreateFactory(options =>
        {
            options.Portfolios = [new AssetPriceFetch { BrokerName = "XPI", PortfolioName = "FII" }];
        });

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/asset-price-fetch");
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var first = doc.RootElement[0];
        first.TryGetProperty("brokerName", out _).Should().BeTrue("frontend expects camelCase 'brokerName'");
        first.TryGetProperty("portfolioName", out _).Should().BeTrue("frontend expects camelCase 'portfolioName'");
    }
}
