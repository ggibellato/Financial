using System.Text;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.BankOpeningBalance;

/// <summary>
/// Outcome of one migration run: how many banks had their opening balance defaulted
/// versus how many already carried a real value from a previous run or manual edit.
/// </summary>
public sealed class BankOpeningBalanceMigrationSummary
{
    public int BanksDefaultedCount { get; private set; }
    public int BanksAlreadySetCount { get; private set; }

    public void CountBankDefaulted() => BanksDefaultedCount++;
    public void CountBankAlreadySet() => BanksAlreadySetCount++;

    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Bank opening balance migration summary");
        builder.AppendLine($"  Banks: {BanksDefaultedCount} defaulted, {BanksAlreadySetCount} already set");
        return builder.ToString();
    }
}
