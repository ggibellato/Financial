using Financial.CashFlow.Domain.ValueObjects;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.ValueObjects;

[Trait("Category", "Unit")]
public class OpeningBalanceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1250.75)]
    public void Create_WithNonNegativeAmount_KeepsAmountAndStartDate(decimal amount)
    {
        var openingBalance = OpeningBalance.Create(amount, new DateOnly(2026, 9, 1));

        using (new AssertionScope())
        {
            openingBalance.Amount.Should().Be(amount);
            openingBalance.StartDate.Should().Be(new DateOnly(2026, 9, 1));
        }
    }

    [Fact]
    public void Create_WithNegativeAmount_Throws()
    {
        var act = () => OpeningBalance.Create(-0.01m, new DateOnly(2026, 9, 1));

        act.Should().Throw<ArgumentException>().WithMessage("Opening balance cannot be negative.");
    }

    [Fact]
    public void Equality_SameAmountAndDate_AreEqual()
    {
        OpeningBalance.Create(10m, new DateOnly(2026, 9, 1))
            .Should().Be(OpeningBalance.Create(10m, new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public void Equality_DifferentDate_AreNotEqual()
    {
        OpeningBalance.Create(10m, new DateOnly(2026, 9, 1))
            .Should().NotBe(OpeningBalance.Create(10m, new DateOnly(2026, 9, 2)));
    }
}
