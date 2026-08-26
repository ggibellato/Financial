using System.Text;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Banks;

public sealed class BankMigrationSummary : MigrationSummaryBase
{
    private readonly List<Expense> _unresolvedExpenses = new();

    public int BanksSeededCount => SeededCount;
    public int BanksAlreadyPresentCount => AlreadyPresentCount;
    public int ExpensesResolvedCount { get; private set; }
    public int ExpensesNotApplicableCount { get; private set; }

    public IReadOnlyList<Expense> UnresolvedExpenses => _unresolvedExpenses;

    public void CountBankSeeded() => CountSeeded();
    public void CountBankAlreadyPresent() => CountAlreadyPresent();
    public void CountExpenseResolved() => ExpensesResolvedCount++;
    public void CountExpenseNotApplicable() => ExpensesNotApplicableCount++;

    public void FlagUnresolvedExpense(Expense expense) => _unresolvedExpenses.Add(expense);

    public string Render()
    {
        var builder = new StringBuilder();
        AppendHeader(builder, "Bank", "Banks");
        builder.AppendLine($"  Expenses: {ExpensesResolvedCount} resolved, {ExpensesNotApplicableCount} not applicable (credit-card charge)");

        AppendUnresolvedSection(builder,
            "Expenses whose bank tag does not match any seeded bank (review manually):",
            _unresolvedExpenses, expense => $"{expense.Id} {expense.Date:yyyy-MM-dd} '{expense.Description}' [{expense.PaymentSourceBank?.Name}]");

        return builder.ToString();
    }
}
