using Financial.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Financial.Api.Tests;

public class AssetEndpointsTests
{
    [Fact]
    public async Task GetAssetDetails_ReturnsOk()
    {
        await using var factory = ApiTestFactory.CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/assets/XPI/Default/BCIA11");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Name.Should().Be("BCIA11");
        asset.BrokerName.Should().Be("XPI");
        asset.PortfolioName.Should().Be("Default");
    }
}
