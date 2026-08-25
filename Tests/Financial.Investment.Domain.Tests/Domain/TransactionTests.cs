using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class TransactionTests
{
    [Fact]
    public void Create_AssignsIdAndTotalPrice()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 2m, 10m, 1m);

        transaction.Id.Should().NotBe(Guid.Empty);
        transaction.TotalPrice.Should().Be(21m);
    }

    [Fact]
    public void CreateWithId_UsesProvidedId()
    {
        var id = Guid.NewGuid();

        var transaction = Transaction.CreateWithId(id, new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, 1m, 5m, 0m);

        transaction.Id.Should().Be(id);
    }

    [Fact]
    public void CreateWithId_WhenEmpty_AssignsNewId()
    {
        var transaction = Transaction.CreateWithId(Guid.Empty, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 1m, 5m, 0m);

        transaction.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void TotalPrice_ForPurchase_AddsFeesToGrossAmount()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 100m, 15.33m, 6.15m);

        transaction.TotalPrice.Should().Be(1539.15m, "a purchase costs the gross amount plus fees");
    }

    /// <summary>
    /// The fee on a sale is deducted from what is received, not added to it. Adding it overstated
    /// proceeds by twice the fee and propagated into Realized Gain/Loss, Average Sell Price and
    /// the XIRR cash-flow series.
    /// </summary>
    [Fact]
    public void TotalPrice_ForSale_DeductsFeesFromGrossAmount()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, 100m, 15.33m, 6.15m);

        transaction.TotalPrice.Should().Be(1526.85m, "a sale yields the gross amount minus fees");
    }

    [Fact]
    public void TotalPrice_ForSaleWhoseFeesExceedGrossAmount_IsNegative()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, 1m, 5m, 8m);

        transaction.TotalPrice.Should().Be(-3m, "a sale can genuinely net negative once costs exceed proceeds");
    }

    [Fact]
    public void Create_WithANegativeFee_FloorsItAtZero()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 2m, 10m, fees: -1m);

        transaction.Fees.Should().Be(0m);
    }

    [Fact]
    public void CreateWithId_WithANegativeFee_FloorsItAtZero()
    {
        var id = Guid.NewGuid();

        var transaction = Transaction.CreateWithId(id, new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, 2m, 10m, fees: -1m);

        transaction.Id.Should().Be(id);
        transaction.Fees.Should().Be(0m);
    }

    /// <summary>
    /// The importer recovers a fee from a recorded total and hands it straight to Create. These
    /// assert the two halves meet: a recovered fee round-trips back to the total it came from.
    /// </summary>
    [Theory]
    [InlineData(Transaction.TransactionType.Buy, 22.5)]
    [InlineData(Transaction.TransactionType.Sell, 19.5)]
    public void Create_WithARecoveredFee_RoundTripsTheRecordedTotal(Transaction.TransactionType type, decimal recordedTotal)
    {
        var fees = TransactionFeeCalculator.RecoverFee(type, 3m, 7m, recordedTotal);

        var transaction = Transaction.Create(new DateTime(2024, 1, 1), type, 3m, 7m, fees);

        transaction.TotalPrice.Should().Be(recordedTotal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithAZeroOrNegativeQuantity_Throws(decimal quantity)
    {
        var act = () => Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, quantity, 10m, 0m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithAZeroOrNegativeUnitPrice_Throws(decimal unitPrice)
    {
        var act = () => Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 2m, unitPrice, 0m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateWithId_WithAZeroOrNegativeQuantity_Throws(decimal quantity)
    {
        var act = () => Transaction.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, quantity, 10m, 0m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateWithId_WithAZeroOrNegativeUnitPrice_Throws(decimal unitPrice)
    {
        var act = () => Transaction.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 2m, unitPrice, 0m);

        act.Should().Throw<ArgumentException>();
    }

}
