using System.Globalization;
using System.Windows.Media;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class BillStatusToBrushConverterTests
{
    private readonly BillStatusToBrushConverter _converter = new();

    [Theory]
    [InlineData("Unset", "#FFFFFFFF")]
    [InlineData("Scheduled", "#FFEBEBEB")]
    [InlineData("Paid", "#FF107C10")]
    [InlineData("NotAStatus", "#FFFFFFFF")]
    public void Convert_Background_ReturnsExpectedBrush(string status, string expectedHex)
    {
        var result = _converter.Convert(status, typeof(Brush), "Background", CultureInfo.InvariantCulture);

        result.Should().BeOfType<SolidColorBrush>()
            .Which.Color.Should().Be((Color)ColorConverter.ConvertFromString(expectedHex)!);
    }

    [Theory]
    [InlineData("Unset", "#FF242424")]
    [InlineData("Scheduled", "#FF616161")]
    [InlineData("Paid", "#FFFFFFFF")]
    [InlineData("NotAStatus", "#FF242424")]
    public void Convert_Foreground_ReturnsExpectedBrush(string status, string expectedHex)
    {
        var result = _converter.Convert(status, typeof(Brush), "Foreground", CultureInfo.InvariantCulture);

        result.Should().BeOfType<SolidColorBrush>()
            .Which.Color.Should().Be((Color)ColorConverter.ConvertFromString(expectedHex)!);
    }

    [Fact]
    public void Convert_NonStringValue_ReturnsUnsetBrush()
    {
        var result = _converter.Convert(42, typeof(Brush), "Background", CultureInfo.InvariantCulture);

        result.Should().BeOfType<SolidColorBrush>()
            .Which.Color.Should().Be((Color)ColorConverter.ConvertFromString("#FFFFFFFF")!);
    }

    [Fact]
    public void ConvertBack_Always_ThrowsNotImplementedException()
    {
        Action act = () => _converter.ConvertBack(Brushes.Green, typeof(string), null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotImplementedException>();
    }
}
