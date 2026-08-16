using System.Text;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.CreditCardReferences;

/// <summary>
/// Outcome of one migration run: whether the file already carried the current CreditCardId-based
/// shape (no-op), how many credit cards had to be bootstrapped (file predates even F01), and how
/// many expenses/card statements were rewritten. Unlike the ReserveBucket equivalent, an
/// unresolved legacy card name is never recorded here - <see cref="CreditCardReferenceMigrator"/>
/// aborts the whole run instead, per this PRD's explicit error-handling requirement.
/// </summary>
public sealed class CreditCardReferenceMigrationSummary
{
    public bool AlreadyCurrentShape { get; private set; }
    public int CardsBootstrappedCount { get; private set; }
    public int ExpensesMigratedCount { get; private set; }
    public int CardStatementsMigratedCount { get; private set; }

    public static CreditCardReferenceMigrationSummary NoOp() => new() { AlreadyCurrentShape = true };

    public void SetCardsBootstrappedCount(int count) => CardsBootstrappedCount = count;
    public void CountExpenseMigrated() => ExpensesMigratedCount++;
    public void CountCardStatementMigrated() => CardStatementsMigratedCount++;

    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Credit card reference migration summary");

        if (AlreadyCurrentShape)
        {
            builder.AppendLine("  Data file already in the current CreditCardId-based shape - nothing to migrate.");
            return builder.ToString();
        }

        if (CardsBootstrappedCount > 0)
        {
            builder.AppendLine($"  Credit cards: {CardsBootstrappedCount} bootstrapped (file predated the F01 seed migration)");
        }

        builder.AppendLine($"  Expenses: {ExpensesMigratedCount} migrated");
        builder.AppendLine($"  Card statements: {CardStatementsMigratedCount} migrated");

        return builder.ToString();
    }
}
