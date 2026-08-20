using Financial.Investment.Domain.Entities;
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
    public void CreateFromTotal_ForPurchase_DerivesFeesAsExcessOverGrossAmount()
    {
        var transaction = Transaction.CreateFromTotal(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 2m, 10m, totalAmount: 21m);

        transaction.Fees.Should().Be(1m);
        transaction.TotalPrice.Should().Be(21m);
    }

    /// <summary>
    /// A sale's recorded total is the net proceeds, which fall short of the gross amount by the
    /// fee. Deriving it in the purchase direction produced a negative fee that was floored to
    /// zero, silently discarding it.
    /// </summary>
    [Fact]
    public void CreateFromTotal_ForSale_DerivesFeesAsShortfallBelowGrossAmount()
    {
        var transaction = Transaction.CreateFromTotal(new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, 2m, 10m, totalAmount: 19m);

        transaction.Fees.Should().Be(1m);
        transaction.TotalPrice.Should().Be(19m);
    }

    [Fact]
    public void CreateFromTotal_ForPurchaseWhenDerivedFeesWouldBeNegative_FloorsAtZero()
    {
        var transaction = Transaction.CreateFromTotal(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 2m, 10m, totalAmount: 19m);

        transaction.Fees.Should().Be(0m);
    }

    [Fact]
    public void CreateFromTotal_ForSaleWhenDerivedFeesWouldBeNegative_FloorsAtZero()
    {
        var transaction = Transaction.CreateFromTotal(new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, 2m, 10m, totalAmount: 21m);

        transaction.Fees.Should().Be(0m);
    }

    [Fact]
    public void CreateFromTotal_RoundTripsTheRecordedTotalForBothDirections()
    {
        var purchase = Transaction.CreateFromTotal(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 3m, 7m, totalAmount: 22.5m);
        var sale = Transaction.CreateFromTotal(new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, 3m, 7m, totalAmount: 19.5m);

        purchase.TotalPrice.Should().Be(22.5m);
        sale.TotalPrice.Should().Be(19.5m);
    }
}
