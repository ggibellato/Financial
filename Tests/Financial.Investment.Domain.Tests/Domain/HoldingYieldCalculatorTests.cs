using Financial.Investment.Domain.Rules;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Domain.Tests;

[Trait("Category", "Unit")]
public class HoldingYieldCalculatorTests
{
    private static readonly DateTime November2026 = new(2026, 11, 25);

    [Fact]
    public void Calculate_FirstBoughtJune2021ViewedNovember2026_SpreadsTheCreditsOver65Months()
    {
        var result = HoldingYieldCalculator.Calculate(
            invested: 2000m, totalCredits: 1000m, firstPurchase: new DateTime(2021, 6, 14),
            lastCredit: 0m, marketValue: null, today: November2026);

        using var _ = new AssertionScope();
        result.CreditsPercentOfInvested.Should().Be(50m);
        result.MonthsHeld.Should().Be(65);
        result.MonthlyYieldPercent.Should().BeApproximately(0.7692m, 0.0001m);
        result.AprPercent.Should().BeApproximately(9.2308m, 0.0001m);
    }

    [Fact]
    public void Calculate_FirstBoughtThisMonth_CountsOneMonthHeld()
    {
        var result = HoldingYieldCalculator.Calculate(
            invested: 1000m, totalCredits: 10m, firstPurchase: new DateTime(2026, 11, 2),
            lastCredit: 0m, marketValue: null, today: November2026);

        using var _ = new AssertionScope();
        result.MonthsHeld.Should().Be(1);
        result.MonthlyYieldPercent.Should().Be(1m);
        result.AprPercent.Should().Be(12m);
    }

    [Fact]
    public void Calculate_LastCreditAgainstMarketValue_ReturnsItsPercent()
    {
        var result = HoldingYieldCalculator.Calculate(
            invested: 1500m, totalCredits: 20m, firstPurchase: new DateTime(2025, 1, 1),
            lastCredit: 20m, marketValue: 2000m, today: November2026);

        result.LastCreditPercentOfMarketValue.Should().Be(1m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void Calculate_WithoutAMarketValue_LeavesTheLastCreditPercentEmpty(int? marketValue)
    {
        var result = HoldingYieldCalculator.Calculate(
            invested: 1500m, totalCredits: 20m, firstPurchase: new DateTime(2025, 1, 1),
            lastCredit: 20m, marketValue: marketValue, today: November2026);

        result.LastCreditPercentOfMarketValue.Should().BeNull();
    }

    [Fact]
    public void Calculate_WithNothingInvested_LeavesTheCreditYieldsEmpty()
    {
        var result = HoldingYieldCalculator.Calculate(
            invested: 0m, totalCredits: 20m, firstPurchase: new DateTime(2025, 1, 1),
            lastCredit: 0m, marketValue: 100m, today: November2026);

        using var _ = new AssertionScope();
        result.CreditsPercentOfInvested.Should().BeNull();
        result.MonthlyYieldPercent.Should().BeNull();
        result.AprPercent.Should().BeNull();
        result.MonthsHeld.Should().Be(22);
    }

    [Fact]
    public void Calculate_WithoutAPurchase_LeavesMonthsHeldAndYieldsEmpty()
    {
        var result = HoldingYieldCalculator.Calculate(
            invested: 100m, totalCredits: 20m, firstPurchase: null,
            lastCredit: 0m, marketValue: 100m, today: November2026);

        using var _ = new AssertionScope();
        result.CreditsPercentOfInvested.Should().Be(20m);
        result.MonthsHeld.Should().BeNull();
        result.MonthlyYieldPercent.Should().BeNull();
        result.AprPercent.Should().BeNull();
    }
}
