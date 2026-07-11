using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Converts a pixel-width double resource into a DataGridLength for DataGridColumn.Width,
/// which does not accept a raw double via StaticResource. Supports TwoWay so resizing a
/// column (which sets Width to a new pixel-mode DataGridLength) writes the new pixel value
/// back to the shared source, keeping any other bound consumer (e.g. a group-header overlay)
/// in sync.
/// </summary>
public class DoubleToDataGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is double pixels ? new DataGridLength(pixels) : DataGridLength.Auto;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is DataGridLength length ? length.Value : Binding.DoNothing;
    }
}
