using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class CorporateActionsEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task AddSplit_ValidRequest_ReturnsOk()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            RatioFactor = 2.0m,
            Note = "2-for-1 split"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Quantity.Should().Be(16m);
    }

    [Fact]
    public async Task AddSplit_InvalidRatioFactor_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            RatioFactor = 1.0m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddSplit_ZeroQuantityHolding_ReturnsConflict()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            EffectiveDate = new DateTime(2023, 1, 1),
            RatioFactor = 2.0m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, "an unrescalable zero-quantity position is a well-formed request the domain refuses on its own rules, not a malformed one");
    }

    [Fact]
    public async Task AddSplit_UnknownAsset_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "UNKNOWN",
            EffectiveDate = new DateTime(2024, 7, 1),
            RatioFactor = 2.0m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "matches the existing TransactionsController/AssetMutationHelper contract: an unresolvable broker/portfolio/asset context returns null, mapped to 400");
    }

    [Fact]
    public async Task UpdateSplit_UnknownId_ReturnsBadRequest()
    {
        var response = await Client.PutAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Id = Guid.NewGuid(),
            EffectiveDate = new DateTime(2024, 7, 1),
            RatioFactor = 2.0m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSplit_ReturnsOk()
    {
        var added = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            RatioFactor = 2.0m
        });
        added.StatusCode.Should().Be(HttpStatusCode.OK);

        var repository = Services.GetRequiredService<IInvestmentRepository>();
        var actionId = repository.GetAsset("XPI", "Default", "BCIA11")!.CorporateActions.Single().Id;

        var response = await Client.PutAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Id = actionId,
            EffectiveDate = new DateTime(2024, 7, 1),
            RatioFactor = 4.0m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Quantity.Should().Be(32m);
    }

    [Fact]
    public async Task DeleteCorporateAction_UnknownId_ReturnsBadRequest()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/financial/corporate-actions")
        {
            Content = JsonContent.Create(new CorporateActionDeleteDTO
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
    public async Task DeleteCorporateAction_ReturnsOk()
    {
        var added = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            RatioFactor = 2.0m
        });
        added.StatusCode.Should().Be(HttpStatusCode.OK);

        var repository = Services.GetRequiredService<IInvestmentRepository>();
        var actionId = repository.GetAsset("XPI", "Default", "BCIA11")!.CorporateActions.Single().Id;

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/financial/corporate-actions")
        {
            Content = JsonContent.Create(new CorporateActionDeleteDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Id = actionId
            })
        };

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Quantity.Should().Be(8m);
    }

    [Fact]
    public async Task DeleteCorporateAction_WhenALaterSpecificIdDisposalDependsOnTheSplitLots_ReturnsConflict()
    {
        // A dedicated broker/portfolio/asset, rather than the seeded XPI/Default/BCIA11: BCIA11's
        // seeded sell has no lot allocation and no persisted DisposalRecord (it is backfilled at
        // host startup, unrelated to this feature), so switching its broker to SpecificId cannot
        // reconstruct an allocation for it and fails before this scenario even starts.
        var broker = await Client.PostAsJsonAsync("/api/v1/financial/brokers", new BrokerCreateDTO
        {
            Name = "SpecificIdBroker",
            Currency = "GBP",
            CostBasisMethod = Financial.Investment.Domain.Entities.CostBasisMethod.SpecificId
        });
        broker.StatusCode.Should().Be(HttpStatusCode.OK);

        var portfolio = await Client.PostAsJsonAsync("/api/v1/financial/portfolios", new PortfolioCreateDTO
        {
            BrokerName = "SpecificIdBroker",
            Name = "Main"
        });
        portfolio.StatusCode.Should().Be(HttpStatusCode.OK);

        var asset = await Client.PostAsJsonAsync("/api/v1/financial/assets", new AssetAdminCreateDTO
        {
            BrokerName = "SpecificIdBroker",
            PortfolioName = "Main",
            Name = "SPLITASSET"
        });
        asset.StatusCode.Should().Be(HttpStatusCode.OK);

        var buy = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = "SpecificIdBroker",
            PortfolioName = "Main",
            AssetName = "SPLITASSET",
            Date = new DateTime(2024, 1, 1),
            Type = "Buy",
            Quantity = 5m,
            UnitPrice = 5m,
            Fees = 0m
        });
        buy.StatusCode.Should().Be(HttpStatusCode.OK);
        var lotId = (await buy.Content.ReadFromJsonAsync<AssetDetailsDTO>())!.Transactions.Single().Id;

        var added = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitCreateDTO
        {
            BrokerName = "SpecificIdBroker",
            PortfolioName = "Main",
            AssetName = "SPLITASSET",
            EffectiveDate = new DateTime(2024, 2, 1),
            RatioFactor = 2.0m
        });
        added.StatusCode.Should().Be(HttpStatusCode.OK);
        var repository = Services.GetRequiredService<IInvestmentRepository>();
        var actionId = repository.GetAsset("SpecificIdBroker", "Main", "SPLITASSET")!.CorporateActions.Single().Id;

        var sell = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = "SpecificIdBroker",
            PortfolioName = "Main",
            AssetName = "SPLITASSET",
            Date = new DateTime(2024, 3, 1),
            Type = "Sell",
            Quantity = 10m,
            UnitPrice = 6m,
            Fees = 0m,
            SpecificLotAllocations = [new SpecificLotAllocationDTO { SourceTransactionId = lotId, Quantity = 10m }]
        });
        sell.StatusCode.Should().Be(HttpStatusCode.OK);

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/financial/corporate-actions")
        {
            Content = JsonContent.Create(new CorporateActionDeleteDTO
            {
                BrokerName = "SpecificIdBroker",
                PortfolioName = "Main",
                AssetName = "SPLITASSET",
                Id = actionId
            })
        };

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problem!.Detail.Should().Be("Cannot delete: a later disposal depends on lots created by this split.");
    }
}
