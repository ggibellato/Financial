using System;
using System.Globalization;
using System.Windows.Data;

namespace SharesDividendCheck.Converters;

/// <summary>
/// Converts decimal values to currency format strings
/// </summary>
public class CurrencyFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal amount)
        {
            // Parameter can optionally specify currency symbol
            var currencySymbol = parameter as string ?? string.Empty;
            
            if (string.IsNullOrEmpty(currencySymbol))
            {
                return amount.ToString("N2", culture);
            }
            
            return $"{currencySymbol} {amount:N2}";
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && decimal.TryParse(str.Replace(",", "").Replace(".", ""), 
            NumberStyles.Any, culture, out decimal result))
        {
            return result;
        }
        return 0m;
    }
}
