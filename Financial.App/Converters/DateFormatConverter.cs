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
        if (value is DateOnly dateOnly)
        {
            return FormatDate(dateOnly.ToDateTime(TimeOnly.MinValue), parameter);
        }
        if (value is DateTime dateTime)
        {
            return FormatDate(dateTime, parameter);
        }
        return string.Empty;
    }

    private static string FormatDate(DateTime dateTime, object? parameter)
    {
        var effectiveCulture = CultureInfo.CurrentCulture;
        var format = parameter as string;
        if (string.IsNullOrWhiteSpace(format) || string.Equals(format, "d", StringComparison.OrdinalIgnoreCase))
        {
            format = DateFormatHelper.GetPaddedShortDatePattern();
        }
        return dateTime.ToString(format, effectiveCulture);
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

