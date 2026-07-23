using Financial.CashFlow.Domain.Rules;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class ReserveSplitCalculatorTests
{
    [Fact]
    public void Calculate_SplitsAmountIntoExactThirdsAndSixths()
    {
        var result = ReserveSplitCalculator.Calculate(1963m);

        result.Investimento.Should().Be(654.33m);
        result.HouseTreats.Should().Be(654.33m);
        result.Ariana.Should().Be(327.17m);
        result.Gleison.Should().Be(327.17m);
    }

    [Fact]
    public void Calculate_ZeroAmount_ProducesAllZeroOutputs()
    {
        var result = ReserveSplitCalculator.Calculate(0m);

        result.Investimento.Should().Be(0m);
        result.HouseTreats.Should().Be(0m);
        result.Ariana.Should().Be(0m);
        result.Gleison.Should().Be(0m);
    }
}
