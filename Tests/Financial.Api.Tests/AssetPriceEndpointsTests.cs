using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
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

    [Fact]
    public async Task GetCurrentPrice_BondWithName_ReturnsOk()
    {
        var stub = new AssetPriceServiceStub();
        await using var factory = CreateFactory(stub);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/prices/current?ticker=TESOURO+IPCA%2B+2029&assetClass=Bond&name=TESOURO+IPCA%2B+2029");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stub.LastRequest.Should().NotBeNull();
        stub.LastRequest!.AssetClass.Should().Be(GlobalAssetClass.Bond);
        stub.LastRequest.Name.Should().Be("TESOURO IPCA+ 2029");
    }

    [Fact]
    public async Task GetCurrentPrice_BondWithoutName_ReturnsBadRequest()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/prices/current?ticker=TESOURO+IPCA%2B+2029&assetClass=Bond");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetPrice_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync("/api/v1/financial/prices", new SetAssetPriceDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateOnly(2026, 8, 15),
            Price = 123.45m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.PriceHistory.Should().Contain(p => p.Date == new DateOnly(2026, 8, 15) && p.Price == 123.45m && p.IsManual);
    }

    [Fact]
    public async Task SetPrice_ZeroPrice_ReturnsBadRequest()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync("/api/v1/financial/prices", new SetAssetPriceDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateOnly(2026, 8, 15),
            Price = 0m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetPrice_FutureDate_ReturnsBadRequest()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync("/api/v1/financial/prices", new SetAssetPriceDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = DateOnly.FromDateTime(DateTime.Today).AddDays(1),
            Price = 100m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetPrice_UnknownAsset_ReturnsBadRequest()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync("/api/v1/financial/prices", new SetAssetPriceDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "NoSuchAsset",
            Date = new DateOnly(2026, 8, 15),
            Price = 100m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeletePrice_ManualEntry_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var date = new DateOnly(2026, 8, 15);
        await client.PutAsJsonAsync("/api/v1/financial/prices", new SetAssetPriceDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = date,
            Price = 100m
        });

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/financial/prices")
        {
            Content = JsonContent.Create(new DeleteAssetPriceDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Date = date
            })
        };
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.PriceHistory.Should().NotContain(p => p.Date == date);
    }

    [Fact]
    public async Task DeletePrice_NoEntryForDate_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/financial/prices")
        {
            Content = JsonContent.Create(new DeleteAssetPriceDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Date = new DateOnly(2026, 8, 15)
            })
        };
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletePrice_AutomaticEntry_ReturnsBadRequest()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var date = new DateOnly(2026, 8, 15);
        var repository = factory.Services.GetRequiredService<IInvestmentRepository>();
        repository.GetAsset("XPI", "Default", "BCIA11")!.SetPrice(date, 100m, isManual: false);

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/financial/prices")
        {
            Content = JsonContent.Create(new DeleteAssetPriceDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Date = date
            })
        };
        var response = await client.SendAsync(request);

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

            if (request.AssetClass == GlobalAssetClass.Cryptocurrency && string.IsNullOrWhiteSpace(request.BrokerName))
            {
                throw new ArgumentException("BrokerName is required for cryptocurrency assets.", nameof(request));
            }

            if (request.AssetClass == GlobalAssetClass.Bond && string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Name is required for bond assets.", nameof(request));
            }

            if (request.AssetClass != GlobalAssetClass.Cryptocurrency
                && request.AssetClass != GlobalAssetClass.Bond
                && string.IsNullOrWhiteSpace(request.Exchange))
            {
                throw new ArgumentException("Exchange is required.", nameof(request));
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
