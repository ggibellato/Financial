using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

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

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new ApiTestFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAssetPriceService>();
                services.AddSingleton<IAssetPriceService>(new AssetPriceServiceStub());
            });
        });
    }

    private sealed class AssetPriceServiceStub : IAssetPriceService
    {
        public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Exchange) || string.IsNullOrWhiteSpace(request.Ticker))
            {
                throw new ArgumentException("Exchange and ticker are required.", nameof(request));
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
