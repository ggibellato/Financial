using System.Globalization;
using Financial.Presentation.App.Helpers;
using FluentAssertions;

namespace Financial.Presentation.Tests.Helpers;

public class DateFormatHelperTests
{
    [Fact]
    public void GetPaddedShortDatePattern_SingleDigitDayAndMonthTokens_PadsToTwoDigits()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US"); // ShortDatePattern: "M/d/yyyy"

            var result = DateFormatHelper.GetPaddedShortDatePattern();

            result.Should().Be("MM/dd/yyyy");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void GetPaddedShortDatePattern_AlreadyTwoDigitTokens_LeavesPatternUnchanged()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-GB"); // ShortDatePattern: "dd/MM/yyyy"

            var result = DateFormatHelper.GetPaddedShortDatePattern();

            result.Should().Be("dd/MM/yyyy");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void GetPaddedShortDatePattern_DifferentSeparatorCulture_PadsTokensAndKeepsSeparators()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE"); // ShortDatePattern: "dd.MM.yyyy"

            var result = DateFormatHelper.GetPaddedShortDatePattern();

            result.Should().Be("dd.MM.yyyy");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void GetMonthYearPattern_DayLeadsPattern_StripsDayAndLeadingSeparator()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-GB"); // ShortDatePattern: "dd/MM/yyyy"

            var result = DateFormatHelper.GetMonthYearPattern();

            result.Should().Be("MM/yyyy");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void GetMonthYearPattern_DayTrailsPattern_StripsDayAndTrailingSeparator()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US"); // ShortDatePattern: "M/d/yyyy"

            var result = DateFormatHelper.GetMonthYearPattern();

            result.Should().Be("MM/yyyy");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void GetMonthYearPattern_DifferentSeparatorCulture_KeepsThatSeparator()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE"); // ShortDatePattern: "dd.MM.yyyy"

            var result = DateFormatHelper.GetMonthYearPattern();

            result.Should().Be("MM.yyyy");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void FormatMonthYear_ValidYearAndMonth_ReturnsFormattedString()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-GB");

            var result = DateFormatHelper.FormatMonthYear(2026, 9);

            result.Should().Be("09/2026");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData(0, 0)] // the uninitialized default a bound int ViewModel field holds before it's ever set
    [InlineData(0, 6)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    [InlineData(2026, -1)]
    public void FormatMonthYear_OutOfRangeYearOrMonth_ReturnsEmptyInsteadOfThrowing(int year, int month)
    {
        var result = DateFormatHelper.FormatMonthYear(year, month);

        result.Should().BeEmpty();
    }
}
