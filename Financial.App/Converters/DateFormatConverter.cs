using System.Globalization;
using System.Windows.Data;
using Financial.Presentation.App.Helpers;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Converts DateTime to localized date string
/// </summary>
public class DateFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            var effectiveCulture = CultureInfo.CurrentCulture;
            var format = parameter as string;
            if (string.IsNullOrWhiteSpace(format) || string.Equals(format, "d", StringComparison.OrdinalIgnoreCase))
            {
                format = DateFormatHelper.GetPaddedShortDatePattern();
            }
            return dateTime.ToString(format, effectiveCulture);
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var effectiveCulture = CultureInfo.CurrentCulture;
        if (value is string str && DateTime.TryParse(str, effectiveCulture, DateTimeStyles.None, out DateTime result))
        {
            return result;
        }
        return DateTime.MinValue;
    }
}

