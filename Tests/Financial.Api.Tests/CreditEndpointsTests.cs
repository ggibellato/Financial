using Financial.Investment.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class CreditEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task GetCreditsByBroker_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/v1/financial/credits/broker/XPI");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var credits = await response.Content.ReadFromJsonAsync<CreditDTO[]>();
        credits.Should().NotBeNull();
        credits!.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCreditsByPortfolio_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/v1/financial/credits/portfolio/XPI/Default");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var credits = await response.Content.ReadFromJsonAsync<CreditDTO[]>();
        credits.Should().NotBeNull();
        credits!.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddCredit_ReturnsOk()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
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
    public async Task AddCredit_InvalidType_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 2, 1),
            Type = "NotARealType",
            Value = 5.5m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCredit_UnknownId_ReturnsBadRequest()
    {
        var response = await Client.PutAsJsonAsync("/api/v1/financial/credits", new CreditUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Id = Guid.NewGuid(),
            Date = new DateTime(2024, 2, 1),
            Type = "Dividend",
            Value = 5.5m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteCredit_UnknownId_ReturnsBadRequest()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/financial/credits")
        {
            Content = JsonContent.Create(new CreditDeleteDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Id = Guid.NewGuid()
            })
        };

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCredit_ReturnsOk()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
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

        var response = await Client.PutAsJsonAsync("/api/v1/financial/credits", new CreditUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Id = creditId,
            Date = new DateTime(2024, 2, 2),
            Type = "SecuritiesLendingIncome",
            Value = 6.75m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        var updated = asset!.Credits.Single(credit => credit.Id == creditId);
        updated.Type.Should().Be("SecuritiesLendingIncome");
        updated.Value.Should().Be(6.75m);
    }

    [Fact]
    public async Task DeleteCredit_ReturnsOk()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
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

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Credits.Should().NotContain(credit => credit.Id == creditId);
    }

    [Theory]
    [InlineData("SecuritiesLendingIncome")]
    [InlineData("Coupon")]
    public async Task AddCredit_NewIncomeKind_ReturnsOk(string type)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 2, 6),
            Type = type,
            Value = 5m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset!.Credits.Should().Contain(c => c.Type == type);
    }

    [Fact]
    public async Task AddCredit_WithWithheld_ReturnsNetAmountInResponse()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 2, 7),
            Type = "Dividend",
            Value = 100m,
            Withheld = 15m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset!.Credits.Should().Contain(c => c.Withheld == 15m && c.NetAmount == 85m);
    }

    [Fact]
    public async Task AddCredit_NegativeValue_ReturnsOkAsACorrection()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 2, 8),
            Type = "Dividend",
            Value = -10m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset!.Credits.Should().Contain(c => c.Value == -10m);
    }

    [Fact]
    public async Task GetCreditsByBroker_ScopeHistoric_ExcludesActiveAssetCredits()
    {
        // CLOSEDASSET (Historic) has no credits, while the Active BCIA11 has two dividends
        var response = await Client.GetAsync("/api/v1/financial/credits/broker/XPI?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var credits = await response.Content.ReadFromJsonAsync<CreditDTO[]>();
        credits.Should().NotBeNull();
        credits.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCreditsByPortfolio_ScopeHistoric_ExcludesActivePortfolioCredits()
    {
        var response = await Client.GetAsync("/api/v1/financial/credits/portfolio/XPI/Uncategorized?scope=historic");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var credits = await response.Content.ReadFromJsonAsync<CreditDTO[]>();
        credits.Should().NotBeNull();
        credits.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCreditsByPortfolio_DefaultScope_DoesNotReturnHistoricOnlyPortfolio()
    {
        var response = await Client.GetAsync("/api/v1/financial/credits/portfolio/XPI/Uncategorized");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var credits = await response.Content.ReadFromJsonAsync<CreditDTO[]>();
        credits.Should().NotBeNull();
        credits.Should().BeEmpty();
    }
}
