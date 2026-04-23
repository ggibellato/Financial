using Financial.Application.DTOs;
using FluentAssertions;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
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

    [Fact]
    public async Task UpdateOperation_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/operations", new OperationCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 1, 2),
            Type = "Buy",
            Quantity = 1,
            UnitPrice = 10,
            Fees = 0
        });

        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdAsset = await created.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        createdAsset.Should().NotBeNull();
        var operationId = createdAsset!.Operations.First(op => op.Date == new DateTime(2024, 1, 2)).Id;

        var response = await client.PutAsJsonAsync("/api/v1/financial/operations", new OperationUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Id = operationId,
            Date = new DateTime(2024, 1, 2),
            Type = "Buy",
            Quantity = 2.5m,
            UnitPrice = 12.5m,
            Fees = 1.25m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        var updated = asset!.Operations.Single(op => op.Id == operationId);
        updated.Quantity.Should().Be(2.5m);
        updated.UnitPrice.Should().Be(12.5m);
        updated.Fees.Should().Be(1.25m);
    }

    [Fact]
    public async Task DeleteOperation_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/operations", new OperationCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 1, 3),
            Type = "Sell",
            Quantity = 1,
            UnitPrice = 15,
            Fees = 0
        });

        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdAsset = await created.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        createdAsset.Should().NotBeNull();
        var operationId = createdAsset!.Operations.First(op => op.Date == new DateTime(2024, 1, 3)).Id;

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/financial/operations")
        {
            Content = JsonContent.Create(new OperationDeleteDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Id = operationId
            })
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Operations.Should().NotContain(op => op.Id == operationId);
    }
}
