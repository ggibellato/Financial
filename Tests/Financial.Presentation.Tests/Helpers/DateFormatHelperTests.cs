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
}
