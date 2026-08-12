using System.Text;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.Categories;

/// <summary>
/// Outcome of one migration run: how many categories were seeded vs. already present.
/// </summary>
public sealed class CategoryMigrationSummary
{
    public int CategoriesSeededCount { get; private set; }
    public int CategoriesAlreadyPresentCount { get; private set; }

    public void CountCategorySeeded() => CategoriesSeededCount++;
    public void CountCategoryAlreadyPresent() => CategoriesAlreadyPresentCount++;

    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Category migration summary");
        builder.AppendLine($"  Categories: {CategoriesSeededCount} seeded, {CategoriesAlreadyPresentCount} already present");

        return builder.ToString();
    }
}
