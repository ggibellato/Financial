using System.Globalization;
using System.Windows.Data;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Bridges an int ViewModel property (e.g. a year) to Wpf.Ui's NumberBox, whose Value is a
/// nullable double. A null Value (empty box) converts back to 0 rather than leaving the
/// source unset, since the bound properties this is used with are non-nullable int.
/// </summary>
public class IntToNullableDoubleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int intValue ? (double)intValue : null!;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is double doubleValue ? (int)doubleValue : 0;
    }
}
