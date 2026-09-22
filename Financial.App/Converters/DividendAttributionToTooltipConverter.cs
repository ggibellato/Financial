using System.Globalization;
using System.Windows.Data;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Builds the shares/invested/market-value/yield tooltip text from a credit's attribution
/// fields, matching DividendYieldTooltip's content on the React side exactly.
/// </summary>
public class DividendAttributionToTooltipConverter : IMultiValueConverter
{
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [decimal sharesForDividend, _, _, _, _])
        {
            return string.Empty;
        }

        var lines = new List<string> { $"{sharesForDividend} shares attributed" };

        if (values[1] is decimal invested)
        {
            lines.Add($"Invested: {invested:N2}");
        }

        if (values[2] is decimal marketValue)
        {
            lines.Add($"Market value on date: {marketValue:N2}");
        }

        if (values[3] is decimal onInvested)
        {
            lines.Add($"Yield on invested: {onInvested:N1}%");
        }

        if (values[4] is decimal onMarket)
        {
            lines.Add($"Yield on market: {onMarket:N1}%");
        }

        return string.Join("\n", lines);
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
