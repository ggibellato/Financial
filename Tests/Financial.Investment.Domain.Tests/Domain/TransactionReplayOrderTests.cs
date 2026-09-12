using System;
using System.Linq;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class TransactionReplayOrderTests
{
    [Fact]
    public void Sort_MixedDates_OrdersByDateAscending()
    {
        var later = Transaction.Create(new DateTime(2024, 3, 1), Transaction.TransactionType.Buy, 1m, 10m, 0m);
        var earlier = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 1m, 10m, 0m);
        var middle = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 1m, 10m, 0m);

        var sorted = TransactionReplayOrder.Sort([later, earlier, middle]).ToList();

        sorted.Should().Equal([earlier, middle, later]);
    }

    [Fact]
    public void Sort_SameDateBuyAndSell_PutsBuyFirst()
    {
        var date = new DateTime(2024, 1, 1);
        var sell = Transaction.Create(date, Transaction.TransactionType.Sell, 1m, 10m, 0m);
        var buy = Transaction.Create(date, Transaction.TransactionType.Buy, 1m, 10m, 0m);

        var sorted = TransactionReplayOrder.Sort([sell, buy]).ToList();

        sorted.Should().Equal([buy, sell]);
    }

    [Fact]
    public void Sort_SameDateSameType_KeepsSourceOrder()
    {
        var date = new DateTime(2024, 1, 1);
        var first = Transaction.Create(date, Transaction.TransactionType.Buy, 1m, 10m, 0m);
        var second = Transaction.Create(date, Transaction.TransactionType.Buy, 2m, 20m, 0m);

        var sorted = TransactionReplayOrder.Sort([first, second]).ToList();

        sorted.Should().Equal([first, second]);
    }

    [Fact]
    public void Sort_ThreeSameDateBuysAndOneSell_KeepsBuyOrderAndMovesSellLast()
    {
        var date = new DateTime(2024, 1, 1);
        var buy1 = Transaction.Create(date, Transaction.TransactionType.Buy, 1m, 10m, 0m);
        var sell = Transaction.Create(date, Transaction.TransactionType.Sell, 1m, 10m, 0m);
        var buy2 = Transaction.Create(date, Transaction.TransactionType.Buy, 2m, 20m, 0m);

        var sorted = TransactionReplayOrder.Sort([buy1, sell, buy2]).ToList();

        sorted.Should().Equal([buy1, buy2, sell]);
    }

    [Fact]
    public void IsInOrder_EarlierDate_ReturnsTrue()
    {
        var earlier = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 1m, 10m, 0m);
        var later = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 1m, 10m, 0m);

        TransactionReplayOrder.IsInOrder(earlier, later).Should().BeTrue();
    }

    [Fact]
    public void IsInOrder_LaterDate_ReturnsFalse()
    {
        var earlier = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 1m, 10m, 0m);
        var later = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 1m, 10m, 0m);

        TransactionReplayOrder.IsInOrder(later, earlier).Should().BeFalse();
    }

    [Fact]
    public void IsInOrder_SameDateSellThenBuy_ReturnsFalse()
    {
        var date = new DateTime(2024, 1, 1);
        var sell = Transaction.Create(date, Transaction.TransactionType.Sell, 1m, 10m, 0m);
        var buy = Transaction.Create(date, Transaction.TransactionType.Buy, 1m, 10m, 0m);

        TransactionReplayOrder.IsInOrder(sell, buy).Should().BeFalse();
    }

    [Fact]
    public void IsInOrder_SameDateBuyThenSell_ReturnsTrue()
    {
        var date = new DateTime(2024, 1, 1);
        var buy = Transaction.Create(date, Transaction.TransactionType.Buy, 1m, 10m, 0m);
        var sell = Transaction.Create(date, Transaction.TransactionType.Sell, 1m, 10m, 0m);

        TransactionReplayOrder.IsInOrder(buy, sell).Should().BeTrue();
    }

    [Fact]
    public void IsInOrder_SameDateSameType_ReturnsTrue()
    {
        var date = new DateTime(2024, 1, 1);
        var first = Transaction.Create(date, Transaction.TransactionType.Buy, 1m, 10m, 0m);
        var second = Transaction.Create(date, Transaction.TransactionType.Buy, 2m, 20m, 0m);

        TransactionReplayOrder.IsInOrder(first, second).Should().BeTrue();
    }

    [Theory]
    [InlineData(Transaction.TransactionType.Redemption)]
    [InlineData(Transaction.TransactionType.TransferOut)]
    public void Sort_SameDateIncreaseAndDecreaseType_PutsIncreaseFirst(Transaction.TransactionType decreaseType)
    {
        var date = new DateTime(2024, 1, 1);
        var decrease = Transaction.Create(date, decreaseType, 1m, 10m, 0m);
        var increase = Transaction.Create(date, Transaction.TransactionType.TransferIn, 1m, 10m, 0m);

        var sorted = TransactionReplayOrder.Sort([decrease, increase]).ToList();

        sorted.Should().Equal([increase, decrease]);
    }

    [Theory]
    [InlineData(Transaction.TransactionType.Fee)]
    [InlineData(Transaction.TransactionType.CapitalCall)]
    [InlineData(Transaction.TransactionType.ReturnOfCapital)]
    public void Sort_SameDateNoQuantityEffectType_SortsBeforeADecreaseType(Transaction.TransactionType noEffectType)
    {
        var date = new DateTime(2024, 1, 1);
        var sell = Transaction.Create(date, Transaction.TransactionType.Sell, 1m, 10m, 0m);
        var noEffect = Transaction.Create(date, noEffectType, 0m, 0m, fees: 1m);

        var sorted = TransactionReplayOrder.Sort([sell, noEffect]).ToList();

        sorted.Should().Equal([noEffect, sell]);
    }
}
