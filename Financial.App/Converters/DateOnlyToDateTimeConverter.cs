using System.Globalization;
using System.Windows.Data;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Converts DateOnly? to DateTime? for binding a DatePicker directly to a DateOnly? field.
/// </summary>
public class DateOnlyToDateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateOnly dateOnly ? dateOnly.ToDateTime(TimeOnly.MinValue) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTime dateTime ? DateOnly.FromDateTime(dateTime) : null;
}
