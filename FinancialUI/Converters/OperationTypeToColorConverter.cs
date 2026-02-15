using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FinancialUI.Converters;

/// <summary>
/// Converts operation type (Buy/Sell) to color brush
/// </summary>
public class OperationTypeToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string operationType)
        {
            return operationType.Equals("Buy", StringComparison.OrdinalIgnoreCase)
                ? Brushes.Green
                : Brushes.Red;
        }
        return Brushes.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
