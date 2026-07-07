using Financial.Presentation.App.Helpers;
using FluentAssertions;

namespace Financial.Presentation.Tests.Helpers;

public class PeriodFilterHelperTests
{
    private static readonly DateTime ReferenceDate = new(2026, 7, 15);

    [Fact]
    public void GetDateRange_ThisMonth_ReturnsFirstDayOfCurrentMonthThroughNextMonth()
    {
        var (start, endExclusive) = PeriodFilterHelper.GetDateRange(PeriodFilter.ThisMonth, ReferenceDate);

        start.Should().Be(new DateTime(2026, 7, 1));
        endExclusive.Should().Be(new DateTime(2026, 8, 1));
    }

    [Fact]
    public void GetDateRange_Last3Months_ReturnsRollingWindow()
    {
        var (start, _) = PeriodFilterHelper.GetDateRange(PeriodFilter.Last3Months, ReferenceDate);

        start.Should().Be(new DateTime(2026, 5, 1));
    }

    [Fact]
    public void GetDateRange_Last6Months_ReturnsRollingWindow()
    {
        var (start, _) = PeriodFilterHelper.GetDateRange(PeriodFilter.Last6Months, ReferenceDate);

        start.Should().Be(new DateTime(2026, 2, 1));
    }

    [Fact]
    public void GetDateRange_Last12Months_ReturnsRollingWindow()
    {
        var (start, _) = PeriodFilterHelper.GetDateRange(PeriodFilter.Last12Months, ReferenceDate);

        start.Should().Be(new DateTime(2025, 8, 1));
    }

    [Fact]
    public void GetDateRange_Ytd_ReturnsJanuaryFirstOfCurrentYear()
    {
        var (start, endExclusive) = PeriodFilterHelper.GetDateRange(PeriodFilter.Ytd, ReferenceDate);

        start.Should().Be(new DateTime(2026, 1, 1));
        endExclusive.Should().Be(new DateTime(2026, 8, 1));
    }

    [Fact]
    public void GetDateRange_Ytd_WhenReferenceDateIsJanuary_ReturnsSameMonth()
    {
        var (start, _) = PeriodFilterHelper.GetDateRange(PeriodFilter.Ytd, new DateTime(2026, 1, 15));

        start.Should().Be(new DateTime(2026, 1, 1));
    }

    [Fact]
    public void GetDateRange_AllTime_ReturnsNullBounds()
    {
        var (start, endExclusive) = PeriodFilterHelper.GetDateRange(PeriodFilter.AllTime, ReferenceDate);

        start.Should().BeNull();
        endExclusive.Should().BeNull();
    }

    [Fact]
    public void Options_HasExactlySixOptionsInOrder()
    {
        PeriodFilterHelper.Options.Should().Equal(
            ("This month", PeriodFilter.ThisMonth),
            ("Last 3 months", PeriodFilter.Last3Months),
            ("Last 6 months", PeriodFilter.Last6Months),
            ("Last 12 months", PeriodFilter.Last12Months),
            ("YTD", PeriodFilter.Ytd),
            ("All time", PeriodFilter.AllTime));
    }
}
