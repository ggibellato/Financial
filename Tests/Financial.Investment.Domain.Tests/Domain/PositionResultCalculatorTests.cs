using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

[Trait("Category", "Unit")]
public class PositionResultCalculatorTests
{
    [Fact]
    public void ForOpenPosition_WithAGain_ReturnsTheGainAsAPercentOfCost()
    {
        var result = PositionResultCalculator.ForOpenPosition(unrealisedGain: 25m, costOfUnitsHeld: 200m, totalCredits: 10m);

        result.ResultPercent.Should().Be(12.5m);
        result.ResultWithCreditsPercent.Should().Be(17.5m);
    }

    [Fact]
    public void ForOpenPosition_WithALoss_ReturnsANegativePercent()
    {
        var result = PositionResultCalculator.ForOpenPosition(unrealisedGain: -50m, costOfUnitsHeld: 200m, totalCredits: 20m);

        result.ResultPercent.Should().Be(-25m);
        result.ResultWithCreditsPercent.Should().Be(-15m);
    }

    [Fact]
    public void ForOpenPosition_WithoutAPrice_IsUnavailable()
    {
        var result = PositionResultCalculator.ForOpenPosition(unrealisedGain: null, costOfUnitsHeld: 200m, totalCredits: 10m);

        result.ResultPercent.Should().BeNull();
        result.ResultWithCreditsPercent.Should().BeNull();
    }

    [Fact]
    public void ForOpenPosition_WithNoCost_IsUnavailable()
    {
        var result = PositionResultCalculator.ForOpenPosition(unrealisedGain: 0m, costOfUnitsHeld: 0m, totalCredits: 10m);

        result.ResultPercent.Should().BeNull();
        result.ResultWithCreditsPercent.Should().BeNull();
    }

    [Fact]
    public void ForClosedPosition_ExcludesTheCreditsOnlyFromTheResultPercent()
    {
        var result = PositionResultCalculator.ForClosedPosition(realizedGainLoss: 60m, totalCredits: 20m, totalBought: 400m);

        result.ResultPercent.Should().Be(10m);
        result.ResultWithCreditsPercent.Should().Be(15m);
    }

    [Fact]
    public void ForClosedPosition_WithNothingBought_IsUnavailable()
    {
        var result = PositionResultCalculator.ForClosedPosition(realizedGainLoss: 5m, totalCredits: 5m, totalBought: 0m);

        result.ResultPercent.Should().BeNull();
        result.ResultWithCreditsPercent.Should().BeNull();
    }
}
