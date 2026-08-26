using System.Text;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.IncomeSources;

public sealed class IncomeSourceMigrationSummary : MigrationSummaryBase
{
    private readonly List<Income> _unresolvedIncomes = new();

    public int SourcesSeededCount => SeededCount;
    public int SourcesAlreadyPresentCount => AlreadyPresentCount;
    public int IncomesResolvedCount { get; private set; }

    public IReadOnlyList<Income> UnresolvedIncomes => _unresolvedIncomes;

    public void CountSourceSeeded() => CountSeeded();
    public void CountSourceAlreadyPresent() => CountAlreadyPresent();
    public void CountIncomeResolved() => IncomesResolvedCount++;

    public void FlagUnresolvedIncome(Income income) => _unresolvedIncomes.Add(income);

    public string Render()
    {
        var builder = new StringBuilder();
        AppendHeader(builder, "Income source", "Income sources");
        builder.AppendLine($"  Incomes: {IncomesResolvedCount} resolved");

        AppendUnresolvedSection(builder,
            "Incomes whose source name does not match any seeded income source (review manually):",
            _unresolvedIncomes, income => $"{income.Id} {income.Date:yyyy-MM-dd} [{income.IncomeSource.Name}]");

        return builder.ToString();
    }
}
