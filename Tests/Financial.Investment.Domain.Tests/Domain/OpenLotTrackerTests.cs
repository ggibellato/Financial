using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class OpenLotTrackerTests
{
    [Fact]
    public void GetOpenLots_NoTransactions_ReturnsEmpty()
    {
        var lots = OpenLotTracker.GetOpenLots(Array.Empty<Transaction>());

        lots.Should().BeEmpty();
    }

    [Fact]
    public void GetOpenLots_OrdersOldestFirst_WithSameDatePurchaseBeforeSaleTieBreak()
    {
        var earlierBuy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var sameDateBuyOne = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 4m, 6m, 0m);
        var sameDateBuyTwo = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 6m, 7m, 0m);

        var lots = OpenLotTracker.GetOpenLots(new[] { earlierBuy, sameDateBuyOne, sameDateBuyTwo });

        lots.Should().HaveCount(3);
        lots[0].SourceTransactionId.Should().Be(earlierBuy.Id);
        lots[1].SourceTransactionId.Should().Be(sameDateBuyOne.Id);
        lots[2].SourceTransactionId.Should().Be(sameDateBuyTwo.Id);
    }

    [Fact]
    public void GetOpenLots_DisposalLargerThanOldestLot_SplitsAcrossNextLots()
    {
        var firstBuy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 5m, 5m, 0m);
        var secondBuy = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 5m, 6m, 0m);
        var sell = Transaction.Create(new DateTime(2024, 3, 1), Transaction.TransactionType.Sell, 8m, 7m, 0m);

        var lots = OpenLotTracker.GetOpenLots(new[] { firstBuy, secondBuy, sell });

        lots.Should().ContainSingle();
        lots[0].SourceTransactionId.Should().Be(secondBuy.Id);
        lots[0].RemainingQuantity.Should().Be(2m);
    }

    [Fact]
    public void GetOpenLots_TransferOut_ReducesQuantityWithNoAssociatedGainLoss()
    {
        var buy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var transferOut = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.TransferOut, 4m, 5m, 0m);

        var lots = OpenLotTracker.GetOpenLots(new[] { buy, transferOut });

        lots.Should().ContainSingle();
        lots[0].RemainingQuantity.Should().Be(6m);
        // OpenLot itself carries no gain/loss field at all - the type proves TransferOut cannot
        // produce one here.
        typeof(OpenLot).GetProperty("GainLoss").Should().BeNull();
    }

    [Fact]
    public void GetOpenLots_FullyConsumedLot_IsExcludedFromResult()
    {
        var buy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var sell = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 10m, 6m, 0m);

        var lots = OpenLotTracker.GetOpenLots(new[] { buy, sell });

        lots.Should().BeEmpty();
    }

    [Fact]
    public void GetOpenLots_UnitCostIncludesFees_MatchingAveragePriceConvention()
    {
        var buy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 10m);

        var lots = OpenLotTracker.GetOpenLots(new[] { buy });

        lots.Should().ContainSingle();
        lots[0].UnitCost.Should().Be(6m);
    }

    [Fact]
    public void GetOpenLots_TransferInAlsoCreatesALot()
    {
        var transferIn = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.TransferIn, 10m, 5m, 0m);

        var lots = OpenLotTracker.GetOpenLots(new[] { transferIn });

        lots.Should().ContainSingle();
        lots[0].RemainingQuantity.Should().Be(10m);
    }

    [Fact]
    public void GetOpenLots_SplitAfterTwoOpenLots_RescalesBothProportionallyKeepingTotalCostUnchanged()
    {
        var firstBuy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m);
        var secondBuy = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 3m, 20m, 0m);
        var split = CorporateAction.CreateSplit(new DateTime(2024, 3, 1), 2.0m);

        var lots = OpenLotTracker.GetOpenLots(new[] { firstBuy, secondBuy }, new[] { split });

        lots.Should().HaveCount(2);
        lots[0].RemainingQuantity.Should().Be(10m);
        lots[0].UnitCost.Should().Be(5m);
        lots[1].RemainingQuantity.Should().Be(6m);
        lots[1].UnitCost.Should().Be(10m);
        lots.Sum(lot => lot.RemainingQuantity * lot.UnitCost).Should().Be(5m * 10m + 3m * 20m);
    }

    [Fact]
    public void GetOpenLots_ReverseSplitAfterOpenLot_RescalesDownProportionally()
    {
        var buy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 10m, 0m);
        var reverseSplit = CorporateAction.CreateSplit(new DateTime(2024, 2, 1), 0.1m);

        var lots = OpenLotTracker.GetOpenLots(new[] { buy }, new[] { reverseSplit });

        lots.Should().ContainSingle();
        lots[0].RemainingQuantity.Should().Be(1m);
        lots[0].UnitCost.Should().Be(100m);
    }

    [Fact]
    public void GetOpenLots_SplitSameDateAsSale_AppliesBeforeTheSaleDepletesRescaledLots()
    {
        var date = new DateTime(2024, 2, 1);
        var buy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m);
        var split = CorporateAction.CreateSplit(date, 2.0m);
        var sell = Transaction.Create(date, Transaction.TransactionType.Sell, 8m, 6m, 0m);

        var lots = OpenLotTracker.GetOpenLots(new[] { buy, sell }, new[] { split });

        lots.Should().ContainSingle();
        lots[0].RemainingQuantity.Should().Be(2m, "10 post-split units less the same-date sale of 8");
    }

    [Fact]
    public void GetOpenLots_MergerSource_ClearsAllOpenLots()
    {
        var firstBuy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m);
        var secondBuy = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 3m, 20m, 0m);
        var mergerSource = CorporateAction.CreateMergerSource(
            new DateTime(2024, 3, 1), 0.5m, null, null, Guid.NewGuid(), "XCORP", 8m, 260m);

        var lots = OpenLotTracker.GetOpenLots(new[] { firstBuy, secondBuy }, new[] { mergerSource });

        lots.Should().BeEmpty();
    }

    [Fact]
    public void GetOpenLots_MergerTarget_AppendsOneNewLotAtTheCarriedUnitCost()
    {
        var mergerTarget = CorporateAction.CreateMergerTarget(
            new DateTime(2024, 3, 1), null, Guid.NewGuid(), "TWTR", 40m, 1000m);

        var lots = OpenLotTracker.GetOpenLots(Array.Empty<Transaction>(), new[] { mergerTarget });

        lots.Should().ContainSingle();
        lots[0].SourceTransactionId.Should().Be(mergerTarget.Id);
        lots[0].RemainingQuantity.Should().Be(40m);
        lots[0].UnitCost.Should().Be(25m);
    }

    [Fact]
    public void GetOpenLots_MergerTargetOnTopOfExistingLots_KeepsPriorLotsAndAppendsTheNewOne()
    {
        var buy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var mergerTarget = CorporateAction.CreateMergerTarget(
            new DateTime(2024, 2, 1), null, Guid.NewGuid(), "TWTR", 40m, 1000m);

        var lots = OpenLotTracker.GetOpenLots(new[] { buy }, new[] { mergerTarget });

        lots.Should().HaveCount(2);
        lots[0].SourceTransactionId.Should().Be(buy.Id);
        lots[1].SourceTransactionId.Should().Be(mergerTarget.Id);
        lots[1].RemainingQuantity.Should().Be(40m);
        lots[1].UnitCost.Should().Be(25m);
    }

    [Fact]
    public void GetOpenLots_NoCorporateActions_SameAsSingleArgumentOverload()
    {
        var buy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);

        var lots = OpenLotTracker.GetOpenLots(new[] { buy }, Array.Empty<CorporateAction>());

        lots.Should().BeEquivalentTo(OpenLotTracker.GetOpenLots(new[] { buy }));
    }

    [Fact]
    public void GetOpenLots_SpinOffParent_ReducesUnitCostOfEveryOpenLotKeepingQuantityUnchanged()
    {
        var firstBuy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m);
        var secondBuy = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 3m, 20m, 0m);
        var spinOffParent = CorporateAction.CreateSpinOffParent(
            new DateTime(2024, 3, 1), 20m, null, Guid.NewGuid(), "SPINCO", 1m, 26m);

        var lots = OpenLotTracker.GetOpenLots(new[] { firstBuy, secondBuy }, new[] { spinOffParent });

        lots.Should().HaveCount(2);
        lots[0].RemainingQuantity.Should().Be(5m);
        lots[0].UnitCost.Should().Be(8m);
        lots[1].RemainingQuantity.Should().Be(3m);
        lots[1].UnitCost.Should().Be(16m);
    }

    [Fact]
    public void GetOpenLots_SpinOffParentZeroAllocation_LeavesUnitCostUnchanged()
    {
        var buy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m);
        var spinOffParent = CorporateAction.CreateSpinOffParent(
            new DateTime(2024, 2, 1), 0m, null, Guid.NewGuid(), "SPINCO", 1m, 0m);

        var lots = OpenLotTracker.GetOpenLots(new[] { buy }, new[] { spinOffParent });

        lots.Should().ContainSingle();
        lots[0].RemainingQuantity.Should().Be(5m);
        lots[0].UnitCost.Should().Be(10m);
    }

    [Fact]
    public void GetOpenLots_SpinOffNew_AppendsOneNewLotAtTheCarriedUnitCost()
    {
        var spinOffNew = CorporateAction.CreateSpinOffNew(
            new DateTime(2024, 3, 1), null, Guid.NewGuid(), "GEHC", 5m, 150m);

        var lots = OpenLotTracker.GetOpenLots(Array.Empty<Transaction>(), new[] { spinOffNew });

        lots.Should().ContainSingle();
        lots[0].SourceTransactionId.Should().Be(spinOffNew.Id);
        lots[0].RemainingQuantity.Should().Be(5m);
        lots[0].UnitCost.Should().Be(30m);
    }

    [Fact]
    public void GetOpenLots_SpinOffNewOnTopOfExistingLots_KeepsPriorLotsAndAppendsTheNewOne()
    {
        var buy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var spinOffNew = CorporateAction.CreateSpinOffNew(
            new DateTime(2024, 2, 1), null, Guid.NewGuid(), "GEHC", 5m, 150m);

        var lots = OpenLotTracker.GetOpenLots(new[] { buy }, new[] { spinOffNew });

        lots.Should().HaveCount(2);
        lots[0].SourceTransactionId.Should().Be(buy.Id);
        lots[1].SourceTransactionId.Should().Be(spinOffNew.Id);
        lots[1].RemainingQuantity.Should().Be(5m);
        lots[1].UnitCost.Should().Be(30m);
    }
}
