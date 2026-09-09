using System.Globalization;
using System.Windows.Media;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class CalendarSyncStateToBrushConverterTests
{
    private readonly CalendarSyncStateToBrushConverter _converter = new();

    [Theory]
    [InlineData("Synced", "#FF107C10")]
    [InlineData("Error", "#FFD13438")]
    [InlineData("Pending", "#FFEBEBEB")]
    [InlineData(null, "#FFEBEBEB")]
    [InlineData("NotAState", "#FFEBEBEB")]
    public void Convert_Background_ReturnsExpectedBrush(string? state, string expectedHex)
    {
        var result = _converter.Convert(state, typeof(Brush), "Background", CultureInfo.InvariantCulture);

        result.Should().BeOfType<SolidColorBrush>()
            .Which.Color.Should().Be((Color)ColorConverter.ConvertFromString(expectedHex)!);
    }

    [Theory]
    [InlineData("Synced", "#FFFFFFFF")]
    [InlineData("Error", "#FFFFFFFF")]
    [InlineData("Pending", "#FF616161")]
    [InlineData(null, "#FF616161")]
    public void Convert_Foreground_ReturnsExpectedBrush(string? state, string expectedHex)
    {
        var result = _converter.Convert(state, typeof(Brush), "Foreground", CultureInfo.InvariantCulture);

        result.Should().BeOfType<SolidColorBrush>()
            .Which.Color.Should().Be((Color)ColorConverter.ConvertFromString(expectedHex)!);
    }

    [Fact]
    public void Convert_NonStringValue_ReturnsPendingBrush()
    {
        var result = _converter.Convert(42, typeof(Brush), "Background", CultureInfo.InvariantCulture);

        result.Should().BeOfType<SolidColorBrush>()
            .Which.Color.Should().Be((Color)ColorConverter.ConvertFromString("#FFEBEBEB")!);
    }

    [Fact]
    public void ConvertBack_Always_ThrowsNotImplementedException()
    {
        Action act = () => _converter.ConvertBack(Brushes.Green, typeof(string), null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotImplementedException>();
    }
}
