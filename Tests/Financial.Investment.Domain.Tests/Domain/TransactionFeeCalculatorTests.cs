using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests.Domain;

public class TransactionFeeCalculatorTests
{
    [Fact]
    public void RecoverFee_ForPurchase_ReturnsTheExcessOverGrossAmount()
    {
        var fees = TransactionFeeCalculator.RecoverFee(Transaction.TransactionType.Buy, 2m, 10m, totalPrice: 21m);

        fees.Should().Be(1m);
    }

    [Fact]
    public void RecoverFee_ForSale_ReturnsTheShortfallBelowGrossAmount()
    {
        var fees = TransactionFeeCalculator.RecoverFee(Transaction.TransactionType.Sell, 2m, 10m, totalPrice: 19m);

        fees.Should().Be(1m);
    }

    /// <summary>
    /// The negative figure is the point of this rule existing separately from the entity: the
    /// entity floors it, and a repair the caller cannot see is indistinguishable from a row that
    /// needed none.
    /// </summary>
    [Fact]
    public void RecoverFee_ForPurchaseWhoseTotalIsBelowGross_ReturnsTheNegativeFigure()
    {
        var fees = TransactionFeeCalculator.RecoverFee(Transaction.TransactionType.Buy, 2m, 10m, totalPrice: 19m);

        fees.Should().Be(-1m);
    }

    [Fact]
    public void RecoverFee_ForSaleWhoseTotalIsAboveGross_ReturnsTheNegativeFigure()
    {
        var fees = TransactionFeeCalculator.RecoverFee(Transaction.TransactionType.Sell, 2m, 10m, totalPrice: 21m);

        fees.Should().Be(-1m);
    }

    /// <summary>The direction is the whole rule: the same inputs mean opposite things per side.</summary>
    [Fact]
    public void RecoverFee_ForTheSameInputs_ReturnsOppositeSignsPerDirection()
    {
        var purchase = TransactionFeeCalculator.RecoverFee(Transaction.TransactionType.Buy, 2m, 10m, totalPrice: 21m);
        var sale = TransactionFeeCalculator.RecoverFee(Transaction.TransactionType.Sell, 2m, 10m, totalPrice: 21m);

        purchase.Should().Be(1m);
        sale.Should().Be(-1m);
    }
}
