using System.ComponentModel;
using System.Globalization;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class SortDirectionToArrowConverterTests
{
    private readonly SortDirectionToArrowConverter _converter = new();

    [Fact]
    public void Convert_Ascending_ReturnsUpArrow()
    {
        _converter.Convert(ListSortDirection.Ascending, typeof(string), null, CultureInfo.InvariantCulture).Should().Be("▲");
    }

    [Fact]
    public void Convert_Descending_ReturnsDownArrow()
    {
        _converter.Convert(ListSortDirection.Descending, typeof(string), null, CultureInfo.InvariantCulture).Should().Be("▼");
    }

    [Fact]
    public void Convert_Null_ReturnsEmptyString()
    {
        _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture).Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertBack_Throws()
    {
        var act = () => _converter.ConvertBack("▲", typeof(ListSortDirection), null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotSupportedException>();
    }
}
