using System.Text;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.Incomes;

/// <summary>
/// Outcome of one migration run: confirms the Incomes collection is present
/// and reports how many entries it currently holds.
/// </summary>
public sealed class IncomeMigrationSummary
{
    public int IncomeCount { get; }
    public int EntriesImportedCount { get; }

    public IncomeMigrationSummary(int incomeCount, int entriesImportedCount = 0)
    {
        IncomeCount = incomeCount;
        EntriesImportedCount = entriesImportedCount;
    }

    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Income migration summary");
        builder.AppendLine($"  Incomes collection present with {IncomeCount} entries.");
        if (EntriesImportedCount > 0)
        {
            builder.AppendLine($"  Imported {EntriesImportedCount} new entries from the spreadsheet backfill.");
        }

        return builder.ToString();
    }
}
