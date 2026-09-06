using System.Globalization;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class DateOnlyToDateTimeConverterTests
{
    private readonly DateOnlyToDateTimeConverter _converter = new();

    [Fact]
    public void Convert_DateOnly_ReturnsDateTimeAtMidnight()
    {
        var result = _converter.Convert(new DateOnly(2026, 3, 15), typeof(DateTime), null, CultureInfo.InvariantCulture);

        result.Should().Be(new DateTime(2026, 3, 15, 0, 0, 0));
    }

    [Fact]
    public void Convert_NonDateOnlyValue_ReturnsNull()
    {
        _converter.Convert("not a date", typeof(DateTime), null, CultureInfo.InvariantCulture).Should().BeNull();
    }

    [Fact]
    public void ConvertBack_DateTime_ReturnsDateOnly()
    {
        var result = _converter.ConvertBack(new DateTime(2026, 3, 15, 13, 45, 0), typeof(DateOnly), null, CultureInfo.InvariantCulture);

        result.Should().Be(new DateOnly(2026, 3, 15));
    }

    [Fact]
    public void ConvertBack_NonDateTimeValue_ReturnsNull()
    {
        _converter.ConvertBack("not a date", typeof(DateOnly), null, CultureInfo.InvariantCulture).Should().BeNull();
    }
}
