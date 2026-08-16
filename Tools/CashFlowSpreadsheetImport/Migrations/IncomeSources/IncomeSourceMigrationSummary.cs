using System.Text;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.IncomeSources;

/// <summary>
/// Outcome of one migration run: how many income sources were seeded and how every existing
/// income's source name audited against them, plus anything that needs manual review.
/// </summary>
public sealed class IncomeSourceMigrationSummary
{
    private readonly List<Income> _unresolvedIncomes = new();

    public int SourcesSeededCount { get; private set; }
    public int SourcesAlreadyPresentCount { get; private set; }
    public int IncomesResolvedCount { get; private set; }

    public IReadOnlyList<Income> UnresolvedIncomes => _unresolvedIncomes;

    public void CountSourceSeeded() => SourcesSeededCount++;
    public void CountSourceAlreadyPresent() => SourcesAlreadyPresentCount++;
    public void CountIncomeResolved() => IncomesResolvedCount++;

    public void FlagUnresolvedIncome(Income income) => _unresolvedIncomes.Add(income);

    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Income source migration summary");
        builder.AppendLine($"  Income sources: {SourcesSeededCount} seeded, {SourcesAlreadyPresentCount} already present");
        builder.AppendLine($"  Incomes: {IncomesResolvedCount} resolved");

        if (_unresolvedIncomes.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Incomes whose source name does not match any seeded income source (review manually):");
            foreach (var income in _unresolvedIncomes)
            {
                builder.AppendLine($"  {income.Id} {income.Date:yyyy-MM-dd} [{income.IncomeSource.Name}]");
            }
        }

        return builder.ToString();
    }
}
