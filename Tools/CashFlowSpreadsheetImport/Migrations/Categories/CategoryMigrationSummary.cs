using System.Text;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Categories;

public sealed class CategoryMigrationSummary : MigrationSummaryBase
{
    public int CategoriesSeededCount => SeededCount;
    public int CategoriesAlreadyPresentCount => AlreadyPresentCount;

    public void CountCategorySeeded() => CountSeeded();
    public void CountCategoryAlreadyPresent() => CountAlreadyPresent();

    public string Render()
    {
        var builder = new StringBuilder();
        AppendHeader(builder, "Category", "Categories");

        return builder.ToString();
    }
}
