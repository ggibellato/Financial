using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.Incomes.SpreadsheetImport;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.Incomes;

/// <summary>
/// Backfills historical Income entries from the retired spreadsheet's monthly income-total rows
/// (see <see cref="IncomeTotalsReader"/>). One entry is created per month per income source,
/// dated the first of that month, skipping months where a source's total is zero. Every entry is
/// attributed to a single fixed bank, since the spreadsheet never recorded which account each
/// total landed in. Idempotent: re-running never duplicates an entry already present for the
/// same date, source and bank.
/// </summary>
public static class IncomeBackfillImporter
{
    private const string TargetBankName = "Barclays";

    public static int Import(CashFlowData data, XLWorkbook workbook)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(workbook);

        var importedCount = 0;

        foreach (var sheet in workbook.Worksheets)
        {
            if (!SheetNameParser.TryParseMonthlySheetName(sheet.Name, out var year, out var month))
            {
                continue;
            }

            var date = new DateOnly(year, month, 1);
            foreach (var total in IncomeTotalsReader.ReadTotals(sheet))
            {
                if (TryImport(data, date, total))
                {
                    importedCount++;
                }
            }
        }

        return importedCount;
    }

    private static bool TryImport(CashFlowData data, DateOnly date, IncomeTotal total)
    {
        if (total.NetValue == 0m || AlreadyImported(data, date, total.Source))
        {
            return false;
        }

        data.AddIncome(Income.Create(date, total.Source, total.GrossValue, total.NetValue, TargetBankName));
        return true;
    }

    private static bool AlreadyImported(CashFlowData data, DateOnly date, string source) =>
        data.Incomes.Any(income =>
            income.Date == date && income.IncomeSource == source && income.Bank == TargetBankName);
}
