using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class TransactionsTests
{
    /// <summary>Every test drives the same Transactions, so it is wired once here.</summary>
    private readonly Transactions _sut;

    public TransactionsTests()
    {
        _sut = new Transactions();
    }

    [Fact]
    public void Add_NullTransaction_ThrowsArgumentNullException()
    {
        Action act = () => _sut.Add(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddRange_AddsAllTransactionsAndRecalculates()
    {
        var items = new[]
        {
            Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m),
            Transaction.Create(new DateTime(2024, 1, 2), Transaction.TransactionType.Buy, 10m, 7m, 0m),
        };

        _sut.AddRange(items);

        _sut.Quantity.Should().Be(20m);
        _sut.AveragePrice.Should().Be(6m);
        _sut.Should().HaveCount(2);
    }

    [Fact]
    public void Add_Buy_UpdatesAveragePriceAndQuantity()
    {
        var first = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var second = Transaction.Create(new DateTime(2024, 1, 2), Transaction.TransactionType.Buy, 10m, 7m, 0m);

        _sut.Add(first);
        _sut.Add(second);

        _sut.Quantity.Should().Be(20m);
        _sut.AveragePrice.Should().Be(6m);
    }

    [Fact]
    public void Add_Sell_DecreasesQuantityAndKeepsAveragePrice()
    {
        _sut.Add(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m));

        _sut.Add(Transaction.Create(new DateTime(2024, 1, 2), Transaction.TransactionType.Sell, 5m, 12m, 0m));

        _sut.Quantity.Should().Be(0m);
        _sut.AveragePrice.Should().Be(10m);
    }

    [Fact]
    public void Add_Sell_AccumulatesRealizedCapitalGainAtRunningCostBasis()
    {
        _sut.Add(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        _sut.Add(Transaction.Create(new DateTime(2021, 5, 1), Transaction.TransactionType.Buy, 15m, 100m, 0m));

        _sut.Add(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));

        // Weighted-average cost after both buys is 100; capital gain = 550 - (5 x 100) = 50
        _sut.RealizedCapitalGain.Should().Be(50m);
    }

    [Fact]
    public void AverageSellPrice_NoSales_IsNull()
    {
        _sut.Add(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));

        _sut.AverageSellPrice.Should().BeNull();
    }

    [Fact]
    public void AverageSellPrice_MultipleSales_IsWeightedAverage()
    {
        _sut.Add(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 20m, 100m, 0m));
        _sut.Add(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));

        _sut.Add(Transaction.Create(new DateTime(2022, 6, 1), Transaction.TransactionType.Sell, 5m, 120m, 0m));

        // Weighted average = (5 x 110 + 5 x 120) / 10 = 115
        _sut.AverageSellPrice.Should().Be(115m);
    }

    [Fact]
    public void Update_RebuildsAndRecalculates()
    {
        var tx1Id = Guid.NewGuid();
        var tx1 = Transaction.CreateWithId(tx1Id, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var tx2 = Transaction.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 2), Transaction.TransactionType.Buy, 10m, 7m, 0m);
        _sut.Add(tx1);
        _sut.Add(tx2);

        var updated = Transaction.CreateWithId(tx1Id, tx1.Date, tx1.Type, 20m, 5m, 0m);
        var result = _sut.Update(updated);

        result.Should().BeTrue();
        _sut.Quantity.Should().Be(30m);
        var expected = (20m * 5m + 10m * 7m) / 30m;
        _sut.AveragePrice.Should().Be(expected);
    }

    [Fact]
    public void Update_UnknownId_ReturnsFalse()
    {
        var result = _sut.Update(Transaction.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 1m, 1m, 0m));

        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveById_UnknownId_ReturnsFalse()
    {
        _sut.RemoveById(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void RemoveById_ExistingId_RemovesAndRecalculates()
    {
        var txId = Guid.NewGuid();
        _sut.Add(Transaction.CreateWithId(txId, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));

        var result = _sut.RemoveById(txId);

        result.Should().BeTrue();
        _sut.Should().BeEmpty();
        _sut.Quantity.Should().Be(0m);
    }
}
