using System.Text;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.Banks;

/// <summary>
/// Outcome of one migration run: how many banks were seeded and how every expense's
/// existing bank tag audited against them, plus anything that needs manual review.
/// </summary>
public sealed class BankMigrationSummary
{
    private readonly List<Expense> _unresolvedExpenses = new();

    public int BanksSeededCount { get; private set; }
    public int BanksAlreadyPresentCount { get; private set; }
    public int ExpensesResolvedCount { get; private set; }
    public int ExpensesNotApplicableCount { get; private set; }

    public IReadOnlyList<Expense> UnresolvedExpenses => _unresolvedExpenses;

    public void CountBankSeeded() => BanksSeededCount++;
    public void CountBankAlreadyPresent() => BanksAlreadyPresentCount++;
    public void CountExpenseResolved() => ExpensesResolvedCount++;
    public void CountExpenseNotApplicable() => ExpensesNotApplicableCount++;

    public void FlagUnresolvedExpense(Expense expense) => _unresolvedExpenses.Add(expense);

    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Bank migration summary");
        builder.AppendLine($"  Banks: {BanksSeededCount} seeded, {BanksAlreadyPresentCount} already present");
        builder.AppendLine($"  Expenses: {ExpensesResolvedCount} resolved, {ExpensesNotApplicableCount} not applicable (credit-card charge)");

        if (_unresolvedExpenses.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Expenses whose bank tag does not match any seeded bank (review manually):");
            foreach (var expense in _unresolvedExpenses)
            {
                builder.AppendLine($"  {expense.Id} {expense.Date:yyyy-MM-dd} '{expense.Description}' [{expense.PaymentSource}]");
            }
        }

        return builder.ToString();
    }
}
