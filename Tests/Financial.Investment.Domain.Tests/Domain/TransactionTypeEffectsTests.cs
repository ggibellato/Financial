using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class TransactionTypeEffectsTests
{
    [Theory]
    [InlineData(Transaction.TransactionType.Buy, QuantityEffect.Increase, CashEffect.Out)]
    [InlineData(Transaction.TransactionType.Sell, QuantityEffect.Decrease, CashEffect.In)]
    [InlineData(Transaction.TransactionType.Fee, QuantityEffect.None, CashEffect.Out)]
    [InlineData(Transaction.TransactionType.Redemption, QuantityEffect.Decrease, CashEffect.In)]
    [InlineData(Transaction.TransactionType.TransferIn, QuantityEffect.Increase, CashEffect.None)]
    [InlineData(Transaction.TransactionType.TransferOut, QuantityEffect.Decrease, CashEffect.None)]
    [InlineData(Transaction.TransactionType.CapitalCall, QuantityEffect.None, CashEffect.Out)]
    [InlineData(Transaction.TransactionType.ReturnOfCapital, QuantityEffect.None, CashEffect.In)]
    public void For_DeclaresTheExpectedEffects(Transaction.TransactionType type, QuantityEffect expectedQuantity, CashEffect expectedCash)
    {
        var effect = TransactionTypeEffects.For(type);

        effect.Quantity.Should().Be(expectedQuantity);
        effect.Cash.Should().Be(expectedCash);
    }

    [Fact]
    public void For_UnknownType_Throws()
    {
        var act = () => TransactionTypeEffects.For((Transaction.TransactionType)999);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
