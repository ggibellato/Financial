using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class XirrCalculatorTests
{
    [Fact]
    public void Calculate_FewerThanTwoCashFlows_ReturnsNull()
    {
        var result = XirrCalculator.Calculate([(new DateTime(2024, 1, 1), -1000m)]);

        result.Should().BeNull();
    }

    [Fact]
    public void Calculate_TenPercentGrowthOverOneYear_ReturnsApproximatelyTenPercent()
    {
        var cashFlows = new List<(DateTime Date, decimal Amount)>
        {
            (new DateTime(2023, 1, 1), -1000m),
            (new DateTime(2024, 1, 1), 1100m)
        };

        var result = XirrCalculator.Calculate(cashFlows);

        result.Should().NotBeNull();
        result!.Value.Should().BeApproximately(0.10m, 0.001m);
    }

    [Fact]
    public void Calculate_LossOverOneYear_ReturnsNegativeRate()
    {
        var cashFlows = new List<(DateTime Date, decimal Amount)>
        {
            (new DateTime(2023, 1, 1), -1000m),
            (new DateTime(2024, 1, 1), 900m)
        };

        var result = XirrCalculator.Calculate(cashFlows);

        result.Should().NotBeNull();
        result!.Value.Should().BeLessThan(0m);
    }

    [Fact]
    public void Calculate_UnorderedCashFlows_MatchesResultOfOrderedCashFlows()
    {
        var ordered = new List<(DateTime Date, decimal Amount)>
        {
            (new DateTime(2022, 1, 1), -1000m),
            (new DateTime(2023, 1, 1), 100m),
            (new DateTime(2024, 1, 1), 1100m)
        };
        var unordered = new List<(DateTime Date, decimal Amount)>
        {
            (new DateTime(2024, 1, 1), 1100m),
            (new DateTime(2022, 1, 1), -1000m),
            (new DateTime(2023, 1, 1), 100m)
        };

        var orderedResult = XirrCalculator.Calculate(ordered);
        var unorderedResult = XirrCalculator.Calculate(unordered);

        unorderedResult.Should().Be(orderedResult);
    }

    [Fact]
    public void Calculate_ExtremeReturnThatDoesNotConvergeWithinIterationLimit_ReturnsNull()
    {
        var cashFlows = new List<(DateTime Date, decimal Amount)>
        {
            (new DateTime(2024, 1, 1), -1m),
            (new DateTime(2024, 1, 2), 1_000_000m)
        };

        var result = XirrCalculator.Calculate(cashFlows);

        result.Should().BeNull();
    }

    [Fact]
    public void Calculate_AllPositiveCashFlows_ReturnsNull()
    {
        var cashFlows = new List<(DateTime Date, decimal Amount)>
        {
            (new DateTime(2023, 1, 1), 500m),
            (new DateTime(2024, 1, 1), 500m)
        };

        var result = XirrCalculator.Calculate(cashFlows);

        result.Should().BeNull();
    }
}
