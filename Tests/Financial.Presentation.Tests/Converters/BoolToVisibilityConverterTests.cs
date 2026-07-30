using System.Globalization;
using System.Windows;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class BoolToVisibilityConverterTests
{
    private readonly BoolToVisibilityConverter _converter = new();

    [Fact]
    public void Convert_True_ReturnsVisible()
    {
        _converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Visible);
    }

    [Fact]
    public void Convert_False_ReturnsCollapsed()
    {
        _converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_NonBoolValue_ReturnsCollapsed()
    {
        _converter.Convert("not a bool", typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void ConvertBack_Visible_ReturnsTrue()
    {
        _converter.ConvertBack(Visibility.Visible, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(true);
    }

    [Fact]
    public void ConvertBack_Collapsed_ReturnsFalse()
    {
        _converter.ConvertBack(Visibility.Collapsed, typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
    }

    [Fact]
    public void ConvertBack_NonVisibilityValue_ReturnsFalse()
    {
        _converter.ConvertBack("not a visibility", typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
    }
}
