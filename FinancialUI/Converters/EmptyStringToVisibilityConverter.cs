using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FinancialUI.Converters;

/// <summary>
/// Converts empty string to Visibility (empty = Visible, not empty = Collapsed)
/// Used to show "empty state" messages
/// </summary>
public class EmptyStringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return string.IsNullOrEmpty(str) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
