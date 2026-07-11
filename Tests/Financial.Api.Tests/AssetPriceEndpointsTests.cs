using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class AssetPriceEndpointsTests
{
    [Fact]
    public async Task GetCurrentPrice_ReturnsOk()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/prices/current?exchange=BVMF&ticker=BCIA11");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var price = await response.Content.ReadFromJsonAsync<AssetPriceDTO>();
        price.Should().NotBeNull();
        price!.Exchange.Should().Be("BVMF");
        price.Ticker.Should().Be("BCIA11");
        price.Price.Should().Be(10.5m);
    }

    [Fact]
    public async Task GetCurrentPrice_WhenMissingTicker_ReturnsBadRequest()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/prices/current?exchange=BVMF");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCurrentPrice_WithAssetClassAndBrokerName_ReturnsOk()
    {
        var stub = new AssetPriceServiceStub();
        await using var factory = CreateFactory(stub);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/prices/current?ticker=BTC&assetClass=Cryptocurrency&brokerName=Coinbase");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stub.LastRequest.Should().NotBeNull();
        stub.LastRequest!.AssetClass.Should().Be(GlobalAssetClass.Cryptocurrency);
        stub.LastRequest.BrokerName.Should().Be("Coinbase");
    }

    [Fact]
    public async Task GetCurrentPrice_CryptocurrencyWithoutBrokerName_ReturnsBadRequest()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/prices/current?ticker=BTC&assetClass=Cryptocurrency");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCurrentPrice_UnrecognizedAssetClass_DefaultsToUnknownAndRequiresExchange()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/prices/current?ticker=BTC&assetClass=NotARealClass");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static WebApplicationFactory<Program> CreateFactory(AssetPriceServiceStub? stub = null)
    {
        return new ApiTestFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAssetPriceService>();
                services.AddSingleton<IAssetPriceService>(stub ?? new AssetPriceServiceStub());
            });
        });
    }

    private sealed class AssetPriceServiceStub : IAssetPriceService
    {
        public AssetPriceRequestDTO? LastRequest { get; private set; }

        public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request)
        {
            LastRequest = request;

            if (string.IsNullOrWhiteSpace(request.Ticker))
            {
                throw new ArgumentException("Ticker is required.", nameof(request));
            }

            return new AssetPriceDTO
            {
                Exchange = request.Exchange,
                Ticker = request.Ticker,
                Name = "Sample Asset",
                Price = 10.5m,
                AsOf = new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero)
            };
        }
    }
}
