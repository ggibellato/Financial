using System.Globalization;
using System.Windows;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class DividendAttributionToVisibilityConverterTests
{
    private readonly DividendAttributionToVisibilityConverter _converter = new();

    [Fact]
    public void Convert_DecimalValue_ReturnsVisible()
    {
        _converter.Convert(1000m, typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Visible);
    }

    [Fact]
    public void Convert_Null_ReturnsCollapsed()
    {
        _converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void ConvertBack_Throws()
    {
        var act = () => _converter.ConvertBack(Visibility.Visible, typeof(decimal?), null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotSupportedException>();
    }
}
