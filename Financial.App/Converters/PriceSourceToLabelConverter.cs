using System.Globalization;
using System.Windows.Data;
using Financial.Investment.Domain.Entities;

namespace Financial.Presentation.App.Converters;

/// <summary>Mirrors Financial.Web's PriceHistoryTab SOURCE_LABELS map, so both front ends show the same wording.</summary>
public class PriceSourceToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is PriceSource source
            ? source switch
            {
                PriceSource.Unknown => "Unknown",
                PriceSource.Manual => "Manual",
                PriceSource.ProviderValuation => "Provider Valuation",
                PriceSource.Google => "Google",
                PriceSource.Yahoo => "Yahoo",
                PriceSource.StatusInvest => "StatusInvest",
                PriceSource.DicionarioDoInvestidor => "Dicionario do Investidor",
                PriceSource.Redentia => "Redentia",
                _ => source.ToString()
            }
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
