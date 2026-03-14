using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Financial.Presentation.Tools.Converters;

public class SignedValueToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal dec)
        {
            return dec >= 0 ? Brushes.Green : Brushes.Red;
        }

        if (value is double dbl)
        {
            return dbl >= 0 ? Brushes.Green : Brushes.Red;
        }

        return Brushes.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
