using ClosedXML.Excel;
using Financial.CashFlow.Domain.Enums;
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
/// </summary>
public static class IncomeTotalsReader
{
    private const int LabelScanLastRow = 10;
    private const int FirstLabelColumn = 10; // J
    private const int LastLabelColumn = 11; // K

    private static readonly Dictionary<IncomeSource, string> SourceLabelKeywords = new()
    {
        [IncomeSource.Gleison] = "gleison",
        [IncomeSource.Ariana] = "ariana",
        [IncomeSource.Lottery] = "lot",
        [IncomeSource.DividendoJuros] = "dividend",
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

        return totals;
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

    private static IncomeTotal? ReadValuesRightOf(IXLWorksheet sheet, int row, int labelColumn, IncomeSource source)
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
        // trusted as NetValue. The gross column is occasionally smaller than net for Ariana's
        // weekly-paycheck rows (the two totals were summed across slightly different manual
        // entries) - when that happens the recorded "gross" isn't a real gross figure, so it's
        // dropped rather than violating the domain's GrossValue >= NetValue invariant.
        var grossValue = first >= second ? first : null;
        return new IncomeTotal(source, grossValue, second.Value);
    }
}
