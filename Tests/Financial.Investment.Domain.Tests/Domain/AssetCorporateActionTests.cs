using System;
using System.Linq;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using Financial.Investment.Domain.Rules;
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
}
