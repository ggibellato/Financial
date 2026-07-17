using System.Globalization;
using System.Windows.Media;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class PositionTypeToColorConverterTests
{
    private readonly PositionTypeToColorConverter _converter = new();

    [Fact]
    public void Convert_Long_ReturnsGreenBrush()
    {
        var result = _converter.Convert("Long", typeof(Brush), null, CultureInfo.InvariantCulture);

        result.Should().Be(Brushes.Green);
    }

    [Fact]
    public void Convert_Flat_ReturnsBlackBrush()
    {
        var result = _converter.Convert("Flat", typeof(Brush), null, CultureInfo.InvariantCulture);

        result.Should().Be(Brushes.Black);
    }

    [Fact]
    public void Convert_Short_ReturnsRedBrush()
    {
        var result = _converter.Convert("Short", typeof(Brush), null, CultureInfo.InvariantCulture);

        result.Should().Be(Brushes.Red);
    }

    [Fact]
    public void Convert_UnrecognizedOrNullValue_ReturnsBlackBrush()
    {
        _converter.Convert(null, typeof(Brush), null, CultureInfo.InvariantCulture).Should().Be(Brushes.Black);
        _converter.Convert("Unknown", typeof(Brush), null, CultureInfo.InvariantCulture).Should().Be(Brushes.Black);
    }
}
