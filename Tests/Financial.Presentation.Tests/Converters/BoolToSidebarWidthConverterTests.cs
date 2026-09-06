using System.Globalization;
using System.Windows;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class BoolToSidebarWidthConverterTests
{
    private readonly BoolToSidebarWidthConverter _converter = new();

    [Fact]
    public void Convert_True_ReturnsCollapsedWidth()
    {
        _converter.Convert(true, typeof(GridLength), null, CultureInfo.InvariantCulture).Should().Be(new GridLength(56));
    }

    [Fact]
    public void Convert_False_ReturnsExpandedWidth()
    {
        _converter.Convert(false, typeof(GridLength), null, CultureInfo.InvariantCulture).Should().Be(new GridLength(240));
    }

    [Fact]
    public void Convert_NonBoolValue_ReturnsExpandedWidth()
    {
        _converter.Convert("not a bool", typeof(GridLength), null, CultureInfo.InvariantCulture).Should().Be(new GridLength(240));
    }

    [Fact]
    public void ConvertBack_Throws()
    {
        var act = () => _converter.ConvertBack(new GridLength(56), typeof(bool), null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotSupportedException>();
    }
}
