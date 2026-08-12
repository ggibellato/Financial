using System.Text;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.CategoryReferences;

/// <summary>
/// Outcome of one migration run: whether the file already carried the current CategoryId-based
/// shape (no-op), how many categories had to be bootstrapped (file predates even F01), and how
/// many expenses were rewritten. An unresolved legacy category name is never recorded here -
/// <see cref="CategoryReferenceMigrator"/> aborts the whole run instead, per this PRD's explicit
/// error-handling requirement.
/// </summary>
public sealed class CategoryReferenceMigrationSummary
{
    public bool AlreadyCurrentShape { get; private set; }
    public int CategoriesBootstrappedCount { get; private set; }
    public int ExpensesMigratedCount { get; private set; }

    public static CategoryReferenceMigrationSummary NoOp() => new() { AlreadyCurrentShape = true };

    public void SetCategoriesBootstrappedCount(int count) => CategoriesBootstrappedCount = count;
    public void CountExpenseMigrated() => ExpensesMigratedCount++;

    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Category reference migration summary");

        if (AlreadyCurrentShape)
        {
            builder.AppendLine("  Data file already in the current CategoryId-based shape - nothing to migrate.");
            return builder.ToString();
        }

        if (CategoriesBootstrappedCount > 0)
        {
            builder.AppendLine($"  Categories: {CategoriesBootstrappedCount} bootstrapped (file predated the F01 seed migration)");
        }

        builder.AppendLine($"  Expenses: {ExpensesMigratedCount} migrated");

        return builder.ToString();
    }
}
