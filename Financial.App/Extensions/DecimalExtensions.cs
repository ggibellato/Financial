using System.Globalization;
using System.Linq;

namespace Financial.Presentation.App.Extensions;

public static class DecimalExtensions
{
    private static readonly Dictionary<string, CultureInfo> CurrencyToCultureMap =
        CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(c => new { c, Iso = GetISOCurrencySymbol(c.LCID) })
            .Where(o => o.Iso != null)
            .GroupBy(x => x.Iso)
            .ToDictionary(g => g.Key!, g => g.First().c, StringComparer.OrdinalIgnoreCase);

    public static string FormatCurrency(this decimal amount, string currencyCode)
    {
        if (CurrencyToCultureMap.TryGetValue(currencyCode, out var culture))
            return string.Format(culture, "{0:C}", amount);
        return amount.ToString("0.00");
    }

    private static string? GetISOCurrencySymbol(int lcid)
    {
        try { return new RegionInfo(lcid).ISOCurrencySymbol; }
        catch { return null; }
    }
}
