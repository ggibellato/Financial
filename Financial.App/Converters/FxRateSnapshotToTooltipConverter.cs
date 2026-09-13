using System.Globalization;
using System.Windows.Data;
using Financial.Investment.Application.DTOs;

namespace Financial.Presentation.App.Converters;

/// <summary>
/// Builds the three-line provenance text ("1 {currency} = {rate} {toCurrency}", "Source: ...",
/// "Retrieved: ...") from a record's own Currency and (possibly absent) FxRateSnapshot, matching
/// FxProvenanceTooltip's content on the React side exactly.
/// </summary>
public class FxRateSnapshotToTooltipConverter : IMultiValueConverter
{
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [string currency, FxRateSnapshotDTO snapshot])
        {
            return string.Empty;
        }

        return $"1 {currency} = {snapshot.Rate} {snapshot.ToCurrency}\n" +
               $"Source: {snapshot.Source}\n" +
               $"Retrieved: {snapshot.RetrievedAt.LocalDateTime:dd/MM/yyyy HH:mm}";
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
