using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class AssetEndpointsTests
{
    [Fact]
    public async Task GetAssetDetails_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/assets/XPI/Default/BCIA11");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Name.Should().Be("BCIA11");
        asset.BrokerName.Should().Be("XPI");
        asset.PortfolioName.Should().Be("Default");
    }

    [Fact]
    public async Task GetAssetDetails_ScopeHistoric_ResolvesHistoricAsset()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/assets/XPI/Uncategorized/CLOSEDASSET?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Name.Should().Be("CLOSEDASSET");
        asset.PositionType.Should().Be(PositionType.Flat);
    }

    [Fact]
    public async Task GetAssetDetails_ScopeActive_HistoricAssetNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/assets/XPI/Uncategorized/CLOSEDASSET?scope=active");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
