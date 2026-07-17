using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Converts a PositionType string (Long/Flat/Short) to a brush: green/black/red.
/// </summary>
public class PositionTypeToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string positionType)
        {
            return positionType switch
            {
                "Long" => Brushes.Green,
                "Short" => Brushes.Red,
                _ => Brushes.Black
            };
        }

        return Brushes.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
