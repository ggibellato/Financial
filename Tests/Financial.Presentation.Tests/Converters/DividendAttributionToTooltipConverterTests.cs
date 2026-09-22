using System.Globalization;
using Financial.Presentation.App.Converters;
using FluentAssertions;

namespace Financial.Presentation.Tests.Converters;

public class DividendAttributionToTooltipConverterTests
{
    private readonly DividendAttributionToTooltipConverter _converter = new();

    [Fact]
    public void Convert_ExplicitSharesForDividend_LabelsThemAsAttributed()
    {
        var date = new DateTime(2024, 6, 1);

        var result = _converter.Convert(
            [800m, 9m, 7200m, 10m, 8000m, date, 800m],
            typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(
            "800 shares attributed\n" +
            "Average cost/share: 9.00\n" +
            "Total bought: 7,200.00\n" +
            $"Share price on {date:d}: 10.00\n" +
            "Total current value: 8,000.00");
    }

    [Fact]
    public void Convert_NoSharesForDividendEntered_LabelsAttributionAsEntirePosition()
    {
        var date = new DateTime(2024, 6, 1);

        var result = _converter.Convert(
            [1000m, 9m, 9000m, null, null, date, null],
            typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(
            "1000 shares attributed (entire position - none specified)\n" +
            "Average cost/share: 9.00\n" +
            "Total bought: 9,000.00");
    }

    [Fact]
    public void Convert_NoAttributedShares_ReturnsEmpty()
    {
        var result = _converter.Convert(
            [null, null, null, null, null, new DateTime(2024, 6, 1), null],
            typeof(string), null, CultureInfo.InvariantCulture);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupported()
    {
        var act = () => _converter.ConvertBack(
            "value",
            [typeof(decimal), typeof(decimal), typeof(decimal), typeof(decimal), typeof(decimal), typeof(DateTime), typeof(decimal)],
            null, CultureInfo.InvariantCulture);

        act.Should().Throw<NotSupportedException>();
    }
}
