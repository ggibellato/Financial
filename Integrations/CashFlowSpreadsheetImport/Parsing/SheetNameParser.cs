using System.Text.RegularExpressions;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;

/// <summary>
/// Recognizes the abbreviated "MonYYYY" monthly-tab naming convention (e.g. "Jul2026", "Fev2017")
/// used from February 2017 onward. Older full-Portuguese-month-name tabs (e.g. "Janeiro 2017",
/// "Julho 2014") never match this pattern and are therefore never selected for import.
/// </summary>
public static partial class SheetNameParser
{
    public const int FirstInScopeYear = 2017;
    public const int LastInScopeYear = 2026;

    private static readonly Regex MonthlySheetNamePattern = BuildMonthlySheetNamePattern();

    public static bool TryParseMonthlySheetName(string sheetName, out int year, out int month)
    {
        year = 0;
        month = 0;

        var match = MonthlySheetNamePattern.Match(sheetName);
        if (!match.Success)
        {
            return false;
        }

        if (!PortugueseMonthAbbreviations.Map.TryGetValue(match.Groups["month"].Value, out month))
        {
            return false;
        }

        year = int.Parse(match.Groups["year"].Value);
        return true;
    }

    public static bool IsInScope(int year, int month) =>
        year <= LastInScopeYear && (year > FirstInScopeYear || (year == FirstInScopeYear && month >= 2));

    [GeneratedRegex(@"^(?<month>Jan|Fev|Mar|Abr|Mai|Jun|Jul|Ago|Set|Out|Nov|Dez)(?<year>\d{4})$", RegexOptions.IgnoreCase)]
    private static partial Regex BuildMonthlySheetNamePattern();
}
