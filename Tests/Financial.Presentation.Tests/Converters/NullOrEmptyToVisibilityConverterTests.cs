using System.Globalization;
using System.Windows;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class NullOrEmptyToVisibilityConverterTests
{
    private readonly NullOrEmptyToVisibilityConverter _converter = new();

    [Fact]
    public void Convert_Null_ReturnsCollapsed()
    {
        _converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_EmptyString_ReturnsCollapsed()
    {
        _converter.Convert(string.Empty, typeof(Visibility), null, CultureInfo.InvariantCulture).Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_NonEmptyString_ReturnsVisible()
    {
        _converter.Convert("Target Balance must be zero or greater.", typeof(Visibility), null, CultureInfo.InvariantCulture)
            .Should().Be(Visibility.Visible);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupported()
    {
        var act = () => _converter.ConvertBack(Visibility.Visible, typeof(string), null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotSupportedException>();
    }
}
