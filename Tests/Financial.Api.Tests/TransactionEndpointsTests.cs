using Financial.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class TransactionEndpointsTests
{
    [Fact]
    public async Task AddTransaction_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new TransactionCreateDTO
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

        var response = await client.PostAsJsonAsync("/api/v1/financial/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Transactions.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateTransaction_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
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
        var transactionId = createdAsset!.Transactions.First(t => t.Date == new DateTime(2024, 1, 2)).Id;

        var response = await client.PutAsJsonAsync("/api/v1/financial/transactions", new TransactionUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Id = transactionId,
            Date = new DateTime(2024, 1, 2),
            Type = "Buy",
            Quantity = 2.5m,
            UnitPrice = 12.5m,
            Fees = 1.25m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        var updated = asset!.Transactions.Single(t => t.Id == transactionId);
        updated.Quantity.Should().Be(2.5m);
        updated.UnitPrice.Should().Be(12.5m);
        updated.Fees.Should().Be(1.25m);
    }

    [Fact]
    public async Task DeleteTransaction_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
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
        var transactionId = createdAsset!.Transactions.First(t => t.Date == new DateTime(2024, 1, 3)).Id;

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/financial/transactions")
        {
            Content = JsonContent.Create(new TransactionDeleteDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Id = transactionId
            })
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Transactions.Should().NotContain(t => t.Id == transactionId);
    }
}
