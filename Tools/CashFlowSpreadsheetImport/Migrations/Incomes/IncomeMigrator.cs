using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Incomes;

/// <summary>
/// Confirms the Incomes collection exists on the data file and, when a spreadsheet workbook is
/// supplied, backfills historical entries from it via <see cref="IncomeBackfillImporter"/>. With
/// no workbook, this is a no-op audit — CashFlowData.Incomes already default-initializes to an
/// empty list, so the run's only job is to make it auditable and backed up.
/// </summary>
public static class IncomeMigrator
{
    public static IncomeMigrationSummary Migrate(CashFlowData data, XLWorkbook? workbook = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        var importedCount = workbook is null ? 0 : IncomeBackfillImporter.Import(data, workbook);

        return new IncomeMigrationSummary(data.Incomes.Count, importedCount);
    }
}
