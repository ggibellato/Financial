using System.Globalization;
using ClosedXML.Excel;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;

/// <summary>
/// Reads a numeric cell, tolerating a value that was entered as text using a comma decimal
/// separator (e.g. "17,28") instead of a genuine Excel number. Confirmed present in the real
/// workbook (Mar2017, cell D82): calling <c>cell.GetValue&lt;double&gt;()</c> directly on that cell
/// silently misreads it as 1728 rather than 17.28, because the runtime culture treats the comma as
/// a thousands separator - a 100x amplification of a single row's value.
/// </summary>
public static class NumericCellReader
{
    public static decimal? TryRead(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType == XLDataType.Number)
        {
            return (decimal)cell.GetValue<double>();
        }

        var text = cell.GetString().Trim();
        if (text.Length == 0)
        {
            return null;
        }

        var normalized = text.Contains(',') && !text.Contains('.')
            ? text.Replace(',', '.')
            : text;

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }
}
