using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class TransactionsTests
{
    [Fact]
    public void Add_NullTransaction_ThrowsArgumentNullException()
    {
        var transactions = new Transactions();

        Action act = () => transactions.Add(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddRange_AddsAllTransactionsAndRecalculates()
    {
        var transactions = new Transactions();
        var items = new[]
        {
            Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m),
            Transaction.Create(new DateTime(2024, 1, 2), Transaction.TransactionType.Buy, 10m, 7m, 0m),
        };

        transactions.AddRange(items);

        transactions.Quantity.Should().Be(20m);
        transactions.AveragePrice.Should().Be(6m);
        transactions.Should().HaveCount(2);
    }

    [Fact]
    public void Add_Buy_UpdatesAveragePriceAndQuantity()
    {
        var transactions = new Transactions();
        var first = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var second = Transaction.Create(new DateTime(2024, 1, 2), Transaction.TransactionType.Buy, 10m, 7m, 0m);

        transactions.Add(first);
        transactions.Add(second);

        transactions.Quantity.Should().Be(20m);
        transactions.AveragePrice.Should().Be(6m);
    }

    [Fact]
    public void Add_Sell_DecreasesQuantityAndKeepsAveragePrice()
    {
        var transactions = new Transactions();
        transactions.Add(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m));

        transactions.Add(Transaction.Create(new DateTime(2024, 1, 2), Transaction.TransactionType.Sell, 5m, 12m, 0m));

        transactions.Quantity.Should().Be(0m);
        transactions.AveragePrice.Should().Be(10m);
    }

    [Fact]
    public void Add_Sell_AccumulatesRealizedCapitalGainAtRunningCostBasis()
    {
        var transactions = new Transactions();
        transactions.Add(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        transactions.Add(Transaction.Create(new DateTime(2021, 5, 1), Transaction.TransactionType.Buy, 15m, 100m, 0m));

        transactions.Add(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));

        // Weighted-average cost after both buys is 100; capital gain = 550 - (5 x 100) = 50
        transactions.RealizedCapitalGain.Should().Be(50m);
    }

    [Fact]
    public void AverageSellPrice_NoSales_IsNull()
    {
        var transactions = new Transactions();
        transactions.Add(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        transactions.AverageSellPrice.Should().BeNull();
    }

    [Fact]
    public void AverageSellPrice_MultipleSales_IsWeightedAverage()
    {
        var transactions = new Transactions();
        transactions.Add(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 20m, 100m, 0m));
        transactions.Add(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));

        transactions.Add(Transaction.Create(new DateTime(2022, 6, 1), Transaction.TransactionType.Sell, 5m, 120m, 0m));

        // Weighted average = (5 x 110 + 5 x 120) / 10 = 115
        transactions.AverageSellPrice.Should().Be(115m);
    }

    [Fact]
    public void Update_RebuildsAndRecalculates()
    {
        var transactions = new Transactions();
        var tx1Id = Guid.NewGuid();
        var tx1 = Transaction.CreateWithId(tx1Id, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var tx2 = Transaction.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 2), Transaction.TransactionType.Buy, 10m, 7m, 0m);
        transactions.Add(tx1);
        transactions.Add(tx2);

        var updated = Transaction.CreateWithId(tx1Id, tx1.Date, tx1.Type, 20m, 5m, 0m);
        var result = transactions.Update(updated);

        result.Should().BeTrue();
        transactions.Quantity.Should().Be(30m);
        var expected = (20m * 5m + 10m * 7m) / 30m;
        transactions.AveragePrice.Should().Be(expected);
    }

    [Fact]
    public void Update_UnknownId_ReturnsFalse()
    {
        var transactions = new Transactions();

        var result = transactions.Update(Transaction.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 1m, 1m, 0m));

        result.Should().BeFalse();
    }

    [Fact]
    public void Update_EmptyId_Throws()
    {
        var transactions = new Transactions();
        // Activator bypasses the public factory methods so Id stays Guid.Empty.
        var transaction = (Transaction)Activator.CreateInstance(typeof(Transaction), nonPublic: true)!;

        Action act = () => transactions.Update(transaction);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveById_UnknownId_ReturnsFalse()
    {
        var transactions = new Transactions();

        transactions.RemoveById(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void RemoveById_ExistingId_RemovesAndRecalculates()
    {
        var transactions = new Transactions();
        var txId = Guid.NewGuid();
        transactions.Add(Transaction.CreateWithId(txId, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));

        var result = transactions.RemoveById(txId);

        result.Should().BeTrue();
        transactions.Should().BeEmpty();
        transactions.Quantity.Should().Be(0m);
    }
}
