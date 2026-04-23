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

public class CreditEndpointsTests
{
    [Fact]
    public async Task GetCreditsByBroker_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/credits/broker/XPI");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var credits = await response.Content.ReadFromJsonAsync<CreditDTO[]>();
        credits.Should().NotBeNull();
        credits!.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCreditsByPortfolio_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/credits/portfolio/XPI/Default");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var credits = await response.Content.ReadFromJsonAsync<CreditDTO[]>();
        credits.Should().NotBeNull();
        credits!.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddCredit_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 2, 1),
            Type = "Dividend",
            Value = 5.5m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Credits.Should().Contain(credit => credit.Date == new DateTime(2024, 2, 1));
    }

    [Fact]
    public async Task UpdateCredit_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 2, 2),
            Type = "Dividend",
            Value = 4.25m
        });

        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdAsset = await created.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        createdAsset.Should().NotBeNull();
        var creditId = createdAsset!.Credits.First(credit => credit.Date == new DateTime(2024, 2, 2)).Id;

        var response = await client.PutAsJsonAsync("/api/v1/financial/credits", new CreditUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Id = creditId,
            Date = new DateTime(2024, 2, 2),
            Type = "Rent",
            Value = 6.75m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        var updated = asset!.Credits.Single(credit => credit.Id == creditId);
        updated.Type.Should().Be("Rent");
        updated.Value.Should().Be(6.75m);
    }

    [Fact]
    public async Task DeleteCredit_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 2, 3),
            Type = "Dividend",
            Value = 3m
        });

        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdAsset = await created.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        createdAsset.Should().NotBeNull();
        var creditId = createdAsset!.Credits.First(credit => credit.Date == new DateTime(2024, 2, 3)).Id;

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/financial/credits")
        {
            Content = JsonContent.Create(new CreditDeleteDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Id = creditId
            })
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Credits.Should().NotContain(credit => credit.Id == creditId);
    }
}
