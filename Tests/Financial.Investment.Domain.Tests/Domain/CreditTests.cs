using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

[Trait("Category", "Unit")]
public class CreditTests
{
    [Fact]
    public void Create_SetsEveryField()
    {
        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.JCP, 10m, withheld: 1m, sharesForDividend: 800m, intermediationFee: 0.5m);

        using (new FluentAssertions.Execution.AssertionScope())
        {
            credit.Id.Should().NotBe(Guid.Empty);
            credit.Date.Should().Be(new DateTime(2024, 1, 1));
            credit.Type.Should().Be(Credit.CreditType.JCP);
            credit.Value.Should().Be(10m);
            credit.Withheld.Should().Be(1m);
            credit.SharesForDividend.Should().Be(800m);
            credit.IntermediationFee.Should().Be(0.5m);
        }
    }

    [Fact]
    public void Create_AssignsCurrencyAndFxRateSnapshot()
    {
        var snapshot = FxRateSnapshot.Create(Currency.GBP, 0.146m, FxRateSource.Frankfurter, TestClock.Default);

        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 10m, withheld: 0m, currency: Currency.BRL, fxRateSnapshot: snapshot);

        using (new FluentAssertions.Execution.AssertionScope())
        {
            credit.Currency.Should().Be(Currency.BRL);
            credit.FxRateSnapshot.Should().Be(snapshot);
        }
    }

    [Fact]
    public void Create_WithoutTheOptionalValues_UsesTheirDefaults()
    {
        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 10m);

        using (new FluentAssertions.Execution.AssertionScope())
        {
            credit.FxRateSnapshot.Should().BeNull();
            credit.SharesForDividend.Should().BeNull();
            credit.IntermediationFee.Should().Be(0m);
        }
    }

    [Fact]
    public void CreateWithId_UsesProvidedId()
    {
        var id = Guid.NewGuid();

        var credit = Credit.CreateWithId(id, new DateTime(2024, 1, 1), Credit.CreditType.SecuritiesLendingIncome, 12m);

        credit.Id.Should().Be(id);
    }

    [Fact]
    public void CreateWithId_EmptyGuid_StoresEmptyId()
    {
        var credit = Credit.CreateWithId(Guid.Empty, new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 10m);

        credit.Id.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Create_WithZeroValue_Throws()
    {
        var act = () => Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 0m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativeValue_IsValidAsACorrection()
    {
        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, -15m);

        credit.Value.Should().Be(-15m);
        credit.Type.Should().Be(Credit.CreditType.Dividend, "a correction uses the same kind as the payment it corrects");
    }

    [Fact]
    public void NetAmount_NoWithheld_EqualsValue()
    {
        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 100m);

        credit.NetAmount.Should().Be(100m);
    }

    [Fact]
    public void NetAmount_WithWithheld_IsValueMinusWithheld()
    {
        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 100m, withheld: 15m);

        credit.NetAmount.Should().Be(85m);
    }

    [Fact]
    public void NetAmount_ForNegativeCorrectionWithWithheld_StaysNegative()
    {
        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, -100m, withheld: -15m);

        credit.NetAmount.Should().Be(-85m);
    }

    [Theory]
    [InlineData(100, -1)]
    [InlineData(100, 101)]
    public void Create_PositiveValueWithOutOfRangeWithheld_Throws(decimal value, decimal withheld)
    {
        var act = () => Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, value, withheld);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BackfillSharesForDividend_NonPositive_Throws()
    {
        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 100m);

        var act = () => credit.BackfillSharesForDividend(0m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(100, 100, 0)]
    [InlineData(-100, -100, 0)]
    [InlineData(100, 0, 100)]
    public void Create_DeductionEqualToTheValue_IsAllowed(decimal value, decimal withheld, decimal intermediationFee)
    {
        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, value, withheld, intermediationFee: intermediationFee);

        credit.NetAmount.Should().Be(0m);
    }

    [Theory]
    [InlineData(-10, 20)]
    [InlineData(20, -10)]
    public void Create_DeductionWithTheOppositeSign_ThrowsEvenWhenTheCombinedTotalFits(decimal withheld, decimal intermediationFee)
    {
        var act = () => Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 100m, withheld, intermediationFee: intermediationFee);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-100, 1)]
    [InlineData(-100, -101)]
    public void Create_NegativeValueWithOutOfRangeWithheld_Throws(decimal value, decimal withheld)
    {
        var act = () => Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, value, withheld);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveSharesForDividend_Throws(decimal sharesForDividend)
    {
        var act = () => Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 10m, sharesForDividend: sharesForDividend);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NetAmount_WithWithheldAndIntermediationFee_SubtractsBoth()
    {
        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.SecuritiesLendingIncome, 0.16m, withheld: 0.03m, intermediationFee: 0.04m);

        credit.NetAmount.Should().Be(0.09m);
    }

    [Theory]
    [InlineData(100, -1)]
    [InlineData(100, 101)]
    public void Create_PositiveValueWithOutOfRangeIntermediationFee_Throws(decimal value, decimal intermediationFee)
    {
        var act = () => Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, value, intermediationFee: intermediationFee);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithheldAndIntermediationFeeCombinedExceedValueMagnitude_Throws()
    {
        var act = () => Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 10m, withheld: 6m, intermediationFee: 6m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateWithId_WithIntermediationFee_AssignsIt()
    {
        var credit = Credit.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 1), Credit.CreditType.SecuritiesLendingIncome, 0.16m, withheld: 0.03m, intermediationFee: 0.04m);

        credit.IntermediationFee.Should().Be(0.04m);
    }
}
