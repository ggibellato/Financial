using Financial.CashFlow.Domain.Rules;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class AnnualAverageMonthsCalculatorTests
{
    [Theory]
    [InlineData(2026, 1, 15, 2026, 0)]
    [InlineData(2026, 10, 7, 2026, 9)]
    [InlineData(2026, 10, 7, 2017, 11)]
    [InlineData(2026, 10, 7, 2024, 12)]
    public void NumberOfMonthsForAverage_ReturnsTheCompletedMonthsForTheCurrentYearAndTheFixedCountsForPastYears(
        int nowYear, int nowMonth, int nowDay, int year, int expectedMonths)
    {
        var now = new DateTimeOffset(nowYear, nowMonth, nowDay, 0, 0, 0, TimeSpan.Zero);

        AnnualAverageMonthsCalculator.NumberOfMonthsForAverage(now, year).Should().Be(expectedMonths);
    }
}
