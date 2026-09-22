using System.Globalization;
using System.Windows.Data;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Builds the shares/cost/price detail tooltip text from a credit's attribution fields, matching
/// DividendYieldTooltip's content on the React side exactly. The yield percentages themselves are
/// shown as their own grid columns, not repeated here.
/// </summary>
public class DividendAttributionToTooltipConverter : IMultiValueConverter
{
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [decimal attributedShares, _, _, _, _, DateTime date, ..])
        {
            return string.Empty;
        }

        var isExplicit = values is [_, _, _, _, _, _, decimal];
        var lines = new List<string>
        {
            isExplicit
                ? $"{attributedShares} shares attributed"
                : $"{attributedShares} shares attributed (entire position - none specified)"
        };

        if (values[1] is decimal averageCostPerShare)
        {
            lines.Add($"Average cost/share: {averageCostPerShare:N2}");
        }

        if (values[2] is decimal invested)
        {
            lines.Add($"Total bought: {invested:N2}");
        }

        if (values[3] is decimal priceOnDate)
        {
            lines.Add($"Share price on {date:d}: {priceOnDate:N2}");
        }

        if (values[4] is decimal marketValue)
        {
            lines.Add($"Total current value: {marketValue:N2}");
        }

        return string.Join("\n", lines);
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
