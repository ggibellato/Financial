using System.Globalization;
using Financial.Presentation.App.Converters;
using Financial.Presentation.App.Navigation;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class GroupHasSelectedChildConverterTests
{
    private readonly GroupHasSelectedChildConverter _converter = new();

    private static readonly IReadOnlyList<NavChild> Children =
    [
        new NavChild("admin-brokers", "Brokers", "admin-brokers"),
        new NavChild("admin-portfolios", "Portfolios", "admin-portfolios"),
    ];

    [Fact]
    public void Convert_SelectedChildMatchesByViewKey_ReturnsTrue()
    {
        var result = _converter.Convert([Children, "admin-portfolios"], typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void Convert_NoMatch_ReturnsFalse()
    {
        var result = _converter.Convert([Children, "admin-banks"], typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(false);
    }

    [Fact]
    public void Convert_UnexpectedValueTypes_ReturnsFalse()
    {
        var result = _converter.Convert(["not-a-list", "admin-brokers"], typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(false);
    }

    [Fact]
    public void ConvertBack_Always_ThrowsNotSupportedException()
    {
        Action act = () => _converter.ConvertBack(true, [typeof(IReadOnlyList<NavChild>), typeof(string)], null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotSupportedException>();
    }
}
