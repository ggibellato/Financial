using System;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

[Trait("Category", "Unit")]
public class AverageCostReplayTests
{
    [Fact]
    public void Apply_FirstBuy_AveragePriceEqualsUnitCostPlusFeesPerUnit()
    {
        var buy = Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 10m);

        var averagePrice = AverageCostReplay.Apply(0m, 0m, buy);

        averagePrice.Should().Be((5m * 10m + 10m) / 10m);
    }

    [Fact]
    public void Apply_SecondBuy_BlendsWithExistingQuantityAndAveragePrice()
    {
        var secondBuy = Transaction.Create(new DateTime(2026, 1, 2), Transaction.TransactionType.Buy, 10m, 7m, 0m);

        var averagePrice = AverageCostReplay.Apply(10m, 5m, secondBuy);

        averagePrice.Should().Be(6m);
    }

    [Fact]
    public void Apply_ResultingQuantityIsZero_ReturnsZeroRatherThanDividingByZero()
    {
        var transferIn = Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.TransferIn, 5m, 10m, 0m);

        var averagePrice = AverageCostReplay.Apply(-5m, 3m, transferIn);

        averagePrice.Should().Be(0m);
    }
}
