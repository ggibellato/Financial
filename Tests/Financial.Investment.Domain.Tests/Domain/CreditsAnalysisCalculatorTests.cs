using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Domain.Tests;

[Trait("Category", "Unit")]
public class CreditsAnalysisCalculatorTests
{
    private static Credit Dividend(int year, int month, int day, decimal value) =>
        Credit.Create(new DateTime(year, month, day), Credit.CreditType.Dividend, value);

    [Fact]
    public void Calculate_QuarterlyCredits_SumsTheLatestMonthOnOrBeforeTodayAndAnnualisesIt()
    {
        var credits = new[]
        {
            Dividend(2025, 12, 5, 8m),
            Dividend(2026, 1, 10, 10m),
            Dividend(2026, 4, 8, 12m),
            Dividend(2026, 4, 20, 3m),
            Dividend(2026, 7, 10, 50m)
        };

        var analysis = CreditsAnalysisCalculator.Calculate(credits, weightBasis: 300m, today: new DateTime(2026, 5, 5));

        using (new AssertionScope())
        {
            analysis.LastMonthCredits.Should().Be(15m);
            analysis.LastCreditMonth.Should().Be("2026-04");
            analysis.LastMonthCreditsPercent.Should().Be(5m);
            analysis.CreditFrequencyPerYear.Should().Be(4);
            analysis.EstimatedAnnualCredits.Should().Be(60m);
            analysis.EstimatedAnnualPercent.Should().Be(20m);
            analysis.CurrentMonthCredits.Should().Be(0m);
        }
    }

    [Fact]
    public void Calculate_CreditDatedToday_IsTheLatestCredit()
    {
        var credits = new[] { Dividend(2026, 3, 1, 5m), Dividend(2026, 4, 8, 7m) };

        var analysis = CreditsAnalysisCalculator.Calculate(credits, weightBasis: 100m, today: new DateTime(2026, 4, 8));

        using (new AssertionScope())
        {
            analysis.LastMonthCredits.Should().Be(7m);
            analysis.LastCreditMonth.Should().Be("2026-04");
        }
    }

    [Fact]
    public void Calculate_CurrentMonthCredits_SumsEveryCreditOfThisCalendarMonthOnly()
    {
        var credits = new[]
        {
            Dividend(2025, 7, 2, 100m),
            Dividend(2026, 6, 30, 1m),
            Dividend(2026, 7, 1, 4m),
            Dividend(2026, 7, 20, 6m)
        };

        var analysis = CreditsAnalysisCalculator.Calculate(credits, weightBasis: 100m, today: new DateTime(2026, 7, 5));

        analysis.CurrentMonthCredits.Should().Be(10m);
    }

    [Fact]
    public void Calculate_NoCreditOnOrBeforeToday_ReportsZeroWithNoMonthOrPercentages()
    {
        var credits = new[] { Dividend(2026, 8, 10, 20m) };

        var analysis = CreditsAnalysisCalculator.Calculate(credits, weightBasis: 300m, today: new DateTime(2026, 7, 5));

        using (new AssertionScope())
        {
            analysis.LastMonthCredits.Should().Be(0m);
            analysis.LastCreditMonth.Should().BeNull();
            analysis.LastMonthCreditsPercent.Should().BeNull();
            analysis.EstimatedAnnualCredits.Should().BeNull();
            analysis.EstimatedAnnualPercent.Should().BeNull();
        }
    }

    [Fact]
    public void Calculate_ZeroWeightBasis_KeepsTheAmountsButLeavesThePercentagesEmpty()
    {
        var credits = new[] { Dividend(2026, 1, 10, 10m), Dividend(2026, 4, 8, 15m) };

        var analysis = CreditsAnalysisCalculator.Calculate(credits, weightBasis: 0m, today: new DateTime(2026, 5, 5));

        using (new AssertionScope())
        {
            analysis.LastMonthCredits.Should().Be(15m);
            analysis.EstimatedAnnualCredits.Should().Be(60m);
            analysis.LastMonthCreditsPercent.Should().BeNull();
            analysis.EstimatedAnnualPercent.Should().BeNull();
        }
    }

    [Fact]
    public void Calculate_NoDetectableFrequency_LeavesTheAnnualEstimateEmpty()
    {
        var credits = new[] { Dividend(2026, 4, 8, 12m) };

        var analysis = CreditsAnalysisCalculator.Calculate(credits, weightBasis: 300m, today: new DateTime(2026, 5, 5));

        using (new AssertionScope())
        {
            analysis.LastMonthCreditsPercent.Should().Be(4m);
            analysis.CreditFrequencyPerYear.Should().BeNull();
            analysis.EstimatedAnnualCredits.Should().BeNull();
            analysis.EstimatedAnnualPercent.Should().BeNull();
        }
    }
}
