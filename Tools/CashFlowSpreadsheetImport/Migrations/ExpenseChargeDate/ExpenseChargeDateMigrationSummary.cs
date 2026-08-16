using System.Text;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ExpenseChargeDate;

/// <summary>
/// Outcome of one migration run: how many expenses were migrated in each shape, plus the
/// already-settled expenses that couldn't be migrated (no matching statement, or no recoverable
/// legacy SettledAt) and need manual review.
/// </summary>
public sealed class ExpenseChargeDateMigrationSummary
{
    private readonly List<Expense> _missingStatementExpenses = new();
    private readonly List<Expense> _missingSettledAtExpenses = new();

    public int BankExpenseCount { get; private set; }
    public int AlreadyMigratedCount { get; private set; }
    public int NewlyMigratedUnpaidCount { get; private set; }
    public int NewlyMigratedSettledCount { get; private set; }

    public IReadOnlyList<Expense> MissingStatementExpenses => _missingStatementExpenses;
    public IReadOnlyList<Expense> MissingSettledAtExpenses => _missingSettledAtExpenses;

    public void CountBankExpense() => BankExpenseCount++;
    public void CountAlreadyMigrated() => AlreadyMigratedCount++;
    public void CountNewlyMigratedUnpaid() => NewlyMigratedUnpaidCount++;
    public void CountNewlyMigratedSettled() => NewlyMigratedSettledCount++;

    public void FlagMissingStatement(Expense expense) => _missingStatementExpenses.Add(expense);
    public void FlagMissingSettledAt(Expense expense) => _missingSettledAtExpenses.Add(expense);

    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Expense charge/invoice date migration summary");
        builder.AppendLine($"  Bank expenses (untouched): {BankExpenseCount}");
        builder.AppendLine($"  Already migrated: {AlreadyMigratedCount}");
        builder.AppendLine($"  Newly migrated (unpaid charge): {NewlyMigratedUnpaidCount}");
        builder.AppendLine($"  Newly migrated (settled charge): {NewlyMigratedSettledCount}");

        if (_missingStatementExpenses.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Settled expenses with no matching paid statement (skipped, review manually):");
            foreach (var expense in _missingStatementExpenses)
            {
                builder.AppendLine($"  {expense.Id} {expense.Date:yyyy-MM-dd} '{expense.Description}' [{expense.CreditCard?.Name}]");
            }
        }

        if (_missingSettledAtExpenses.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Settled expenses with no recoverable legacy SettledAt (skipped, review manually):");
            foreach (var expense in _missingSettledAtExpenses)
            {
                builder.AppendLine($"  {expense.Id} {expense.Date:yyyy-MM-dd} '{expense.Description}' [{expense.CreditCard?.Name}]");
            }
        }

        return builder.ToString();
    }
}
