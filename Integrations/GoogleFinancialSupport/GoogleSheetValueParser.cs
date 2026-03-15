using System;
using Google.Apis.Sheets.v4.Data;

namespace Financial.Infrastructure.Integrations.GoogleFinancialSupport;

internal static class GoogleSheetValueParser
{
    internal static decimal ToDecimal(object toDecimal)
    {
        if (toDecimal is ExtendedValue extendedValue && extendedValue.NumberValue != null)
        {
            return (decimal)extendedValue.NumberValue;
        }

        var value = toDecimal.ToString().Replace(",", "");
        return decimal.Parse(value);
    }
}
