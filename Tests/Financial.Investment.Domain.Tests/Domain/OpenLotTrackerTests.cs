using System;
using System.Collections.Generic;
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
}
