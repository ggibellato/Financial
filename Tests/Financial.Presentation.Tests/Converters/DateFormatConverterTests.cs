using System.Globalization;
using Financial.Presentation.App.Converters;
using Financial.Presentation.App.Helpers;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class DateFormatConverterTests
{
    private readonly DateFormatConverter _converter = new();

    [Fact]
    public void Convert_WithExplicitFormatParameter_FormatsUsingThatFormat()
    {
        var date = new DateTime(2026, 7, 5);

        var result = _converter.Convert(date, typeof(string), "yyyy-MM-dd", CultureInfo.InvariantCulture);

        result.Should().Be("2026-07-05");
    }

    [Fact]
    public void Convert_WithNullFormatParameter_UsesPaddedShortDatePattern()
    {
        var date = new DateTime(2026, 7, 5);

        var result = _converter.Convert(date, typeof(string), null, CultureInfo.InvariantCulture);

        var expectedFormat = DateFormatHelper.GetPaddedShortDatePattern();
        result.Should().Be(date.ToString(expectedFormat, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Convert_WithLowercaseDFormatParameter_UsesPaddedShortDatePattern()
    {
        var date = new DateTime(2026, 7, 5);

        var result = _converter.Convert(date, typeof(string), "d", CultureInfo.InvariantCulture);

        var expectedFormat = DateFormatHelper.GetPaddedShortDatePattern();
        result.Should().Be(date.ToString(expectedFormat, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Convert_WithDateOnlyValue_UsesPaddedShortDatePattern()
    {
        var date = new DateOnly(2026, 7, 5);

        var result = _converter.Convert(date, typeof(string), null, CultureInfo.InvariantCulture);

        var expectedFormat = DateFormatHelper.GetPaddedShortDatePattern();
        result.Should().Be(date.ToDateTime(TimeOnly.MinValue).ToString(expectedFormat, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Convert_WithDateOnlyValueAndExplicitFormatParameter_FormatsUsingThatFormat()
    {
        var date = new DateOnly(2026, 7, 5);

        var result = _converter.Convert(date, typeof(string), "yyyy-MM-dd", CultureInfo.InvariantCulture);

        result.Should().Be("2026-07-05");
    }

    [Fact]
    public void Convert_NonDateValue_ReturnsEmptyString()
    {
        var result = _converter.Convert("not a date", typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertBack_ValidDateString_ParsesToDateTime()
    {
        var result = _converter.ConvertBack("2026-07-05", typeof(DateTime), null, CultureInfo.InvariantCulture);

        result.Should().Be(new DateTime(2026, 7, 5));
    }

    [Fact]
    public void ConvertBack_InvalidString_ReturnsDateTimeMinValue()
    {
        var result = _converter.ConvertBack("not a date", typeof(DateTime), null, CultureInfo.InvariantCulture);

        result.Should().Be(DateTime.MinValue);
    }
}
