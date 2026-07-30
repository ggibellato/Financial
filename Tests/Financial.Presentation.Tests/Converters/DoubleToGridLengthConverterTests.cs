using System.Globalization;
using System.Windows;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class DoubleToGridLengthConverterTests
{
    private readonly DoubleToGridLengthConverter _converter = new();

    [Fact]
    public void Convert_DoubleValue_ReturnsPixelGridLength()
    {
        var result = _converter.Convert(120.0, typeof(GridLength), null, CultureInfo.InvariantCulture);

        result.Should().Be(new GridLength(120.0));
    }

    [Fact]
    public void Convert_NonDoubleValue_ReturnsAuto()
    {
        var result = _converter.Convert("not a double", typeof(GridLength), null, CultureInfo.InvariantCulture);

        result.Should().Be(GridLength.Auto);
    }

    [Fact]
    public void ConvertBack_Always_ThrowsNotSupportedException()
    {
        Action act = () => _converter.ConvertBack(new GridLength(120.0), typeof(double), null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotSupportedException>();
    }
}
