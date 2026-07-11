using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Converts boolean active status to a brush: green for active, red for inactive.
/// </summary>
public class BoolToActiveColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? Brushes.Green : Brushes.Red;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
