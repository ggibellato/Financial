using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Converts the sidebar's IsCollapsed flag into its column width: 56px collapsed, 240px expanded.
/// </summary>
public class BoolToSidebarWidthConverter : IValueConverter
{
    private const double CollapsedWidth = 56;
    private const double ExpandedWidth = 240;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isCollapsed = value is bool collapsed && collapsed;
        return new GridLength(isCollapsed ? CollapsedWidth : ExpandedWidth);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
