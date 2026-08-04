using System.Globalization;
using System.Windows.Data;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Compares two bound values for equality, e.g. a nav item's id against the currently
/// selected id, to drive a Tag-bound DataTrigger for active-item highlighting.
/// </summary>
public class EqualityToBoolConverter : IMultiValueConverter
{
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        return values.Length == 2 && Equals(values[0], values[1]);
    }

    public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
