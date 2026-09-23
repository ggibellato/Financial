using System;
using System.Linq;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class AssetCorporateActionTests
{
    [Fact]
    public void RecordCorporateAction_TwoForOneSplit_DoublesQuantityAndHalvesAveragePriceWithCostBasisUnchanged()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        var costBasisBefore = asset.Quantity * asset.AveragePrice;

        asset.RecordCorporateAction(CorporateAction.CreateSplit(new DateTime(2021, 6, 1), 2.0m));

        asset.Quantity.Should().Be(20m);
        asset.AveragePrice.Should().Be(50m);
        (asset.Quantity * asset.AveragePrice).Should().Be(costBasisBefore);
    }

    [Fact]
    public void RecordCorporateAction_OneForTenReverseSplit_ReducesQuantityAndMultipliesAveragePrice()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        asset.RecordCorporateAction(CorporateAction.CreateSplit(new DateTime(2021, 6, 1), 0.1m));

        asset.Quantity.Should().Be(1m);
        asset.AveragePrice.Should().Be(1000m);
    }

    [Fact]
    public void RecordCorporateAction_SameDateAsSell_AppliesBeforeTheSaleAndRealizesGainAtRescaledCost()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        var date = new DateTime(2021, 6, 1);
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        asset.RecordCorporateAction(CorporateAction.CreateSplit(date, 2.0m));
        asset.RecordTransaction(Transaction.Create(date, Transaction.TransactionType.Sell, 5m, 60m, 0m));

        asset.Quantity.Should().Be(15m, "20 post-split units less the same-date sale of 5");
        var disposal = asset.DisposalRecords.Single();
        disposal.CostBasis.Should().Be(250m, "5 units at the post-split average cost of 50");
    }

    [Fact]
    public void RecordCorporateAction_Fifo_RescalesEveryOpenLotProportionallyKeepingTotalLotCostUnchanged()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 3m, 20m, 0m));
        var totalLotCostBefore = OpenLotTracker.GetOpenLots(asset.Transactions).Sum(lot => lot.RemainingQuantity * lot.UnitCost);

        asset.RecordCorporateAction(CorporateAction.CreateSplit(new DateTime(2021, 6, 1), 2.0m));

        var lots = OpenLotTracker.GetOpenLots(asset.Transactions, asset.CorporateActions);
        lots.Should().HaveCount(2);
        lots[0].RemainingQuantity.Should().Be(10m);
        lots[1].RemainingQuantity.Should().Be(6m);
        lots.Sum(lot => lot.RemainingQuantity * lot.UnitCost).Should().Be(totalLotCostBefore);
    }

    [Fact]
    public void RecordCorporateAction_SpecificId_LaterDisposalAgainstRescaledLotTotalsCorrectly()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        var buy = Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 5m, 100m, 0m);
        asset.AddTransaction(buy);

        asset.RecordCorporateAction(CorporateAction.CreateSplit(new DateTime(2021, 6, 1), 2.0m), CostBasisMethod.SpecificId);
        asset.RecordTransaction(
            Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 4m, 60m, 0m),
            CostBasisMethod.SpecificId,
            new[] { new SpecificLotAllocation(buy.Id, 4m) });

        var disposal = asset.DisposalRecords.Single();
        disposal.LotsConsumed.Should().ContainSingle(l => l.SourceTransactionId == buy.Id);
        disposal.CostBasis.Should().Be(200m, "4 units at the post-split unit cost of 50");
    }

    [Fact]
    public void RecordCorporateAction_ZeroQuantityHolding_ThrowsAndLeavesNoStateChange()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Sell, 10m, 110m, 0m));
        asset.Quantity.Should().Be(0m);

        Action act = () => asset.RecordCorporateAction(CorporateAction.CreateSplit(new DateTime(2021, 9, 1), 2.0m));

        act.Should().Throw<InvestmentRuleViolationException>();
        asset.CorporateActions.Should().BeEmpty();
        asset.Quantity.Should().Be(0m);
    }

    [Fact]
    public void RecordCorporateAction_EffectiveDateBeforeFirstTransaction_ThrowsTheSameZeroQuantityGuard()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        Action act = () => asset.RecordCorporateAction(CorporateAction.CreateSplit(new DateTime(2021, 1, 1), 2.0m));

        act.Should().Throw<InvestmentRuleViolationException>();
        asset.CorporateActions.Should().BeEmpty();
    }

    [Fact]
    public void RecordCorporateAction_NullCorporateAction_ThrowsArgumentNullException()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");

        Action act = () => asset.RecordCorporateAction(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordCorporateAction_ReverseSplitShrinksLotBelowAnExistingSpecificIdDisposal_RollsBackAndRethrows()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        var buy = Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m);
        asset.AddTransaction(buy);
        asset.RecordTransaction(
            Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 10m, 60m, 0m),
            CostBasisMethod.SpecificId,
            new[] { new SpecificLotAllocation(buy.Id, 10m) });

        Action act = () => asset.RecordCorporateAction(CorporateAction.CreateSplit(new DateTime(2021, 6, 1), 0.1m), CostBasisMethod.SpecificId);

        act.Should().Throw<InvestmentRuleViolationException>();
        asset.CorporateActions.Should().BeEmpty("the failed record rolled back");
    }

    [Fact]
    public void RetractCorporateAction_UnknownId_ReturnsFalse()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");

        asset.RetractCorporateAction(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void RetractCorporateAction_ExistingSplit_RetriggersReplayAndUpdatesQuantityAndAveragePrice()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        var split = CorporateAction.CreateSplit(new DateTime(2021, 6, 1), 2.0m);
        asset.RecordCorporateAction(split);
        asset.Quantity.Should().Be(20m);

        var result = asset.RetractCorporateAction(split.Id);

        result.Should().BeTrue();
        asset.CorporateActions.Should().BeEmpty();
        asset.Quantity.Should().Be(10m);
        asset.AveragePrice.Should().Be(100m);
    }

    [Fact]
    public void ReviseCorporateAction_UnknownId_ReturnsFalse()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");

        var result = asset.ReviseCorporateAction(CorporateAction.CreateSplitWithId(Guid.NewGuid(), new DateTime(2021, 6, 1), 2.0m));

        result.Should().BeFalse();
    }

    [Fact]
    public void ReviseCorporateAction_ChangedRatio_RecomputesPositionUnderTheNewRatio()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        var split = CorporateAction.CreateSplit(new DateTime(2021, 6, 1), 2.0m);
        asset.RecordCorporateAction(split);
        asset.Quantity.Should().Be(20m);

        var revised = CorporateAction.CreateSplitWithId(split.Id, split.EffectiveDate, 4.0m);
        var result = asset.ReviseCorporateAction(revised);

        result.Should().BeTrue();
        asset.Quantity.Should().Be(40m);
        asset.AveragePrice.Should().Be(25m);
    }

    [Fact]
    public void RecordCorporateAction_NoLaterDisposal_CreatesNoDisposalRecord()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        asset.RecordCorporateAction(CorporateAction.CreateSplit(new DateTime(2021, 6, 1), 2.0m));

        asset.DisposalRecords.Should().BeEmpty();
    }

    [Fact]
    public void PositionAsOf_BeforeAnySameDateTransaction_ExcludesTheSameDateBuy()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        var sameDate = new DateTime(2021, 6, 1);
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.AddTransaction(Transaction.Create(sameDate, Transaction.TransactionType.Buy, 5m, 60m, 0m));

        var (quantity, averagePrice) = asset.PositionAsOf(sameDate);

        quantity.Should().Be(10m, "the same-date buy hasn't been applied yet");
        averagePrice.Should().Be(100m);
    }

    [Fact]
    public void PositionAsOf_AfterAnExistingSplit_ReflectsThePostSplitPosition()
    {
        var asset = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordCorporateAction(CorporateAction.CreateSplit(new DateTime(2021, 6, 1), 2.0m));

        var (quantity, averagePrice) = asset.PositionAsOf(new DateTime(2021, 12, 1));

        quantity.Should().Be(20m);
        averagePrice.Should().Be(50m);
    }

    [Fact]
    public void RecordCorporateAction_MergerSource_ClosesPositionAndClearsOpenLots()
    {
        var source = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        source.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        var mergerSource = CorporateAction.CreateMergerSource(
            new DateTime(2021, 6, 1), 0.5m, null, null, Guid.NewGuid(), "Asset B", 10m, 1000m);
        source.RecordCorporateAction(mergerSource);

        source.Quantity.Should().Be(0m);
        source.AveragePrice.Should().Be(0m);
        OpenLotTracker.GetOpenLots(source.Transactions, source.CorporateActions).Should().BeEmpty();
    }

    [Fact]
    public void RecordCorporateAction_MergerSourceZeroQuantityHolding_ThrowsAndLeavesNoStateChange()
    {
        var source = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");

        Action act = () => source.RecordCorporateAction(CorporateAction.CreateMergerSource(
            new DateTime(2021, 6, 1), 0.5m, null, null, Guid.NewGuid(), "Asset B", 10m, 1000m));

        act.Should().Throw<InvestmentRuleViolationException>();
        source.CorporateActions.Should().BeEmpty();
    }

    [Fact]
    public void RecordCorporateAction_MergerTargetZeroQuantityHolding_IsAllowed()
    {
        var target = Asset.Create("Asset B", "ISIN-B", "NASDAQ", "BBB");

        target.RecordCorporateAction(CorporateAction.CreateMergerTarget(
            new DateTime(2021, 6, 1), null, Guid.NewGuid(), "Asset A", 20m, 1000m));

        target.Quantity.Should().Be(20m);
        target.AveragePrice.Should().Be(50m);
    }

    [Fact]
    public void RecordCorporateAction_LinkedSourceAndTarget_CarriesQuantityAndCostBasisAcrossAssets()
    {
        var source = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        source.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        var target = Asset.Create("Asset B", "ISIN-B", "NASDAQ", "BBB");
        target.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 5m, 20m, 0m));

        var correlationId = Guid.NewGuid();
        var effectiveDate = new DateTime(2021, 6, 1);
        var (sourceQuantity, sourceAveragePrice) = source.PositionAsOf(effectiveDate);
        var convertedQuantity = sourceQuantity * 2m;
        var carriedCostBasis = sourceQuantity * sourceAveragePrice;

        source.RecordCorporateAction(CorporateAction.CreateMergerSource(
            effectiveDate, 2.0m, null, null, correlationId, "Asset B", convertedQuantity, carriedCostBasis));
        target.RecordCorporateAction(CorporateAction.CreateMergerTarget(
            effectiveDate, null, correlationId, "Asset A", convertedQuantity, carriedCostBasis));

        source.Quantity.Should().Be(0m);
        target.Quantity.Should().Be(25m, "5 existing units plus 20 received units");
        target.AveragePrice.Should().Be((5m * 20m + 1000m) / 25m);
    }

    [Fact]
    public void RecordCorporateAction_MergerTargetWithInvestments_CreatesTaxClassificationRequiringReview()
    {
        var target = Asset.Create("Asset B", "ISIN-B", "NASDAQ", "BBB");
        var investments = Investments.Create();
        var mergerTarget = CorporateAction.CreateMergerTarget(
            new DateTime(2021, 6, 1), null, Guid.NewGuid(), "Asset A", 20m, 1000m);

        target.RecordCorporateAction(mergerTarget, investments: investments, brokerCurrency: Currency.GBP);

        var classification = target.TaxClassifications.Should().ContainSingle().Subject;
        classification.SourceType.Should().Be(SourceType.CorporateAction);
        classification.SourceId.Should().Be(mergerTarget.Id);
        classification.EventCategory.Should().Be(EventCategory.CorporateAction);
        classification.CalculationStatus.Should().Be(CalculationStatus.RequiresReview);
    }

    [Fact]
    public void RecordCorporateAction_MergerSourceWithInvestments_CreatesNoTaxClassification()
    {
        var source = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        source.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        var investments = Investments.Create();

        source.RecordCorporateAction(
            CorporateAction.CreateMergerSource(new DateTime(2021, 6, 1), 0.5m, null, null, Guid.NewGuid(), "Asset B", 10m, 1000m),
            investments: investments,
            brokerCurrency: Currency.GBP);

        source.TaxClassifications.Should().BeEmpty();
    }

    [Fact]
    public void RetractCorporateAction_MergerTargetWithTaxClassification_SupersedesTheLinkedClassification()
    {
        var target = Asset.Create("Asset B", "ISIN-B", "NASDAQ", "BBB");
        var investments = Investments.Create();
        var mergerTarget = CorporateAction.CreateMergerTarget(
            new DateTime(2021, 6, 1), null, Guid.NewGuid(), "Asset A", 20m, 1000m);
        target.RecordCorporateAction(mergerTarget, investments: investments, brokerCurrency: Currency.GBP);

        target.RetractCorporateAction(mergerTarget.Id, investments: investments);

        var classification = target.TaxClassifications.Should().ContainSingle().Subject;
        classification.Status.Should().Be(TaxClassificationStatus.Superseded);
        target.Quantity.Should().Be(0m);
    }

    [Fact]
    public void ReviseCorporateAction_MergerTarget_SupersedesOldClassificationAndAppendsANewOne()
    {
        var target = Asset.Create("Asset B", "ISIN-B", "NASDAQ", "BBB");
        var investments = Investments.Create();
        var correlationId = Guid.NewGuid();
        var mergerTarget = CorporateAction.CreateMergerTarget(
            new DateTime(2021, 6, 1), null, correlationId, "Asset A", 20m, 1000m);
        target.RecordCorporateAction(mergerTarget, investments: investments, brokerCurrency: Currency.GBP);
        var originalClassificationId = target.TaxClassifications.Single().Id;

        var revised = CorporateAction.CreateMergerTargetWithId(
            mergerTarget.Id, mergerTarget.EffectiveDate, null, correlationId, "Asset A", 25m, 1200m);
        target.ReviseCorporateAction(revised, investments: investments, brokerCurrency: Currency.GBP);

        target.TaxClassifications.Should().HaveCount(2);
        var original = target.TaxClassifications.Single(c => c.Id == originalClassificationId);
        original.Status.Should().Be(TaxClassificationStatus.Superseded);
        var current = target.TaxClassifications.Single(c => c.Id != originalClassificationId);
        current.Status.Should().Be(TaxClassificationStatus.Active);
        current.CostBasis.Should().Be(1200m);
        target.Quantity.Should().Be(25m);
    }

    [Fact]
    public void RecordCorporateAction_SpinOffParent_QuantityUnchangedCostBasisReducedByAllocation()
    {
        var parent = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        parent.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        var spinOffParent = CorporateAction.CreateSpinOffParent(
            new DateTime(2021, 6, 1), 15m, null, Guid.NewGuid(), "SPINCO", 1m, 150m);
        parent.RecordCorporateAction(spinOffParent);

        parent.Quantity.Should().Be(10m, "a spin-off never changes the parent's quantity");
        parent.AveragePrice.Should().Be(85m, "15% of the prior 100 average cost is moved to the new asset");
    }

    [Fact]
    public void RecordCorporateAction_SpinOffParentZeroQuantityHolding_IsAllowed()
    {
        var parent = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");

        Action act = () => parent.RecordCorporateAction(CorporateAction.CreateSpinOffParent(
            new DateTime(2021, 6, 1), 15m, null, Guid.NewGuid(), "SPINCO", 1m, 0m));

        act.Should().NotThrow("unlike Split/Merger, a spin-off allocates a percentage of whatever cost basis exists, including zero");
    }

    [Fact]
    public void RecordCorporateAction_SpinOffParent_Fifo_ReducesEveryOpenLotUnitCostKeepingQuantityUnchanged()
    {
        var parent = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        parent.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m));
        parent.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 3m, 20m, 0m));

        parent.RecordCorporateAction(CorporateAction.CreateSpinOffParent(
            new DateTime(2021, 6, 1), 20m, null, Guid.NewGuid(), "SPINCO", 1m, 26m));

        var lots = OpenLotTracker.GetOpenLots(parent.Transactions, parent.CorporateActions);
        lots.Should().HaveCount(2);
        lots[0].RemainingQuantity.Should().Be(5m);
        lots[0].UnitCost.Should().Be(8m);
        lots[1].RemainingQuantity.Should().Be(3m);
        lots[1].UnitCost.Should().Be(16m);
    }

    [Fact]
    public void RecordCorporateAction_SpinOffNewZeroQuantityHolding_IsAllowed()
    {
        var newAsset = Asset.Create("Asset B", "ISIN-B", "NASDAQ", "BBB");

        newAsset.RecordCorporateAction(CorporateAction.CreateSpinOffNew(
            new DateTime(2021, 6, 1), null, Guid.NewGuid(), "Asset A", 5m, 150m));

        newAsset.Quantity.Should().Be(5m);
        newAsset.AveragePrice.Should().Be(30m);
    }

    [Fact]
    public void RecordCorporateAction_LinkedSpinOffParentAndNew_CarriesAllocatedCostBasisAcrossAssets()
    {
        var parent = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        parent.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        var newAsset = Asset.Create("Asset B", "ISIN-B", "NASDAQ", "BBB");

        var correlationId = Guid.NewGuid();
        var effectiveDate = new DateTime(2021, 6, 1);
        var (parentQuantity, parentAveragePrice) = parent.PositionAsOf(effectiveDate);
        var carriedCostBasis = 0.15m * parentQuantity * parentAveragePrice;

        parent.RecordCorporateAction(CorporateAction.CreateSpinOffParent(
            effectiveDate, 15m, null, correlationId, "Asset B", 5m, carriedCostBasis));
        newAsset.RecordCorporateAction(CorporateAction.CreateSpinOffNew(
            effectiveDate, null, correlationId, "Asset A", 5m, carriedCostBasis));

        parent.Quantity.Should().Be(10m);
        parent.AveragePrice.Should().Be(85m);
        newAsset.Quantity.Should().Be(5m);
        newAsset.AveragePrice.Should().Be(30m);
    }

    [Fact]
    public void RecordCorporateAction_SpinOffNewWithInvestments_CreatesTaxClassificationRequiringReview()
    {
        var newAsset = Asset.Create("Asset B", "ISIN-B", "NASDAQ", "BBB");
        var investments = Investments.Create();
        var spinOffNew = CorporateAction.CreateSpinOffNew(
            new DateTime(2021, 6, 1), null, Guid.NewGuid(), "Asset A", 5m, 150m);

        newAsset.RecordCorporateAction(spinOffNew, investments: investments, brokerCurrency: Currency.GBP);

        var classification = newAsset.TaxClassifications.Should().ContainSingle().Subject;
        classification.SourceType.Should().Be(SourceType.CorporateAction);
        classification.SourceId.Should().Be(spinOffNew.Id);
        classification.EventCategory.Should().Be(EventCategory.CorporateAction);
        classification.CalculationStatus.Should().Be(CalculationStatus.RequiresReview);
    }

    [Fact]
    public void RecordCorporateAction_SpinOffParentWithInvestments_CreatesNoTaxClassification()
    {
        var parent = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        parent.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        var investments = Investments.Create();

        parent.RecordCorporateAction(
            CorporateAction.CreateSpinOffParent(new DateTime(2021, 6, 1), 15m, null, Guid.NewGuid(), "Asset B", 5m, 150m),
            investments: investments,
            brokerCurrency: Currency.GBP);

        parent.TaxClassifications.Should().BeEmpty();
    }

    [Fact]
    public void RecordCorporateAction_SpinOffParent_NoLaterDisposal_CreatesNoDisposalRecord()
    {
        var parent = Asset.Create("Asset A", "ISIN-A", "LSE", "AAA");
        parent.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        parent.RecordCorporateAction(CorporateAction.CreateSpinOffParent(
            new DateTime(2021, 6, 1), 15m, null, Guid.NewGuid(), "Asset B", 5m, 150m));

        parent.DisposalRecords.Should().BeEmpty();
    }

    [Fact]
    public void RetractCorporateAction_SpinOffNewWithTaxClassification_SupersedesTheLinkedClassification()
    {
        var newAsset = Asset.Create("Asset B", "ISIN-B", "NASDAQ", "BBB");
        var investments = Investments.Create();
        var spinOffNew = CorporateAction.CreateSpinOffNew(
            new DateTime(2021, 6, 1), null, Guid.NewGuid(), "Asset A", 5m, 150m);
        newAsset.RecordCorporateAction(spinOffNew, investments: investments, brokerCurrency: Currency.GBP);

        newAsset.RetractCorporateAction(spinOffNew.Id, investments: investments);

        var classification = newAsset.TaxClassifications.Should().ContainSingle().Subject;
        classification.Status.Should().Be(TaxClassificationStatus.Superseded);
        newAsset.Quantity.Should().Be(0m);
    }

}
