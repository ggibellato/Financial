using System.Globalization;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class EqualityToBoolConverterTests
{
    private readonly EqualityToBoolConverter _converter = new();

    [Fact]
    public void Convert_TwoEqualValues_ReturnsTrue()
    {
        _converter.Convert(["a", "a"], typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(true);
    }

    [Fact]
    public void Convert_TwoUnequalValues_ReturnsFalse()
    {
        _converter.Convert(["a", "b"], typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
    }

    [Fact]
    public void Convert_NotExactlyTwoValues_ReturnsFalse()
    {
        _converter.Convert(["a"], typeof(bool), null, CultureInfo.InvariantCulture).Should().Be(false);
    }

    [Fact]
    public void ConvertBack_Throws()
    {
        var act = () => _converter.ConvertBack(true, [typeof(string), typeof(string)], null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotSupportedException>();
    }
}
