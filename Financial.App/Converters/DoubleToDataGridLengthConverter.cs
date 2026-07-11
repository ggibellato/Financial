using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Converts a pixel-width double resource into a DataGridLength for DataGridColumn.Width,
/// which does not accept a raw double via StaticResource.
/// </summary>
public class DoubleToDataGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is double pixels ? new DataGridLength(pixels) : DataGridLength.Auto;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
