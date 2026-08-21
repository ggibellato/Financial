using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests.Domain;

public class TransactionFeeCalculatorTests
{
    [Fact]
    public void DeriveFromTotal_ForPurchase_ReturnsTheExcessOverGrossAmount()
    {
        var fees = TransactionFeeCalculator.DeriveFromTotal(Transaction.TransactionType.Buy, 2m, 10m, totalAmount: 21m);

        fees.Should().Be(1m);
    }

    [Fact]
    public void DeriveFromTotal_ForSale_ReturnsTheShortfallBelowGrossAmount()
    {
        var fees = TransactionFeeCalculator.DeriveFromTotal(Transaction.TransactionType.Sell, 2m, 10m, totalAmount: 19m);

        fees.Should().Be(1m);
    }

    /// <summary>
    /// The negative figure is the point of this rule existing separately from the entity: the
    /// entity floors it, and a repair the caller cannot see is indistinguishable from a row that
    /// needed none.
    /// </summary>
    [Fact]
    public void DeriveFromTotal_ForPurchaseWhoseTotalIsBelowGross_ReturnsTheNegativeFigure()
    {
        var fees = TransactionFeeCalculator.DeriveFromTotal(Transaction.TransactionType.Buy, 2m, 10m, totalAmount: 19m);

        fees.Should().Be(-1m);
    }

    [Fact]
    public void DeriveFromTotal_ForSaleWhoseTotalIsAboveGross_ReturnsTheNegativeFigure()
    {
        var fees = TransactionFeeCalculator.DeriveFromTotal(Transaction.TransactionType.Sell, 2m, 10m, totalAmount: 21m);

        fees.Should().Be(-1m);
    }

    /// <summary>The direction is the whole rule: the same inputs mean opposite things per side.</summary>
    [Fact]
    public void DeriveFromTotal_ForTheSameInputs_ReturnsOppositeSignsPerDirection()
    {
        var purchase = TransactionFeeCalculator.DeriveFromTotal(Transaction.TransactionType.Buy, 2m, 10m, totalAmount: 21m);
        var sale = TransactionFeeCalculator.DeriveFromTotal(Transaction.TransactionType.Sell, 2m, 10m, totalAmount: 21m);

        purchase.Should().Be(1m);
        sale.Should().Be(-1m);
    }
}
