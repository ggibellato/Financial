using System;
using System.Globalization;
using System.Windows.Data;

namespace Financial.Presentation.Tools.Converters;

public class BetweenTextSeparatorConverter : IMultiValueConverter
{
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return string.Empty;
        }

        var left = values[0]?.ToString();
        var right = values[1]?.ToString();

        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return string.Empty;
        }

        return parameter as string ?? " · ";
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
