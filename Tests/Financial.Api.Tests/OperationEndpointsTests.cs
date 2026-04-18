using Financial.Application.DTOs;
using FluentAssertions;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Financial.Api.Tests;

public class OperationEndpointsTests
{
    [Fact]
    public async Task AddOperation_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new OperationCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = DateTime.UtcNow,
            Type = "Buy",
            Quantity = 1,
            UnitPrice = 10,
            Fees = 0
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/operations", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Operations.Count.Should().BeGreaterThan(0);
    }
}
