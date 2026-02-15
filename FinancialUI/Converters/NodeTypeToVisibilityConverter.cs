using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FinancialUI.Converters;

/// <summary>
/// Shows icon only for Asset node types
/// </summary>
public class NodeTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string nodeType && nodeType == "Asset")
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
