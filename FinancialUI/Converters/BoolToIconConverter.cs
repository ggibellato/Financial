using System.Globalization;
using System.Windows.Data;

namespace FinancialUI.Converters;

/// <summary>
/// Converts boolean active status to icon symbol (● for active, ○ for inactive)
/// </summary>
public class BoolToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive ? "●" : "○";
        }
        return "○";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
