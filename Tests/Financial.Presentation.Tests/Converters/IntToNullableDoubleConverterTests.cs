using System.Globalization;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class IntToNullableDoubleConverterTests
{
    private readonly IntToNullableDoubleConverter _converter = new();

    [Fact]
    public void Convert_IntValue_ReturnsDouble()
    {
        var result = _converter.Convert(2026, typeof(double?), null, CultureInfo.InvariantCulture);

        result.Should().Be(2026.0);
    }

    [Fact]
    public void Convert_NonIntValue_ReturnsNull()
    {
        var result = _converter.Convert("not an int", typeof(double?), null, CultureInfo.InvariantCulture);

        result.Should().BeNull();
    }

    [Fact]
    public void ConvertBack_DoubleValue_ReturnsInt()
    {
        var result = _converter.ConvertBack(2026.0, typeof(int), null, CultureInfo.InvariantCulture);

        result.Should().Be(2026);
    }

    [Fact]
    public void ConvertBack_NullValue_ReturnsZero()
    {
        var result = _converter.ConvertBack(null, typeof(int), null, CultureInfo.InvariantCulture);

        result.Should().Be(0);
    }
}
