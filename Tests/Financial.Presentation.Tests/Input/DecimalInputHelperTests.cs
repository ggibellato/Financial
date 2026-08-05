using Financial.Presentation.App.Input;
using FluentAssertions;

namespace Financial.Presentation.Tests.Input;

public class DecimalInputHelperTests
{
    [Theory]
    [InlineData("")]
    [InlineData("10")]
    [InlineData("10.5")]
    [InlineData("10,5")]
    public void IsValidDecimalInput_UnsignedText_ReturnsTrue(string text)
    {
        DecimalInputHelper.IsValidDecimalInput(text).Should().BeTrue();
    }

    [Theory]
    [InlineData("-")]
    [InlineData("-10")]
    [InlineData("-10.5")]
    public void IsValidDecimalInput_NegativeText_ReturnsFalse(string text)
    {
        DecimalInputHelper.IsValidDecimalInput(text).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("-")]
    [InlineData("10")]
    [InlineData("10.5")]
    [InlineData("-10")]
    [InlineData("-10.5")]
    [InlineData("-10,5")]
    public void IsValidSignedDecimalInput_LeadingMinusOrNone_ReturnsTrue(string text)
    {
        DecimalInputHelper.IsValidSignedDecimalInput(text).Should().BeTrue();
    }

    [Theory]
    [InlineData("1-0")]
    [InlineData("10-")]
    [InlineData("--10")]
    [InlineData("10.5.5")]
    public void IsValidSignedDecimalInput_MisplacedOrRepeatedSigns_ReturnsFalse(string text)
    {
        DecimalInputHelper.IsValidSignedDecimalInput(text).Should().BeFalse();
    }
}
