using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2013.WebExtension;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Parsing;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Reporting;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.SheetImporters;

/// <summary>
/// Reads a "Resumo{Year}" annual summary sheet. Account-row positions are NOT stable across years
/// (confirmed empirically: the canonical 2026-shaped sheet has all 11 accounts at rows 29-39, but
/// older years use different row counts, different labels — e.g. "Help to Buy ISA GGS" instead of
/// a Chip Cash ISA — and sometimes omit an account entirely), so rows are located by matching each
/// row's own label text against the registry's known account aliases rather than by fixed row
/// number. The registry (see <see cref="InvestmentAccountMigrator"/>) must already be seeded before
/// this runs. Rows whose label matches no known alias are simply not written — no fabricated
/// snapshot is ever created. For a matched row, every one of the 12 months gets a snapshot: a
/// genuinely blank cell becomes an explicit 0 (so an account's existence in a year is derivable
/// purely from snapshot presence afterward), while a non-blank cell that fails to parse is flagged
/// on the report and left without a snapshot for that month. This is also the only place
/// <see cref="InvestmentSnapshot"/> data is sourced from, so it both validates and writes.
/// </summary>
public static class ResumoValidationReader
{
    private const int LabelColumn = 1;
    private const int FirstMonthColumn = 2;
    private const int MonthCount = 12;
    private const int LabelScanLastRow = 60;

    public static IReadOnlyList<InvestmentSnapshot> ImportAccountSnapshots(
        IXLWorksheet sheet,
        int year,
        IReadOnlyCollection<InvestmentAccount> accounts,
        ImportReport report)
    {
        var snapshots = new List<InvestmentSnapshot>();
        var lastRow = Math.Min(sheet.LastRowUsed()?.RowNumber() ?? 1, LabelScanLastRow);

        for (var row = 1; row <= lastRow; row++)
        {
            var label = sheet.Cell(row, LabelColumn).GetString();
            if (string.IsNullOrWhiteSpace(label) || !TryResolveAccount(label, accounts, out var account))
            {
                continue;
            }

            for (var i = 0; i < MonthCount; i++)
            {
                var cell = sheet.Cell(row, FirstMonthColumn + i);
                var month = i + 1;

                var rawValue = NumericCellReader.TryRead(cell);
                if (rawValue is not null)
                {
                    var adjustedValue = rawValue.Value * (account!.IsLiability ? -1 : 1);
                    snapshots.Add(InvestmentSnapshot.Create(account, year, month, adjustedValue));
                    continue;
                }

                // TryRead returns null for both a genuinely blank cell and a non-blank cell whose
                // text isn't a valid number (e.g. a formula that evaluated to ""), so re-check the
                // cell's own emptiness here rather than trusting TryRead's null to mean "malformed".
                if (cell.IsEmpty() || cell.GetString().Trim().Length == 0)
                {
                    snapshots.Add(InvestmentSnapshot.Create(account!, year, month, 0m));
                    continue;
                }

                report.RowFlagged(
                    sheet.Name,
                    row,
                    $"{account!.Name} Month{month}",
                    cell.GetString(),
                    "Value could not be parsed as a number - snapshot not written for this account/month");
            }
        }

        return snapshots;
    }

    public static IReadOnlyDictionary<int, decimal>? ReadAnnualExpenseTotals(IXLWorksheet sheet)
    {
        var lastRow = Math.Min(sheet.LastRowUsed()?.RowNumber() ?? 1, LabelScanLastRow);

        for (var row = 1; row <= lastRow; row++)
        {
            var label = sheet.Cell(row, LabelColumn).GetString();
            if (!IsTotalDespesasLabel(label))
            {
                continue;
            }

            var totals = new Dictionary<int, decimal>();
            for (var i = 0; i < MonthCount; i++)
            {
                var rawValue = NumericCellReader.TryRead(sheet.Cell(row, FirstMonthColumn + i));
                if (rawValue is not null)
                {
                    totals[i + 1] = rawValue.Value;
                }
            }

            return totals;
        }

        return null;
    }

    private static bool TryResolveAccount(string rawLabel, IReadOnlyCollection<InvestmentAccount> accounts, out InvestmentAccount? account)
    {
        var normalized = NormalizeLabel(rawLabel);
        account = accounts.FirstOrDefault(a =>
            a.Aliases.Any(alias => string.Equals(NormalizeLabel(alias), normalized, StringComparison.OrdinalIgnoreCase)));

        return account is not null;
    }

    private static string NormalizeLabel(string label)
    {
        var withoutSignMarker = label.Replace("(-)", string.Empty, StringComparison.OrdinalIgnoreCase);
        var collapsed = string.Join(' ', withoutSignMarker.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return collapsed.Trim();
    }

    private static bool IsTotalDespesasLabel(string label)
    {
        var trimmed = label.Trim();
        return string.Equals(trimmed, "Total despesas", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "Total", StringComparison.OrdinalIgnoreCase);
    }
}
