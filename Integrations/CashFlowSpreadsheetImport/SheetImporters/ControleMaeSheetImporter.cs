using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Reporting;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;

/// <summary>
/// Parses the single continuous "Controle mae" sheet into <see cref="MaeLedgerEntry"/> entities.
/// The sheet has no dedicated date column — column A is free text that usually embeds a date or
/// month/year — so a row's date is extracted from that text with regex, and rows where none can
/// be confidently found are skipped and reported rather than assigned a fabricated date. Both
/// currency values are preserved exactly as recorded (no live FX call is ever made here).
/// </summary>
public static partial class ControleMaeSheetImporter
{
    private const int DescriptionColumn = 1;
    private const int BrlValueColumn = 2;
    private const int GbpValueColumn = 3;
    private const int NoteColumn = 5;

    private static readonly Dictionary<string, int> MonthAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Jan"] = 1,
        ["Fev"] = 2,
        ["Mar"] = 3,
        ["Abr"] = 4,
        ["Mai"] = 5,
        ["Jun"] = 6,
        ["Jul"] = 7,
        ["Ago"] = 8,
        ["Set"] = 9,
        ["Out"] = 10,
        ["Nov"] = 11,
        ["Dez"] = 12,
    };

    private static readonly Regex FullDatePattern = BuildFullDatePattern();
    private static readonly Regex MonthYearPattern = BuildMonthYearPattern();
    private static readonly Regex YearMonthPattern = BuildYearMonthPattern();

    public static IReadOnlyList<MaeLedgerEntry> Import(IXLWorksheet sheet, ImportReport report)
    {
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var entries = new List<MaeLedgerEntry>();

        for (var row = 1; row <= lastRow; row++)
        {
            var description = sheet.Cell(row, DescriptionColumn).GetString().Trim();
            if (string.IsNullOrWhiteSpace(description) || IsSeparatorLine(description))
            {
                continue;
            }

            var brlValue = TryGetNumeric(sheet.Cell(row, BrlValueColumn));
            var gbpValue = TryGetNumeric(sheet.Cell(row, GbpValueColumn));
            if (brlValue is null && gbpValue is null)
            {
                continue;
            }

            if (!TryExtractDate(description, out var date))
            {
                report.RowFlagged(sheet.Name, row, "Description", description, "No confidently-extractable date - entry not imported");
                continue;
            }

            var note = sheet.Cell(row, NoteColumn).GetString();
            var sourceCurrency = brlValue is not null ? Currency.BRL : Currency.GBP;

            entries.Add(MaeLedgerEntry.Create(date, description, note, sourceCurrency, brlValue, gbpValue));
        }

        return entries;
    }

    private static bool IsSeparatorLine(string description) => description.All(c => c is '-' or ' ');

    private static decimal? TryGetNumeric(IXLCell cell) =>
        !cell.IsEmpty() && cell.TryGetValue<double>(out var value) ? (decimal)value : null;

    private static bool TryExtractDate(string description, out DateOnly date)
    {
        var fullMatch = FullDatePattern.Match(description);
        if (fullMatch.Success)
        {
            var day = int.Parse(fullMatch.Groups["day"].Value);
            var month = int.Parse(fullMatch.Groups["month"].Value);
            var year = int.Parse(fullMatch.Groups["year"].Value);
            if (month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(year, month))
            {
                date = new DateOnly(year, month, day);
                return true;
            }
        }

        var monthYearMatch = MonthYearPattern.Match(description);
        if (monthYearMatch.Success && MonthAbbreviations.TryGetValue(monthYearMatch.Groups["month"].Value, out var monthNumber))
        {
            date = new DateOnly(int.Parse(monthYearMatch.Groups["year"].Value), monthNumber, 1);
            return true;
        }

        var yearMonthMatch = YearMonthPattern.Match(description);
        if (yearMonthMatch.Success)
        {
            var year = int.Parse(yearMonthMatch.Groups["year"].Value);
            var month = int.Parse(yearMonthMatch.Groups["month"].Value);
            if (month is >= 1 and <= 12)
            {
                date = new DateOnly(year, month, 1);
                return true;
            }
        }

        date = default;
        return false;
    }

    [GeneratedRegex(@"(?<day>\d{1,2})/(?<month>\d{1,2})/(?<year>\d{4})")]
    private static partial Regex BuildFullDatePattern();

    [GeneratedRegex(@"(?<month>Jan|Fev|Mar|Abr|Mai|Jun|Jul|Ago|Set|Out|Nov|Dez)/(?<year>\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex BuildMonthYearPattern();

    [GeneratedRegex(@"(?<year>\d{4})/(?<month>\d{1,2})\b")]
    private static partial Regex BuildYearMonthPattern();
}
