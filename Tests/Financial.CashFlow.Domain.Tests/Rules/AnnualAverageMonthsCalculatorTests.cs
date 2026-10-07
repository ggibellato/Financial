using Financial.CashFlow.Domain.Rules;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class AnnualAverageMonthsCalculatorTests
{
    [Fact]
    public void NumberOfMonthsForAverage_CurrentYearInJanuary_ReturnsZero()
    {
        var now = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

        AnnualAverageMonthsCalculator.NumberOfMonthsForAverage(now, 2026).Should().Be(0);
    }

    [Fact]
    public void NumberOfMonthsForAverage_CurrentYearInOctober_ReturnsTheNineCompletedMonths()
    {
        var now = new DateTimeOffset(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);

        AnnualAverageMonthsCalculator.NumberOfMonthsForAverage(now, 2026).Should().Be(9);
    }

    [Fact]
    public void NumberOfMonthsForAverage_Year2017_ReturnsElevenMonths()
    {
        var now = new DateTimeOffset(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);

        AnnualAverageMonthsCalculator.NumberOfMonthsForAverage(now, 2017).Should().Be(11);
    }

    [Fact]
    public void NumberOfMonthsForAverage_OtherPastYear_ReturnsTwelveMonths()
    {
        var now = new DateTimeOffset(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);

        AnnualAverageMonthsCalculator.NumberOfMonthsForAverage(now, 2024).Should().Be(12);
    }
}
