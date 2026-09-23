using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests.Acceptance;

public class CorporateActionHistoryAcceptanceTests : ApiEndpointTests
{
    [Fact]
    [Trait("AC", "P53-F04-history-01")]
    public async Task HistoryEndpoint_ReturnsEverySplitMergerAndSpinOffRecordedAgainstTheAsset_OrderedByEffectiveDate()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/assets", new AssetAdminCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            Name = "CAHIST"
        });
        await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "CAHIST",
            Date = new DateTime(2024, 1, 1),
            Type = "Buy",
            Quantity = 100m,
            UnitPrice = 10m,
            Fees = 0m
        });

        var split = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/split", new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "CAHIST",
            EffectiveDate = new DateTime(2024, 3, 1),
            RatioFactor = 2.0m
        });
        split.StatusCode.Should().Be(HttpStatusCode.OK);

        var spinOff = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/spin-off", new CorporateActionSpinOffCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            ParentAssetName = "CAHIST",
            EffectiveDate = new DateTime(2024, 4, 1),
            QuantityReceived = 10m,
            AllocationPercentage = 10m,
            NewAssetName = "CAHISTSPIN",
            CreateNewAssetInline = true
        });
        spinOff.StatusCode.Should().Be(HttpStatusCode.OK);

        var merger = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/merger", new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "CAHIST",
            EffectiveDate = new DateTime(2024, 5, 1),
            ExchangeRatio = 1.0m,
            TargetAssetName = "CAHISTTARGET",
            CreateTargetAssetInline = true
        });
        merger.StatusCode.Should().Be(HttpStatusCode.OK);

        var asset = await Client.GetFromJsonAsync<AssetDetailsDTO>("/api/v1/financial/assets/XPI/Default/CAHIST");

        using var _ = new AssertionScope();
        asset!.CorporateActions.Should().HaveCount(3, "one split, one spin-off and one merger were recorded against this asset");
        asset.CorporateActions.Select(ca => ca.EffectiveDate).Should().BeInAscendingOrder();

        var splitEntry = asset.CorporateActions[0];
        splitEntry.Type.Should().Be(CorporateAction.CorporateActionType.Split);
        splitEntry.EffectiveDate.Should().Be(new DateTime(2024, 3, 1));
        splitEntry.RatioFactor.Should().Be(2.0m);
        splitEntry.Role.Should().BeNull();

        var spinOffEntry = asset.CorporateActions[1];
        spinOffEntry.Type.Should().Be(CorporateAction.CorporateActionType.SpinOff);
        spinOffEntry.Role.Should().Be(CorporateAction.CorporateActionRole.Parent);
        spinOffEntry.EffectiveDate.Should().Be(new DateTime(2024, 4, 1));
        spinOffEntry.AllocationPercentage.Should().Be(10m);
        spinOffEntry.ConvertedQuantity.Should().Be(10m);
        spinOffEntry.LinkedAssetName.Should().Be("CAHISTSPIN");

        var mergerEntry = asset.CorporateActions[2];
        mergerEntry.Type.Should().Be(CorporateAction.CorporateActionType.Merger);
        mergerEntry.Role.Should().Be(CorporateAction.CorporateActionRole.Source);
        mergerEntry.EffectiveDate.Should().Be(new DateTime(2024, 5, 1));
        mergerEntry.ExchangeRatio.Should().Be(1.0m);
        mergerEntry.LinkedAssetName.Should().Be("CAHISTTARGET");
    }

    [Fact]
    [Trait("AC", "P53-F04-history-02")]
    public async Task MergerWithNoMatchingTaxRule_AppearsInDataQualityWarningsList_NamingTheAffectedHolding()
    {
        var merger = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/merger", new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "MWARNTARGET",
            CreateTargetAssetInline = true
        });
        merger.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await merger.Content.ReadFromJsonAsync<CorporateActionMergerResultDTO>();
        var targetActionId = result!.Target!.CorporateActions.Should().ContainSingle().Subject.Id;

        var report = await Client.GetFromJsonAsync<DataQualityReportDTO>("/api/v1/financial/data-quality-report");

        var finding = report!.CorporateActionsAwaitingTaxReview.Should()
            .ContainSingle(f => f.CorporateActionId == targetActionId).Subject;
        using var _ = new AssertionScope();
        finding.BrokerName.Should().Be("XPI");
        finding.PortfolioName.Should().Be("Default");
        finding.AssetName.Should().Be("MWARNTARGET");
        finding.Type.Should().Be(CorporateAction.CorporateActionType.Merger);
        finding.EffectiveDate.Should().Be(new DateTime(2024, 7, 1));
    }

    [Fact]
    [Trait("AC", "P53-F04-history-02")]
    public async Task SpinOffWithNoMatchingTaxRule_AppearsInDataQualityWarningsList()
    {
        var spinOff = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/spin-off", new CorporateActionSpinOffCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            ParentAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            QuantityReceived = 5m,
            AllocationPercentage = 15m,
            NewAssetName = "SWARNNEW",
            CreateNewAssetInline = true
        });
        spinOff.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await spinOff.Content.ReadFromJsonAsync<CorporateActionSpinOffResultDTO>();
        var newAssetActionId = result!.New!.CorporateActions.Should().ContainSingle().Subject.Id;

        var report = await Client.GetFromJsonAsync<DataQualityReportDTO>("/api/v1/financial/data-quality-report");

        report!.CorporateActionsAwaitingTaxReview.Should().ContainSingle(f =>
            f.CorporateActionId == newAssetActionId && f.AssetName == "SWARNNEW" && f.Type == CorporateAction.CorporateActionType.SpinOff);
    }

    [Fact]
    [Trait("AC", "P53-F04-history-04")]
    public async Task WarningDisappearsOnlyAfterTheCorporateActionIsRevisedFollowingAMatchingTaxRuleBeingAdded()
    {
        var merger = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/merger", new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "REVWARNTARGET",
            CreateTargetAssetInline = true
        });
        merger.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await merger.Content.ReadFromJsonAsync<CorporateActionMergerResultDTO>();
        var sourceAction = result!.Source!.CorporateActions.Should().ContainSingle().Subject;
        var targetActionId = result.Target!.CorporateActions.Should().ContainSingle().Subject.Id;

        var beforeRule = await Client.GetFromJsonAsync<DataQualityReportDTO>("/api/v1/financial/data-quality-report");
        beforeRule!.CorporateActionsAwaitingTaxReview.Should().Contain(f => f.CorporateActionId == targetActionId);

        var ruleCreated = await Client.PostAsJsonAsync("/api/v1/financial/tax-rules", new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.BR,
            EventCategory = EventCategory.CorporateAction,
            Label = "BR corporate action rule",
            EffectiveFrom = new DateOnly(2024, 1, 1),
            EffectiveTo = null
        });
        ruleCreated.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterRuleAddedOnly = await Client.GetFromJsonAsync<DataQualityReportDTO>("/api/v1/financial/data-quality-report");
        afterRuleAddedOnly!.CorporateActionsAwaitingTaxReview.Should().Contain(f => f.CorporateActionId == targetActionId,
            "adding a TaxRule alone never retroactively recomputes an already-created TaxClassification's status");

        var revised = await Client.PutAsJsonAsync("/api/v1/financial/corporate-actions/merger", new CorporateActionMergerUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "BCIA11",
            Id = sourceAction.Id,
            EffectiveDate = new DateTime(2024, 7, 1),
            ExchangeRatio = 2.0m
        });
        revised.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterRevision = await Client.GetFromJsonAsync<DataQualityReportDTO>("/api/v1/financial/data-quality-report");
        var revisedTarget = await Client.GetFromJsonAsync<AssetDetailsDTO>("/api/v1/financial/assets/XPI/Default/REVWARNTARGET");

        using var _ = new AssertionScope();
        afterRevision!.CorporateActionsAwaitingTaxReview.Should().NotContain(f => f.AssetName == "REVWARNTARGET",
            "revising the corporate action re-runs the calculator, which now finds the matching TaxRule");
        revisedTarget!.CorporateActions.Should().ContainSingle().Which.CalculationStatus.Should().Be(CalculationStatus.Final);
    }

    [Fact]
    [Trait("AC", "P53-F04-history-05")]
    public async Task DeletingACorporateAction_RetriggersReplay_AndOnlySupersedesTheLinkedTaxClassification_NeverRewritingItDirectly()
    {
        var before = await Client.GetFromJsonAsync<AssetDetailsDTO>("/api/v1/financial/assets/XPI/Default/BCIA11");
        var preExistingDisposal = before!.DisposalRecords.Should().ContainSingle(d => d.Status == DisposalRecordStatus.Active).Subject;

        var merger = await Client.PostAsJsonAsync("/api/v1/financial/corporate-actions/merger", new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "BCIA11",
            EffectiveDate = new DateTime(2024, 7, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "DELTARGET",
            CreateTargetAssetInline = true
        });
        merger.StatusCode.Should().Be(HttpStatusCode.OK);
        var mergerResult = await merger.Content.ReadFromJsonAsync<CorporateActionMergerResultDTO>();
        var sourceAction = mergerResult!.Source!.CorporateActions.Should().ContainSingle().Subject;

        var repository = Services.GetRequiredService<IInvestmentRepository>();
        repository.GetAsset("XPI", "Default", "DELTARGET")!.TaxClassifications
            .Should().ContainSingle(c => c.Status == TaxClassificationStatus.Active);

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/financial/corporate-actions")
        {
            Content = JsonContent.Create(new CorporateActionDeleteDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Id = sourceAction.Id
            })
        };
        var deleted = await Client.SendAsync(deleteRequest);
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterDelete = await deleted.Content.ReadFromJsonAsync<AssetDetailsDTO>();

        using var _ = new AssertionScope();
        afterDelete!.CorporateActions.Should().BeEmpty("deleting the corporate action re-triggers replay and removes it from the history list");
        afterDelete.DisposalRecords.Should().ContainSingle(d => d.Id == preExistingDisposal.Id && d.Status == DisposalRecordStatus.Active,
            "a pre-existing DisposalRecord is never directly rewritten by deleting an unrelated corporate action");

        var targetClassification = repository.GetAsset("XPI", "Default", "DELTARGET")!.TaxClassifications.Should().ContainSingle().Subject;
        targetClassification.Status.Should().Be(TaxClassificationStatus.Superseded,
            "the linked TaxClassification is only ever superseded by the existing policy, never directly rewritten or removed");
    }
}
