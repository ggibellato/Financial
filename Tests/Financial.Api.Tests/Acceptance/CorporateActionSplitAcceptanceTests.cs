using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests.Acceptance;

public class CorporateActionSplitAcceptanceTests : ApiEndpointTests
{
    [Fact]
    [Trait("AC", "P53-F01-split-and-reverse-split-01")]
    public async Task TwoForOneSplit_DoublesQuantityAndHalvesAveragePrice_WithTotalCostBasisUnchanged()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            RatioFactor = 2.0m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Quantity.Should().Be(16m);
        asset.AveragePrice.Should().Be(50m);
        (asset.Quantity * asset.AveragePrice).Should().Be(800m, "total cost basis must be unchanged by a split");
    }

    [Fact]
    [Trait("AC", "P53-F01-split-and-reverse-split-02")]
    public async Task OneForTenReverseSplit_ReducesQuantityToOneTenthAndMultipliesAveragePriceByTen_WithTotalCostBasisUnchanged()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            RatioFactor = 0.1m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await response.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Quantity.Should().Be(0.8m);
        asset.AveragePrice.Should().Be(1000m);
        (asset.Quantity * asset.AveragePrice).Should().Be(800m, "total cost basis must be unchanged by a reverse split");
    }

    [Fact]
    [Trait("AC", "P53-F01-split-and-reverse-split-03")]
    public async Task SplitOnTheSameDateAsATransaction_IsAppliedBeforeThatTransactionInReplayOrder()
    {
        var buy = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 7, 1),
            Type = "Buy",
            Quantity = 5m,
            UnitPrice = 60m,
            Fees = 0m
        });
        buy.StatusCode.Should().Be(HttpStatusCode.OK);

        var split = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            RatioFactor = 2.0m
        });

        split.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await split.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        asset!.Quantity.Should().Be(21m,
            "the split (8 units -> 16) must apply before the same-day buy (16 + 5 = 21); " +
            "buy-before-split would instead yield (8 + 5) * 2 = 26");
    }

    [Fact]
    [Trait("AC", "P53-F01-split-and-reverse-split-04")]
    public async Task SplitOnAFifoCostedHolding_RescalesTheOpenLotProportionally_WithTotalLotCostUnchanged()
    {
        var setMethod = await Client.PutAsJsonAsync("/api/v1/financial/brokers/XPI/cost-basis-method", new SetCostBasisMethodRequestDTO
        {
            Method = Financial.Investment.Domain.Entities.CostBasisMethod.FIFO
        });
        setMethod.StatusCode.Should().Be(HttpStatusCode.OK);

        var split = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            RatioFactor = 2.0m
        });
        split.StatusCode.Should().Be(HttpStatusCode.OK);

        var sell = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 8, 1),
            Type = "Sell",
            Quantity = 16m,
            UnitPrice = 70m,
            Fees = 0m
        });

        sell.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await sell.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        asset.Should().NotBeNull();
        // BCIA11's seeded 2024-06-01 sell has no persisted DisposalRecord, so it is backfilled the
        // moment the host loads (InvestmentLoader.LoadSync -> DisposalRecordBackfill), independently
        // of this feature; filter to the 2024-08-01 sale this test itself created.
        var disposal = asset!.DisposalRecords.Should().ContainSingle(d => d.Date == new DateTime(2024, 8, 1)).Subject;
        var lot = disposal.LotsConsumed.Should().ContainSingle().Subject;
        lot.Quantity.Should().Be(16m, "the pre-split 8-unit open lot must have been rescaled to 16 units");
        lot.UnitCost.Should().Be(50m, "the pre-split 100 unit cost must have been halved by the 2-for-1 split");
        disposal.CostBasis.Should().Be(800m, "total lot cost (8 x 100 = 16 x 50) must be unchanged by the split");
    }

    [Fact]
    [Trait("AC", "P53-F01-split-and-reverse-split-05")]
    public async Task SplitOnAZeroQuantityHolding_IsRejectedWithNoStateChange()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            EffectiveDate = new DateTime(2023, 1, 1),
            RatioFactor = 2.0m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, "there is no open position to split before the holding's first transaction");

        var asset = await Client.GetFromJsonAsync<AssetDetailsDTO>("/api/v1/financial/assets/XPI/Default/BCIA11");
        asset.Should().NotBeNull();
        asset!.Quantity.Should().Be(8m, "the rejected split must leave the position exactly as it was");
        var repository = Services.GetRequiredService<IInvestmentRepository>();
        repository.GetAsset("XPI", "Default", "BCIA11")!.CorporateActions.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(0)]
    [InlineData(-1.0)]
    [Trait("AC", "P53-F01-split-and-reverse-split-06")]
    public async Task SplitWithARatioOfExactlyOneOrLessThanOrEqualToZero_IsRejected(decimal ratioFactor)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            RatioFactor = ratioFactor
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("AC", "P53-F01-split-and-reverse-split-07")]
    public async Task DeletingASplit_ReTriggersReplayAndRestoresQuantityAndAveragePrice()
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
        var addedAsset = await added.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        addedAsset!.Quantity.Should().Be(16m);

        var repository = Services.GetRequiredService<IInvestmentRepository>();
        var actionId = repository.GetAsset("XPI", "Default", "BCIA11")!.CorporateActions.Single().Id;

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/financial/corporate-actions")
        {
            Content = JsonContent.Create(new CorporateActionDeleteDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Id = actionId
            })
        };
        var deleted = await Client.SendAsync(deleteRequest);

        deleted.StatusCode.Should().Be(HttpStatusCode.OK);
        var deletedAsset = await deleted.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        deletedAsset.Should().NotBeNull();
        deletedAsset!.Quantity.Should().Be(8m);
        deletedAsset.AveragePrice.Should().Be(100m);
    }

    [Fact]
    [Trait("AC", "P53-F01-split-and-reverse-split-08")]
    public async Task RecordingEditingAndDeletingASplit_NeverCreatesOrModifiesADisposalRecord()
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
        var addedAsset = await added.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        // BCIA11's seeded 2024-06-01 sell has no persisted DisposalRecord, so it is backfilled the
        // moment the host loads (InvestmentLoader.LoadSync -> DisposalRecordBackfill), independently
        // of this feature. The split's effective date (2024-07-01) is after that sell, so recording
        // it must leave that pre-existing record completely untouched: same id, same Active status,
        // no additional record created alongside it.
        var preExisting = addedAsset!.DisposalRecords.Should().ContainSingle(d => d.Status == DisposalRecordStatus.Active).Subject;

        var repository = Services.GetRequiredService<IInvestmentRepository>();
        var actionId = repository.GetAsset("XPI", "Default", "BCIA11")!.CorporateActions.Single().Id;

        var updated = await Client.PutAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Id = actionId,
            EffectiveDate = new DateTime(2024, 7, 1),
            RatioFactor = 4.0m
        });
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedAsset = await updated.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        updatedAsset!.DisposalRecords.Should().ContainSingle(d => d.Id == preExisting.Id && d.Status == DisposalRecordStatus.Active);

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/financial/corporate-actions")
        {
            Content = JsonContent.Create(new CorporateActionDeleteDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Id = actionId
            })
        };
        var deleted = await Client.SendAsync(deleteRequest);
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);
        var deletedAsset = await deleted.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        deletedAsset!.DisposalRecords.Should().ContainSingle(d => d.Id == preExisting.Id && d.Status == DisposalRecordStatus.Active);
    }
}
