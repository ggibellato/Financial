using Financial.CashFlow.Domain.ValueObjects;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.ValueObjects;

public class MonthlySeriesTests
{
    private static readonly decimal[] TwelveMonths =
        [10m, 20m, 30m, 40m, 50m, 60m, 70m, 80m, 90m, 100m, 110m, 120m];

    [Fact]
    public void FromMonthlyValues_With12Values_StoresThem()
    {
        var series = MonthlySeries.FromMonthlyValues(TwelveMonths);

        series[0].Should().Be(10m);
        series[11].Should().Be(120m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(13)]
    public void FromMonthlyValues_WithWrongLength_Throws(int length)
    {
        var values = new decimal[length];

        var act = () => MonthlySeries.FromMonthlyValues(values);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Zero_AllTwelveMonthsAreZero()
    {
        var series = MonthlySeries.Zero();

        series.AsReadOnly().Should().AllBeEquivalentTo(0m);
    }

    [Fact]
    public void Sum_ReturnsTotalAcrossAllMonths()
    {
        var series = MonthlySeries.FromMonthlyValues(TwelveMonths);

        series.Sum().Should().Be(780m);
    }

    [Fact]
    public void Average_DividesByMonthsElapsedAndRounds()
    {
        var series = MonthlySeries.FromMonthlyValues([10m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m]);

        series.Average(monthsElapsed: 3, decimalPlaces: 2).Should().Be(3.33m);
    }

    [Fact]
    public void Average_WithZeroMonthsElapsed_ReturnsZero()
    {
        var series = MonthlySeries.FromMonthlyValues(TwelveMonths);

        series.Average(monthsElapsed: 0, decimalPlaces: 2).Should().Be(0m);
    }

    [Fact]
    public void DiffsFrom_WithPriorClosingValue_FirstElementIsJanuaryMinusPriorClosing()
    {
        var series = MonthlySeries.FromMonthlyValues(TwelveMonths);

        var diffs = series.DiffsFrom(priorClosingValue: 5m);

        diffs[0].Should().Be(5m);
    }

    [Fact]
    public void DiffsFrom_WithNullPriorClosingValue_FirstElementIsNull()
    {
        var series = MonthlySeries.FromMonthlyValues(TwelveMonths);

        var diffs = series.DiffsFrom(priorClosingValue: null);

        diffs[0].Should().BeNull();
    }

    [Fact]
    public void DiffsFrom_SubsequentElements_AreMonthMinusPreviousMonth()
    {
        var series = MonthlySeries.FromMonthlyValues(TwelveMonths);

        var diffs = series.DiffsFrom(priorClosingValue: null);

        diffs[1].Should().Be(10m);
        diffs[11].Should().Be(10m);
    }

    [Fact]
    public void Add_SumsElementWise_ReturnsNewInstance()
    {
        var first = MonthlySeries.FromMonthlyValues(TwelveMonths);
        var second = MonthlySeries.Zero();

        var result = first.Add(second);

        result.Should().NotBeSameAs(first);
        result[0].Should().Be(10m);
        result[11].Should().Be(120m);
    }

    [Fact]
    public void Equals_TwoInstancesWithSameValues_AreEqual()
    {
        var first = MonthlySeries.FromMonthlyValues(TwelveMonths);
        var second = MonthlySeries.FromMonthlyValues(TwelveMonths);

        first.Should().Be(second);
    }

    [Fact]
    public void Equals_DifferentValues_AreNotEqual()
    {
        var first = MonthlySeries.FromMonthlyValues(TwelveMonths);
        var second = MonthlySeries.Zero();

        first.Should().NotBe(second);
    }
}
