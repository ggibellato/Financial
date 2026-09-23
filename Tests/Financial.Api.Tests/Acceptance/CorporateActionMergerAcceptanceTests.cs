using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests.Acceptance;

public class CorporateActionMergerAcceptanceTests : ApiEndpointTests
{
    [Fact]
    [Trait("AC", "P53-F02-merger-01")]
    public async Task RecordingAMerger_ReducesSourceQuantityToZero_AndClosesAllItsOpenLots()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/merger", new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "XCORP",
            CreateTargetAssetInline = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CorporateActionMergerResultDTO>();
        result.Should().NotBeNull();
        result!.Source!.Quantity.Should().Be(0m, "the source position must fully close");
        result.Source.AveragePrice.Should().Be(0m, "a closed position carries no average cost");
    }

    [Fact]
    [Trait("AC", "P53-F02-merger-02")]
    public async Task TargetQuantityIncreasesBySourceQuantityTimesRatio_AndCostBasisIncreasesBySourcesFullCostBasis()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/merger", new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "XCORP",
            CreateTargetAssetInline = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CorporateActionMergerResultDTO>();
        result!.Target!.Quantity.Should().Be(16m, "8 source units x 2.0 exchange ratio");
        (result.Target.Quantity * result.Target.AveragePrice).Should().Be(800m, "the source's full 8 x 100 cost basis is carried over");
    }

    [Fact]
    [Trait("AC", "P53-F02-merger-03")]
    public async Task CreateInlineForANonExistentTargetAsset_CreatesItWithTheEnteredIdentityFields_AndLinksTheMergerToIt()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/merger", new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            ExchangeRatio = 0.5m,
            TargetAssetName = "XCORP",
            CreateTargetAssetInline = true,
            TargetISIN = "US00000X1234",
            TargetExchange = "NASDAQ",
            TargetTicker = "XCORP",
            TargetCountry = CountryCode.US,
            TargetLocalTypeCode = "COMMON"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CorporateActionMergerResultDTO>();
        result!.Target!.Name.Should().Be("XCORP");
        result.Target.ISIN.Should().Be("US00000X1234");
        result.Target.Exchange.Should().Be("NASDAQ");
        result.Target.Ticker.Should().Be("XCORP");
        result.Target.Country.Should().Be(CountryCode.US);

        var repository = Services.GetRequiredService<IInvestmentRepository>();
        var target = repository.GetAsset("XPI", "Default", "XCORP");
        target.Should().NotBeNull();
        target!.CorporateActions.Should().ContainSingle().Which.Role.Should().Be(CorporateAction.CorporateActionRole.Target);
    }

    [Fact]
    [Trait("AC", "P53-F02-merger-04")]
    public async Task SelectingATargetAssetNameThatCollidesWithAnExistingDistinctAsset_IsRejectedWithAnInlineError()
    {
        var otherAsset = await Client.PostAsJsonAsync("/api/v1/financial/assets", new AssetAdminCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            Name = "OTHERASSET"
        });
        otherAsset.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/merger", new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "OTHERASSET",
            CreateTargetAssetInline = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, "the target name is already used by a distinct, existing asset");
    }

    [Fact]
    [Trait("AC", "P53-F02-merger-05")]
    public async Task NoDisposalRecordIsCreatedForTheConvertedPortion()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/merger", new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "XCORP",
            CreateTargetAssetInline = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CorporateActionMergerResultDTO>();
        result!.Target!.DisposalRecords.Should().BeEmpty("a merger never realises a disposal on the converted amount");
    }

    [Fact]
    [Trait("AC", "P53-F02-merger-06")]
    public async Task ATaxClassificationIsCreatedForTheMerger_WithStatusRequiresReview_WhenNoMatchingTaxRuleExists()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/merger", new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "XCORP",
            CreateTargetAssetInline = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var repository = Services.GetRequiredService<IInvestmentRepository>();
        var target = repository.GetAsset("XPI", "Default", "XCORP");
        var classification = target!.TaxClassifications.Should().ContainSingle().Subject;
        classification.CalculationStatus.Should().Be(CalculationStatus.RequiresReview);
        classification.SourceType.Should().Be(SourceType.CorporateAction);
    }

    [Fact]
    [Trait("AC", "P53-F02-merger-07")]
    public async Task AnExistingDisposalRecordOnTheSourceAssetPredatingTheMerger_IsLeftByteIdenticalAfterTheMergerIsRecorded()
    {
        var before = await Client.GetFromJsonAsync<AssetDetailsDTO>("/api/v1/financial/assets/XPI/Default/BCIA11");
        // BCIA11's seeded 2024-06-01 sell has no persisted DisposalRecord, so it is backfilled the
        // moment the host loads (InvestmentLoader.LoadSync -> DisposalRecordBackfill), independently
        // of this feature. The merger's effective date (2024-07-01) is after that sell, so recording
        // it must leave that pre-existing record completely untouched.
        var preExisting = before!.DisposalRecords.Should().ContainSingle(d => d.Status == DisposalRecordStatus.Active).Subject;

        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/merger", new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "XCORP",
            CreateTargetAssetInline = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CorporateActionMergerResultDTO>();
        result!.Source!.DisposalRecords.Should().ContainSingle(d => d.Id == preExisting.Id && d.Status == DisposalRecordStatus.Active);
    }

    [Fact]
    [Trait("AC", "P53-F02-merger-08")]
    public async Task CashInLieuAmount_WhenEntered_IsStoredOnTheMergerRecord_AndDoesNotCreateAnyDisposalRecord()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/merger", new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            ExchangeRatio = 2.0m,
            CashInLieuAmount = 3.25m,
            TargetAssetName = "XCORP",
            CreateTargetAssetInline = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CorporateActionMergerResultDTO>();
        result!.Source!.DisposalRecords.Should().HaveCount(1, "the cash-in-lieu amount is a note field, never a disposal");

        var repository = Services.GetRequiredService<IInvestmentRepository>();
        var source = repository.GetAsset("XPI", "Default", "BCIA11");
        source!.CorporateActions.Should().ContainSingle().Which.CashInLieu.Should().Be(3.25m);
    }
}
