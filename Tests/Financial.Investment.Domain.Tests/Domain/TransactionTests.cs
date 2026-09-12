using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class TransactionTests
{
    [Fact]
    public void Create_AssignsIdAndNetCash()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 2m, 10m, 1m);

        transaction.Id.Should().NotBe(Guid.Empty);
        transaction.NetCash.Should().Be(-21m);
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
    public void NetCash_ForPurchase_IsNegativeGrossPlusFees()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 100m, 15.33m, 6.15m);

        transaction.NetCash.Should().Be(-1539.15m, "a purchase costs the gross amount plus fees");
    }

    /// <summary>
    /// The fee on a sale is deducted from what is received, not added to it. Adding it overstated
    /// proceeds by twice the fee and propagated into Realized Gain/Loss, Average Sell Price and
    /// the XIRR cash-flow series.
    /// </summary>
    [Fact]
    public void NetCash_ForSale_DeductsFeesFromGrossAmount()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, 100m, 15.33m, 6.15m);

        transaction.NetCash.Should().Be(1526.85m, "a sale yields the gross amount minus fees");
    }

    [Fact]
    public void NetCash_ForSaleWhoseFeesExceedGrossAmount_IsNegative()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, 1m, 5m, 8m);

        transaction.NetCash.Should().Be(-3m, "a sale can genuinely net negative once costs exceed proceeds");
    }

    [Fact]
    public void NetCash_ForSale_DeductsWithheldAlongsideFees()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, 10m, 10m, fees: 1m, withheld: 5m);

        transaction.NetCash.Should().Be(94m, "withheld tax reduces net cash the same way a fee does for an inflow");
    }

    [Fact]
    public void NetCash_ForPurchase_SubtractsWithheldAlongsideFees()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 10m, fees: 1m, withheld: 5m);

        transaction.NetCash.Should().Be(-106m, "withheld tax is still cash that left, on top of the gross amount and fees");
    }

    [Theory]
    [InlineData(Transaction.TransactionType.Fee)]
    [InlineData(Transaction.TransactionType.CapitalCall)]
    public void NetCash_ForNoQuantityEffectOutflowType_IsNegativeFeesOnly(Transaction.TransactionType type)
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), type, 0m, 0m, fees: 10m);

        transaction.NetCash.Should().Be(-10m);
    }

    [Fact]
    public void NetCash_ForReturnOfCapital_IsFeesOnlyDeductedFromNothing()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.ReturnOfCapital, 0m, 0m, fees: 3m);

        transaction.NetCash.Should().Be(-3m, "ReturnOfCapital has no quantity effect, so only its fee (if any) moves cash");
    }

    [Theory]
    [InlineData(Transaction.TransactionType.TransferIn)]
    [InlineData(Transaction.TransactionType.TransferOut)]
    public void NetCash_ForTransfer_IsFeesOnlyRegardlessOfGross(Transaction.TransactionType type)
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), type, 10m, 100m, fees: 2m);

        transaction.NetCash.Should().Be(-2m, "a Transfer's own principal movement has no cash effect; only its fee does");
    }

    [Fact]
    public void NetCash_ForRedemption_BehavesLikeASale()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Redemption, 10m, 10m, fees: 1m);

        transaction.NetCash.Should().Be(99m);
    }

    [Fact]
    public void Create_WithANegativeFee_FloorsItAtZero()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 2m, 10m, fees: -1m);

        transaction.Fees.Should().Be(0m);
    }

    [Fact]
    public void Create_WithANegativeWithheld_FloorsItAtZero()
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Sell, 2m, 10m, fees: 0m, withheld: -1m);

        transaction.Withheld.Should().Be(0m);
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
    /// <see cref="TransactionFeeCalculator"/> still speaks the old unsigned "recorded total"
    /// vocabulary (Buy/Sell only, per research.md), so the round trip is checked against the
    /// signed <see cref="Transaction.NetCash"/> equivalent for each type's cash direction.
    /// </summary>
    [Theory]
    [InlineData(Transaction.TransactionType.Buy, 22.5)]
    [InlineData(Transaction.TransactionType.Sell, 19.5)]
    public void Create_WithARecoveredFee_RoundTripsTheRecordedTotal(Transaction.TransactionType type, decimal recordedTotal)
    {
        var fees = TransactionFeeCalculator.RecoverFee(type, 3m, 7m, recordedTotal);

        var transaction = Transaction.Create(new DateTime(2024, 1, 1), type, 3m, 7m, fees);

        var expectedNetCash = type == Transaction.TransactionType.Buy ? -recordedTotal : recordedTotal;
        transaction.NetCash.Should().Be(expectedNetCash);
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

    [Theory]
    [InlineData(Transaction.TransactionType.Fee)]
    [InlineData(Transaction.TransactionType.CapitalCall)]
    [InlineData(Transaction.TransactionType.ReturnOfCapital)]
    public void Create_ForNoQuantityEffectType_AllowsZeroQuantityAndUnitPrice(Transaction.TransactionType type)
    {
        var transaction = Transaction.Create(new DateTime(2024, 1, 1), type, 0m, 0m, fees: 5m);

        transaction.Quantity.Should().Be(0m);
        transaction.UnitPrice.Should().Be(0m);
    }

    [Theory]
    [InlineData(Transaction.TransactionType.Fee)]
    [InlineData(Transaction.TransactionType.CapitalCall)]
    [InlineData(Transaction.TransactionType.ReturnOfCapital)]
    public void Create_ForNoQuantityEffectType_WithNonZeroQuantity_Throws(Transaction.TransactionType type)
    {
        var act = () => Transaction.Create(new DateTime(2024, 1, 1), type, 1m, 0m, fees: 0m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(Transaction.TransactionType.Fee)]
    [InlineData(Transaction.TransactionType.CapitalCall)]
    [InlineData(Transaction.TransactionType.ReturnOfCapital)]
    public void Create_ForNoQuantityEffectType_WithNonZeroUnitPrice_Throws(Transaction.TransactionType type)
    {
        var act = () => Transaction.Create(new DateTime(2024, 1, 1), type, 0m, 1m, fees: 0m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(Transaction.TransactionType.Redemption)]
    [InlineData(Transaction.TransactionType.TransferIn)]
    [InlineData(Transaction.TransactionType.TransferOut)]
    public void Create_ForQuantityEffectType_RequiresPositiveQuantityAndUnitPrice(Transaction.TransactionType type)
    {
        var act = () => Transaction.Create(new DateTime(2024, 1, 1), type, 0m, 0m, fees: 0m);

        act.Should().Throw<ArgumentException>();
    }
}
