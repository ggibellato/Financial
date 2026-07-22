using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;

/// <summary>
/// Reads a "Resumo{Year}" yearly summary sheet. Account-row positions are NOT stable across years
/// (confirmed empirically: the canonical 2026-shaped sheet has all 11 accounts at rows 29-39, but
/// older years use different row counts, different labels — e.g. "Help to Buy ISA GGS" instead of
/// a Chip Cash ISA — and sometimes omit an account entirely), so rows are located by matching each
/// row's own label text against the canonical account names rather than by fixed row number. Rows
/// whose label doesn't match a known account (including every genuinely historical-only account
/// name) are simply not written — no fabricated snapshot is ever created. This is also the only
/// place <see cref="InvestmentSnapshot"/> data is sourced from, so it both validates and writes.
/// </summary>
public static class ResumoValidationReader
{
    private const int LabelColumn = 1;
    private const int FirstMonthColumn = 2;
    private const int MonthCount = 12;
    private const int LabelScanLastRow = 60;

    private static readonly Dictionary<InvestmentAccount, string[]> AccountLabelAliases = new()
    {
        [InvestmentAccount.BlueRewardsSaver] = ["Blue Rewards Saver", "Barclays Blue Rewards"],
        [InvestmentAccount.PlatinumVisa8003] = ["Platinum Visa 8003"],
        [InvestmentAccount.PlatinumVisa6007] = ["Platinum Visa 6007"],
        [InvestmentAccount.ChaseMaster4023] = ["Chase Master 4023"],
        [InvestmentAccount.BaAmex] = ["BA Amex"],
        [InvestmentAccount.PaypalCredit] = ["Paypal credit"],
        [InvestmentAccount.ChipCashIsaGleison] = ["Chip Cash ISA Gleison"],
        [InvestmentAccount.ChaseSave] = ["Chase save"],
        [InvestmentAccount.ChipCashIsaAriana] = ["Chip Cash ISA Ariana"],
        [InvestmentAccount.Trading212Invested] = ["Trading 212 Invested"],
        [InvestmentAccount.ReservasPessoais] = ["Reservas pessoais"],
    };

    public static IReadOnlyList<InvestmentSnapshot> ImportAccountSnapshots(IXLWorksheet sheet, int year)
    {
        var snapshots = new List<InvestmentSnapshot>();
        var lastRow = Math.Min(sheet.LastRowUsed()?.RowNumber() ?? 1, LabelScanLastRow);

        for (var row = 1; row <= lastRow; row++)
        {
            var label = sheet.Cell(row, LabelColumn).GetString();
            if (string.IsNullOrWhiteSpace(label) || !TryResolveAccount(label, out var account))
            {
                continue;
            }

            for (var i = 0; i < MonthCount; i++)
            {
                var cell = sheet.Cell(row, FirstMonthColumn + i);
                if (cell.IsEmpty() || !cell.TryGetValue<double>(out var rawValue))
                {
                    continue;
                }

                snapshots.Add(InvestmentSnapshot.Create(account, year, i + 1, Math.Abs((decimal)rawValue)));
            }
        }

        return snapshots;
    }

    public static IReadOnlyDictionary<int, decimal>? ReadYearlyExpenseTotals(IXLWorksheet sheet)
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
                var cell = sheet.Cell(row, FirstMonthColumn + i);
                if (!cell.IsEmpty() && cell.TryGetValue<double>(out var rawValue))
                {
                    totals[i + 1] = (decimal)rawValue;
                }
            }

            return totals;
        }

        return null;
    }

    private static bool TryResolveAccount(string rawLabel, out InvestmentAccount account)
    {
        var normalized = NormalizeLabel(rawLabel);
        foreach (var (candidate, aliases) in AccountLabelAliases)
        {
            if (Array.Exists(aliases, alias => string.Equals(NormalizeLabel(alias), normalized, StringComparison.OrdinalIgnoreCase)))
            {
                account = candidate;
                return true;
            }
        }

        account = default;
        return false;
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
