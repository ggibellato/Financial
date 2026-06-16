using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Converts transaction type (Buy/Sell) to color brush
/// </summary>
public class TransactionTypeToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string transactionType)
        {
            return transactionType.Equals("Buy", StringComparison.OrdinalIgnoreCase)
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
