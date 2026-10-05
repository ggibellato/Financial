using System.Globalization;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

[Trait("Category", "Unit")]
public class BetweenTextSeparatorConverterTests
{
    private readonly BetweenTextSeparatorConverter _converter = new();

    [Fact]
    public void Convert_BothValuesPresent_ReturnsDefaultSeparator()
    {
        var result = _converter.Convert(["Left", "Right"], typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(" · ");
    }

    [Fact]
    public void Convert_BothValuesPresent_WithCustomSeparatorParameter_ReturnsCustomSeparator()
    {
        var result = _converter.Convert(["Left", "Right"], typeof(string), " - ", CultureInfo.InvariantCulture);

        result.Should().Be(" - ");
    }

    [Fact]
    public void Convert_FewerThanTwoValues_ReturnsEmptyString()
    {
        var result = _converter.Convert(["OnlyOne"], typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Convert_LeftValueIsWhitespace_ReturnsEmptyString()
    {
        var result = _converter.Convert(["   ", "Right"], typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Convert_RightValueIsNull_ReturnsEmptyString()
    {
        var result = _converter.Convert(["Left", null], typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(string.Empty);
    }
}
