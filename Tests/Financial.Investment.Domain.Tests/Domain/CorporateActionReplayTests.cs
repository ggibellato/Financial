using System;
using System.Linq;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class CorporateActionReplayTests
{
    [Fact]
    public void Merge_NoCorporateActions_OrdersTransactionsChronologically()
    {
        var earlierBuy = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var laterBuy = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 5m, 6m, 0m);

        var steps = CorporateActionReplay.Merge(new[] { laterBuy, earlierBuy }, Array.Empty<CorporateAction>()).ToList();

        steps.Should().HaveCount(2);
        steps[0].Should().BeOfType<TransactionReplayStep>().Which.Transaction.Should().Be(earlierBuy);
        steps[1].Should().BeOfType<TransactionReplayStep>().Which.Transaction.Should().Be(laterBuy);
    }

    [Fact]
    public void Merge_CorporateActionSameDateAsBuy_CorporateActionSortsFirst()
    {
        var date = new DateTime(2024, 1, 1);
        var buy = Transaction.Create(date, Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var split = CorporateAction.CreateSplit(date, 2.0m);

        var steps = CorporateActionReplay.Merge(new[] { buy }, new[] { split }).ToList();

        steps.Should().HaveCount(2);
        steps[0].Should().BeOfType<CorporateActionReplayStep>().Which.CorporateAction.Should().Be(split);
        steps[1].Should().BeOfType<TransactionReplayStep>().Which.Transaction.Should().Be(buy);
    }

    [Fact]
    public void Merge_CorporateActionSameDateAsSell_CorporateActionSortsFirst()
    {
        var date = new DateTime(2024, 1, 1);
        var sell = Transaction.Create(date, Transaction.TransactionType.Sell, 10m, 5m, 0m);
        var split = CorporateAction.CreateSplit(date, 2.0m);

        var steps = CorporateActionReplay.Merge(new[] { sell }, new[] { split }).ToList();

        steps.Should().HaveCount(2);
        steps[0].Should().BeOfType<CorporateActionReplayStep>();
        steps[1].Should().BeOfType<TransactionReplayStep>();
    }

    [Fact]
    public void Merge_SameDateBuyAndSell_KeepsPurchaseBeforeSaleTieBreak()
    {
        var date = new DateTime(2024, 1, 1);
        var sell = Transaction.Create(date, Transaction.TransactionType.Sell, 10m, 5m, 0m);
        var buy = Transaction.Create(date, Transaction.TransactionType.Buy, 10m, 5m, 0m);

        var steps = CorporateActionReplay.Merge(new[] { sell, buy }, Array.Empty<CorporateAction>()).ToList();

        steps[0].Should().BeOfType<TransactionReplayStep>().Which.Transaction.Should().Be(buy);
        steps[1].Should().BeOfType<TransactionReplayStep>().Which.Transaction.Should().Be(sell);
    }

    [Fact]
    public void Merge_CorporateActionBeforeAllTransactions_SortsFirstOverall()
    {
        var buy = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var split = CorporateAction.CreateSplit(new DateTime(2024, 1, 1), 2.0m);

        var steps = CorporateActionReplay.Merge(new[] { buy }, new[] { split }).ToList();

        steps[0].Should().BeOfType<CorporateActionReplayStep>();
        steps[1].Should().BeOfType<TransactionReplayStep>();
    }

    [Fact]
    public void RescalePosition_TwoForOneSplit_DoublesQuantityAndHalvesAveragePrice()
    {
        var (quantity, averagePrice) = CorporateActionReplay.RescalePosition(10m, 100m, 2.0m);

        quantity.Should().Be(20m);
        averagePrice.Should().Be(50m);
    }

    [Fact]
    public void RescalePosition_OneForTenReverseSplit_ReducesQuantityAndMultipliesAveragePrice()
    {
        var (quantity, averagePrice) = CorporateActionReplay.RescalePosition(10m, 100m, 0.1m);

        quantity.Should().Be(1m);
        averagePrice.Should().Be(1000m);
    }

    [Fact]
    public void RescalePosition_TotalCostBasisIsUnchanged()
    {
        var (quantity, averagePrice) = CorporateActionReplay.RescalePosition(10m, 100m, 2.0m);

        (quantity * averagePrice).Should().Be(10m * 100m);
    }

    [Fact]
    public void RescaleLots_ScalesEveryLotProportionallyAndKeepsTotalCostUnchanged()
    {
        var lots = new[]
        {
            new OpenLot(Guid.NewGuid(), new DateTime(2024, 1, 1), 5m, 10m),
            new OpenLot(Guid.NewGuid(), new DateTime(2024, 2, 1), 3m, 20m),
        };
        var totalCostBefore = lots.Sum(lot => lot.RemainingQuantity * lot.UnitCost);

        var rescaled = CorporateActionReplay.RescaleLots(lots, 2.0m);

        rescaled.Should().HaveCount(2);
        rescaled[0].RemainingQuantity.Should().Be(10m);
        rescaled[0].UnitCost.Should().Be(5m);
        rescaled[1].RemainingQuantity.Should().Be(6m);
        rescaled[1].UnitCost.Should().Be(10m);
        rescaled.Sum(lot => lot.RemainingQuantity * lot.UnitCost).Should().Be(totalCostBefore);
    }

    [Fact]
    public void RescaleLots_PreservesSourceTransactionIdAndDate()
    {
        var sourceId = Guid.NewGuid();
        var date = new DateTime(2024, 1, 1);
        var lots = new[] { new OpenLot(sourceId, date, 5m, 10m) };

        var rescaled = CorporateActionReplay.RescaleLots(lots, 2.0m);

        rescaled[0].SourceTransactionId.Should().Be(sourceId);
        rescaled[0].Date.Should().Be(date);
    }
}
