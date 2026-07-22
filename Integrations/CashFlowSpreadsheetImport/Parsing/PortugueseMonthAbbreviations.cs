namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;

/// <summary>
/// Maps the 3-letter Portuguese month abbreviations used throughout the source spreadsheet (sheet
/// names, free-text dates) to their calendar month number, plus the one full-spelled variant
/// ("Maio") confirmed present in the real "Controle mae" sheet.
/// </summary>
public static class PortugueseMonthAbbreviations
{
    public static readonly IReadOnlyDictionary<string, int> Map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["Jan"] = 1,
        ["Fev"] = 2,
        ["Mar"] = 3,
        ["Abr"] = 4,
        ["Mai"] = 5,
        ["Maio"] = 5,
        ["Jun"] = 6,
        ["Jul"] = 7,
        ["Ago"] = 8,
        ["Set"] = 9,
        ["Out"] = 10,
        ["Nov"] = 11,
        ["Dez"] = 12,
    };
}
