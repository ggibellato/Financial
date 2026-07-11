using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Converts a pixel-width double resource into a GridLength for Grid.ColumnDefinition.Width,
/// which does not accept a raw double via StaticResource.
/// </summary>
public class DoubleToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is double pixels ? new GridLength(pixels) : GridLength.Auto;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
