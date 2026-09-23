using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests.Acceptance;

public class CorporateActionSpinOffAcceptanceTests : ApiEndpointTests
{
    [Fact]
    [Trait("AC", "P53-F03-spinoff-01")]
    public async Task RecordingASpinOff_LeavesParentQuantityUnchanged_AndReducesCostBasisByExactlyAllocationPercentTimesPriorCostBasis()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/spin-off", new CorporateActionSpinOffCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            ParentAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            QuantityReceived = 5m,
            AllocationPercentage = 15m,
            NewAssetName = "SPINCO",
            CreateNewAssetInline = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CorporateActionSpinOffResultDTO>();
        result.Should().NotBeNull();
        result!.Parent!.Quantity.Should().Be(8m, "a spin-off does not reduce the parent's unit count");
        (result.Parent.Quantity * result.Parent.AveragePrice).Should().Be(680m, "800 prior cost basis minus 15% (120) moved to the new asset");
    }

    [Fact]
    [Trait("AC", "P53-F03-spinoff-02")]
    public async Task TheNewAssetsQuantityEqualsEnteredReceivedQuantity_WithUnitCostComputedFromTheAllocation()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/spin-off", new CorporateActionSpinOffCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            ParentAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            QuantityReceived = 5m,
            AllocationPercentage = 15m,
            NewAssetName = "SPINCO",
            CreateNewAssetInline = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CorporateActionSpinOffResultDTO>();
        result!.New!.Quantity.Should().Be(5m, "the entered received quantity, never derived from a ratio");
        result.New.AveragePrice.Should().Be(24m, "(15% x 800 = 120) / 5 units received");
    }

    [Fact]
    [Trait("AC", "P53-F03-spinoff-03")]
    public async Task EveryOpenLotOnAFifoCostedParent_IsProportionallyReducedByTheAllocationPercentage()
    {
        var setMethod = await Client.PutAsJsonAsync("/api/v1/financial/brokers/XPI/cost-basis-method", new SetCostBasisMethodRequestDTO
        {
            Method = CostBasisMethod.FIFO
        });
        setMethod.StatusCode.Should().Be(HttpStatusCode.OK);

        var spinOff = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/spin-off", new CorporateActionSpinOffCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            ParentAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            QuantityReceived = 5m,
            AllocationPercentage = 15m,
            NewAssetName = "SPINCO",
            CreateNewAssetInline = true
        });
        spinOff.StatusCode.Should().Be(HttpStatusCode.OK);

        var sell = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateTime(2024, 8, 1),
            Type = "Sell",
            Quantity = 8m,
            UnitPrice = 90m,
            Fees = 0m
        });

        sell.StatusCode.Should().Be(HttpStatusCode.OK);
        var asset = await sell.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        var disposal = asset!.DisposalRecords.Should().ContainSingle(d => d.Date == new DateTime(2024, 8, 1)).Subject;
        var lot = disposal.LotsConsumed.Should().ContainSingle().Subject;
        lot.Quantity.Should().Be(8m, "a spin-off never rescales lot quantity");
        lot.UnitCost.Should().Be(85m, "the pre-spin-off 100 unit cost reduced by the 15% allocation");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [Trait("AC", "P53-F03-spinoff-04")]
    public async Task AnAllocationPercentageOutside0To100_IsRejectedWithAnInlineError(decimal allocationPercentage)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/spin-off", new CorporateActionSpinOffCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            ParentAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            QuantityReceived = 5m,
            AllocationPercentage = allocationPercentage,
            NewAssetName = "SPINCO",
            CreateNewAssetInline = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [Trait("AC", "P53-F03-spinoff-05")]
    public async Task AQuantityReceivedLessThanOrEqualToZero_IsRejectedWithAnInlineError(decimal quantityReceived)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/spin-off", new CorporateActionSpinOffCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            ParentAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            QuantityReceived = quantityReceived,
            AllocationPercentage = 15m,
            NewAssetName = "SPINCO",
            CreateNewAssetInline = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("AC", "P53-F03-spinoff-06")]
    public async Task ATaxClassificationIsCreatedForTheNewAsset_WithStatusRequiresReview_WhenNoMatchingTaxRuleExists()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/spin-off", new CorporateActionSpinOffCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            ParentAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            QuantityReceived = 5m,
            AllocationPercentage = 15m,
            NewAssetName = "SPINCO",
            CreateNewAssetInline = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var repository = Services.GetRequiredService<IInvestmentRepository>();
        var newAsset = repository.GetAsset("XPI", "Default", "SPINCO");
        var classification = newAsset!.TaxClassifications.Should().ContainSingle().Subject;
        classification.CalculationStatus.Should().Be(CalculationStatus.RequiresReview);
        classification.SourceType.Should().Be(SourceType.CorporateAction);
    }

    [Fact]
    [Trait("AC", "P53-F03-spinoff-07")]
    public async Task NoDisposalRecordIsCreatedOnTheParentAsset()
    {
        var before = await Client.GetFromJsonAsync<AssetDetailsDTO>("/api/v1/financial/assets/XPI/Default/BCIA11");
        // BCIA11's seeded 2024-06-01 sell has no persisted DisposalRecord, so it is backfilled the
        // moment the host loads (InvestmentLoader.LoadSync -> DisposalRecordBackfill), independently
        // of this feature. The spin-off's effective date (2024-07-01) is after that sell, so recording
        // it must leave that pre-existing record completely untouched and create no new one.
        var preExisting = before!.DisposalRecords.Should().ContainSingle(d => d.Status == DisposalRecordStatus.Active).Subject;

        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/spin-off", new CorporateActionSpinOffCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            ParentAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            QuantityReceived = 5m,
            AllocationPercentage = 15m,
            NewAssetName = "SPINCO",
            CreateNewAssetInline = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CorporateActionSpinOffResultDTO>();
        result!.Parent!.DisposalRecords.Should().ContainSingle(d => d.Id == preExisting.Id && d.Status == DisposalRecordStatus.Active,
            "reducing cost basis without reducing quantity is not a disposal");
    }
}
