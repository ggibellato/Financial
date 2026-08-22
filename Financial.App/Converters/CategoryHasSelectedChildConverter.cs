using System.Globalization;
using System.Windows.Data;
using Financial.Presentation.App.Navigation;

namespace Financial.Presentation.App.Converters;

public class CategoryHasSelectedChildConverter : IMultiValueConverter
{
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length != 2 || values[0] is not IReadOnlyList<NavChild> children || values[1] is not string selectedChildId)
        {
            return false;
        }

        return children.Any(c => c.Id == selectedChildId);
    }

    public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
