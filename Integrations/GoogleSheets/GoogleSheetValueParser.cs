using Google.Apis.Sheets.v4.Data;

namespace Financial.Integrations.GoogleSheets;

public static class GoogleSheetValueParser
{
    public static decimal ToDecimal(object rawCellValue)
    {
        if (rawCellValue is ExtendedValue extendedValue && extendedValue.NumberValue != null)
        {
            return (decimal)extendedValue.NumberValue;
        }

        var value = rawCellValue.ToString().Replace(",", "");
        return decimal.Parse(value);
    }
}
