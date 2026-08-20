using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class XirrCalculatorTests
{
    /// <summary>
    /// TASA4 as held in the live data: three purchases and a mark-to-market terminal value at a
    /// 65% loss. The previous Newton-Raphson solver, seeded at +0.10, stepped to -1.258 on its
    /// first iteration; Math.Pow on a negative base with a fractional exponent returned NaN, and
    /// because NaN == 0 is false the zero-derivative guard never tripped, so it exhausted its
    /// iteration limit and reported no rate. The rate exists and is approximately -32.21%.
    /// </summary>
    private static List<(DateTime Date, decimal Amount)> DeeplyNegativePositionCashFlows() =>
    [
        (new DateTime(2023, 8, 30), -1539.15m),
        (new DateTime(2023, 12, 1), -1439.691667m),
        (new DateTime(2025, 5, 12), -157.80m),
        (new DateTime(2026, 8, 20), 1080.20m)
    ];

    [Fact]
    public void Calculate_DeeplyNegativeRate_ReturnsRateInsteadOfNull()
    {
        var result = XirrCalculator.Calculate(DeeplyNegativePositionCashFlows());

        result.Should().NotBeNull();
        result!.Value.Should().BeApproximately(-0.3221m, 0.0005m);
    }

    [Fact]
    public void Calculate_DeeplyNegativeRate_StaysWithinTheDomain()
    {
        var result = XirrCalculator.Calculate(DeeplyNegativePositionCashFlows());

        result!.Value.Should().BeGreaterThan(-1m, "a rate at or below -1 has no real discount factor");
    }

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
        result!.Value.Should().BeApproximately(-0.10m, 0.001m);
    }

    [Fact]
    public void Calculate_NearTotalLossOverOneYear_ReturnsRateJustAboveMinusOne()
    {
        var cashFlows = new List<(DateTime Date, decimal Amount)>
        {
            (new DateTime(2023, 1, 1), -1000m),
            (new DateTime(2024, 1, 1), 1m)
        };

        var result = XirrCalculator.Calculate(cashFlows);

        result.Should().NotBeNull();
        result!.Value.Should().BeApproximately(-0.999m, 0.001m);
        result.Value.Should().BeGreaterThan(-1m);
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

    /// <summary>
    /// A 1,000,000x return overnight implies an annualised rate around 10^2190, which is beyond
    /// the range of a double, so the net present value never crosses zero within the representable
    /// domain and no rate can be reported.
    /// </summary>
    [Fact]
    public void Calculate_ReturnBeyondRepresentableRange_ReturnsNull()
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

    [Fact]
    public void Calculate_AllNegativeCashFlows_ReturnsNull()
    {
        var cashFlows = new List<(DateTime Date, decimal Amount)>
        {
            (new DateTime(2023, 1, 1), -500m),
            (new DateTime(2024, 1, 1), -500m)
        };

        var result = XirrCalculator.Calculate(cashFlows);

        result.Should().BeNull();
    }

    [Fact]
    public void Calculate_AllCashFlowsOnTheSameDate_ReturnsNull()
    {
        var cashFlows = new List<(DateTime Date, decimal Amount)>
        {
            (new DateTime(2024, 1, 1), -500m),
            (new DateTime(2024, 1, 1), 600m)
        };

        var result = XirrCalculator.Calculate(cashFlows);

        result.Should().BeNull();
    }

    [Fact]
    public void Calculate_ZeroTerminalValue_ReturnsTotalLossRate()
    {
        var cashFlows = new List<(DateTime Date, decimal Amount)>
        {
            (new DateTime(2023, 1, 1), -1000m),
            (new DateTime(2023, 6, 1), 400m),
            (new DateTime(2024, 1, 1), 0m)
        };

        var result = XirrCalculator.Calculate(cashFlows);

        result.Should().NotBeNull();
        result!.Value.Should().BeLessThan(0m);
        result.Value.Should().BeGreaterThan(-1m);
    }

    [Fact]
    public void Calculate_SeriesWithInterimCredits_ReturnsRateBetweenBuyAndSell()
    {
        var cashFlows = new List<(DateTime Date, decimal Amount)>
        {
            (new DateTime(2023, 1, 1), -1000m),
            (new DateTime(2023, 7, 1), 50m),
            (new DateTime(2024, 1, 1), 1000m)
        };

        var result = XirrCalculator.Calculate(cashFlows);

        result.Should().NotBeNull();
        result!.Value.Should().BeApproximately(0.0513m, 0.001m);
    }
}
