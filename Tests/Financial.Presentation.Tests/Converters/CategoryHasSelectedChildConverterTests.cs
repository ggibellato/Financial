using System.Globalization;
using Financial.Presentation.App.Converters;
using Financial.Presentation.App.Navigation;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class CategoryHasSelectedChildConverterTests
{
    private readonly CategoryHasSelectedChildConverter _converter = new();

    private static readonly NavCategory FlatCategory = new(
        "cashflow", "CashFlow", "icon",
        [new NavChild("monthly", "Monthly", "monthly")]);

    private static readonly NavCategory GroupedCategory = new(
        "admin", "Admin", "icon", [],
        Groups: [new NavGroup("investment", "Investment", [new NavChild("admin-brokers", "Brokers", "admin-brokers")])]);

    [Fact]
    public void Convert_FlatCategory_SelectedChildMatchesByViewKey_ReturnsTrue()
    {
        var result = _converter.Convert([FlatCategory, "monthly"], typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void Convert_GroupedCategory_SelectedChildInsideGroup_ReturnsTrue()
    {
        var result = _converter.Convert([GroupedCategory, "admin-brokers"], typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void Convert_NoMatch_ReturnsFalse()
    {
        var result = _converter.Convert([FlatCategory, "unrelated"], typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(false);
    }

    [Fact]
    public void Convert_UnexpectedValueTypes_ReturnsFalse()
    {
        var result = _converter.Convert(["not-a-category", "monthly"], typeof(bool), null, CultureInfo.InvariantCulture);

        result.Should().Be(false);
    }

    [Fact]
    public void ConvertBack_Always_ThrowsNotSupportedException()
    {
        Action act = () => _converter.ConvertBack(true, [typeof(NavCategory), typeof(string)], null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotSupportedException>();
    }
}
