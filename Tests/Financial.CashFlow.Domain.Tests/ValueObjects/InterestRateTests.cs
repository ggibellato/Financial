using Financial.CashFlow.Domain.ValueObjects;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.ValueObjects;

[Trait("Category", "Unit")]
public class InterestRateTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4.5)]
    [InlineData(100)]
    public void Create_WithinZeroToOneHundred_KeepsTheRateAndDate(decimal annualPercent)
    {
        var rate = InterestRate.Create(annualPercent, new DateOnly(2026, 9, 1));

        using (new AssertionScope())
        {
            rate.AnnualPercent.Should().Be(annualPercent);
            rate.EffectiveFrom.Should().Be(new DateOnly(2026, 9, 1));
        }
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Create_OutsideZeroToOneHundred_Throws(decimal annualPercent)
    {
        var act = () => InterestRate.Create(annualPercent, new DateOnly(2026, 9, 1));

        act.Should().Throw<ArgumentException>().WithMessage("Interest rate must be between 0% and 100%.");
    }

    [Fact]
    public void Create_WithThreeDecimalPlaces_Throws()
    {
        var act = () => InterestRate.Create(4.125m, new DateOnly(2026, 9, 1));

        act.Should().Throw<ArgumentException>().WithMessage("Interest rate can have at most 2 decimal places.");
    }
}
