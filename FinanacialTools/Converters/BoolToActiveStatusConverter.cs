using System;
using System.Globalization;
using System.Windows.Data;

namespace SharesDividendCheck.Converters
{
    /// <summary>
    /// Converts boolean IsActive value to user-friendly text: "Active" or "Inactive"
    /// </summary>
    public class BoolToActiveStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "Active" : "Inactive";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
