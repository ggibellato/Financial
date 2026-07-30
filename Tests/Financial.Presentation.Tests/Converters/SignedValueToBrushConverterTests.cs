using System.Globalization;
using System.Windows.Media;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class SignedValueToBrushConverterTests
{
    private readonly SignedValueToBrushConverter _converter = new();

    [Fact]
    public void Convert_PositiveDecimal_ReturnsGreenBrush()
    {
        _converter.Convert(10.5m, typeof(Brush), null, CultureInfo.InvariantCulture).Should().Be(Brushes.Green);
    }

    [Fact]
    public void Convert_NegativeDecimal_ReturnsRedBrush()
    {
        _converter.Convert(-10.5m, typeof(Brush), null, CultureInfo.InvariantCulture).Should().Be(Brushes.Red);
    }

    [Fact]
    public void Convert_ZeroDecimal_ReturnsGreenBrush()
    {
        _converter.Convert(0m, typeof(Brush), null, CultureInfo.InvariantCulture).Should().Be(Brushes.Green);
    }

    [Fact]
    public void Convert_PositiveDouble_ReturnsGreenBrush()
    {
        _converter.Convert(10.5d, typeof(Brush), null, CultureInfo.InvariantCulture).Should().Be(Brushes.Green);
    }

    [Fact]
    public void Convert_NegativeDouble_ReturnsRedBrush()
    {
        _converter.Convert(-10.5d, typeof(Brush), null, CultureInfo.InvariantCulture).Should().Be(Brushes.Red);
    }

    [Fact]
    public void Convert_UnsupportedType_ReturnsBlackBrush()
    {
        _converter.Convert("not a number", typeof(Brush), null, CultureInfo.InvariantCulture).Should().Be(Brushes.Black);
    }

    [Fact]
    public void ConvertBack_Always_ThrowsNotImplementedException()
    {
        Action act = () => _converter.ConvertBack(Brushes.Green, typeof(decimal), null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotImplementedException>();
    }
}
