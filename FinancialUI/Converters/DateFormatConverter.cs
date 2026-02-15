using System.Globalization;
using System.Windows.Data;

namespace FinancialUI.Converters;

/// <summary>
/// Converts DateTime to localized date string
/// </summary>
public class DateFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            var format = parameter as string ?? "d"; // Short date by default
            return dateTime.ToString(format, culture);
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && DateTime.TryParse(str, culture, DateTimeStyles.None, out DateTime result))
        {
            return result;
        }
        return DateTime.MinValue;
    }
}
