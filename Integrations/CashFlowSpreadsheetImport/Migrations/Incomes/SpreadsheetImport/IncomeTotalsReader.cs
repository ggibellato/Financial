using ClosedXML.Excel;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.Incomes.SpreadsheetImport;

/// <summary>
/// Reads a monthly sheet's income-totals area. Confirmed empirically against the real workbook
/// that the label/value column positions shift across the spreadsheet's history (e.g. labels in
/// column K for Mar2018-Set2018, column J from Out2018 onward; a Dividendo/Juros row only exists
/// from Abr2022 onward), so - exactly like <c>ResumoValidationReader</c> in the sibling
/// SheetImporters folder - each source's row is located by matching its label text rather than
/// by a fixed row/column. Whatever numeric value(s) sit immediately to the right of a matched
/// label are read: a single value is the net total; two values are gross then net. The "Dizimo"
/// row is intentionally never matched here - it is tithe, not an <see cref="IncomeSource"/>.
/// Confirmed empirically that Fev2017-Fev2018 sheets (before the labeled layout started in
/// Mar2018) predate any label at all and instead carry a single bare net figure at J1 - Gleison
/// was the sole earner for that period, so a sheet with zero matched labels falls back to reading
/// that cell as his net income. From Mar2018 onward J1 is repurposed for something unrelated to
/// income (it stops reconciling with the labeled totals), so this fallback only ever applies when
/// no label matched - never alongside a labeled result.
/// </summary>
public static class IncomeTotalsReader
{
    private const int LabelScanLastRow = 10;
    private const int FirstLabelColumn = 10; // J
    private const int LastLabelColumn = 11; // K
    private const int LegacyUnlabeledNetRow = 1;
    private const int LegacyUnlabeledNetColumn = FirstLabelColumn; // J1

    private static readonly Dictionary<string, string> SourceLabelKeywords = new()
    {
        ["Gleison"] = "gleison",
        ["Ariana"] = "ariana",
        ["Lottery"] = "lot",
        ["DividendoJuros"] = "dividend",
    };

    public static IReadOnlyList<IncomeTotal> ReadTotals(IXLWorksheet sheet)
    {
        var totals = new List<IncomeTotal>();

        foreach (var (source, keyword) in SourceLabelKeywords)
        {
            var labelCell = FindLabelCell(sheet, keyword);
            if (labelCell is null)
            {
                continue;
            }

            var total = ReadValuesRightOf(sheet, labelCell.Value.Row, labelCell.Value.Column, source);
            if (total is not null)
            {
                totals.Add(total.Value);
            }
        }

        if (totals.Count == 0)
        {
            var legacyTotal = ReadLegacyUnlabeledGleisonNet(sheet);
            if (legacyTotal is not null)
            {
                totals.Add(legacyTotal.Value);
            }
        }

        return totals;
    }

    private static IncomeTotal? ReadLegacyUnlabeledGleisonNet(IXLWorksheet sheet)
    {
        var net = NumericCellReader.TryRead(sheet.Cell(LegacyUnlabeledNetRow, LegacyUnlabeledNetColumn));
        return net is null ? null : new IncomeTotal("Gleison", null, net.Value);
    }

    private static (int Row, int Column)? FindLabelCell(IXLWorksheet sheet, string keyword)
    {
        for (var row = 1; row <= LabelScanLastRow; row++)
        {
            for (var column = FirstLabelColumn; column <= LastLabelColumn; column++)
            {
                var label = sheet.Cell(row, column).GetString();
                if (!string.IsNullOrWhiteSpace(label) && label.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return (row, column);
                }
            }
        }

        return null;
    }

    private static IncomeTotal? ReadValuesRightOf(IXLWorksheet sheet, int row, int labelColumn, string source)
    {
        var first = NumericCellReader.TryRead(sheet.Cell(row, labelColumn + 1));
        if (first is null)
        {
            return null;
        }

        var second = NumericCellReader.TryRead(sheet.Cell(row, labelColumn + 2));
        if (second is null)
        {
            return new IncomeTotal(source, null, first.Value);
        }

        // The net column is what the spreadsheet's own tithe formula sums, so it's always
        // trusted as NetValue. Gross can legitimately be lower than net (e.g. non-tax-free
        // compensation), so whatever gross figure is recorded is imported as-is.
        return new IncomeTotal(source, first, second.Value);
    }
}
