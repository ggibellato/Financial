using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class CreditTests
{
    [Fact]
    public void Create_AssignsId()
    {
        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 10m);

        credit.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void CreateWithId_UsesProvidedId()
    {
        var id = Guid.NewGuid();

        var credit = Credit.CreateWithId(id, new DateTime(2024, 1, 1), Credit.CreditType.SecuritiesLendingIncome, 12m);

        credit.Id.Should().Be(id);
    }

    [Fact]
    public void Create_WithJcpType_AssignsType()
    {
        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.JCP, 10m);

        credit.Type.Should().Be(Credit.CreditType.JCP);
    }

    [Fact]
    public void Create_WithCouponType_AssignsType()
    {
        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Coupon, 10m);

        credit.Type.Should().Be(Credit.CreditType.Coupon);
    }

    [Fact]
    public void Create_WithSecuritiesLendingIncomeType_AssignsType()
    {
        var credit = Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.SecuritiesLendingIncome, 10m);

        credit.Type.Should().Be(Credit.CreditType.SecuritiesLendingIncome);
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
    public void CreateWithId_WithZeroValue_Throws()
    {
        var act = () => Credit.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 0m);

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

    [Theory]
    [InlineData(-100, 1)]
    [InlineData(-100, -101)]
    public void Create_NegativeValueWithOutOfRangeWithheld_Throws(decimal value, decimal withheld)
    {
        var act = () => Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, value, withheld);

        act.Should().Throw<ArgumentException>();
    }
}
