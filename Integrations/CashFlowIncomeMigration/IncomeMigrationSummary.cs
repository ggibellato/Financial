using System.Text;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowIncomeMigration;

/// <summary>
/// Outcome of one migration run: confirms the Incomes collection is present
/// and reports how many entries it currently holds.
/// </summary>
public sealed class IncomeMigrationSummary
{
    public int IncomeCount { get; }

    public IncomeMigrationSummary(int incomeCount)
    {
        IncomeCount = incomeCount;
    }

    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Income migration summary");
        builder.AppendLine($"  Incomes collection present with {IncomeCount} entries.");
        return builder.ToString();
    }
}
