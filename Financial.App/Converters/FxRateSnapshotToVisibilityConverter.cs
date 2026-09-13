using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Financial.Investment.Application.DTOs;

namespace Financial.Presentation.App.Converters;

public class FxRateSnapshotToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is FxRateSnapshotDTO ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
