using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace Financial.Presentation.App.Converters;

public class SortDirectionToArrowConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            ListSortDirection.Ascending => "▲",
            ListSortDirection.Descending => "▼",
            _ => string.Empty,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
