using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FinancialToolSupport
{
    public static class DecimalExtension
    {
        private static readonly Dictionary<string, CultureInfo> ISOCurrenciesToACultureMap =
            CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .Select(c => new { c, ISOCurrencySymbol=GetRegionISOCurrencySymbol(c.LCID) })
                .Where(o => o.ISOCurrencySymbol != null)
                .GroupBy(x => x.ISOCurrencySymbol)
                .ToDictionary(g => g.Key, g => g.First().c, StringComparer.OrdinalIgnoreCase);

        private static string GetRegionISOCurrencySymbol(int lcid)
        {
            try
            {
                return new RegionInfo(lcid).ISOCurrencySymbol;

            }
            catch { }
            return null;
        }

        public static string FormatCurrency(this decimal amount, string currencyCode)
        {
            CultureInfo culture;
            if (ISOCurrenciesToACultureMap.TryGetValue(currencyCode, out culture))
                return string.Format(culture, "{0:C}", amount);
            return amount.ToString("0.00");
        }
    }
}
