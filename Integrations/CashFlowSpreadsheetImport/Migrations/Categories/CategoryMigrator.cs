using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.Categories;

/// <summary>
/// Idempotently seeds the 14 tracked categories. This is the single source of truth for these
/// names - nothing else in the app hardcodes a category list; every other consumer resolves
/// against the seeded entities by name.
/// </summary>
public static class CategoryMigrator
{
    private static readonly string[] SeededCategoryNames =
    [
        "Ariana",
        "Carro",
        "Casa",
        "Estudo",
        "Extras",
        "Familia",
        "Gleison",
        "Mercado",
        "Samuel",
        "Saude",
        "Viagem",
        "Dizimo",
        "Investimento",
        "Reserva",
    ];

    private const string InvestmentCategoryName = "Investimento";
    private const string TitheCategoryName = "Dizimo";

    public static CategoryMigrationSummary Migrate(CashFlowData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var summary = new CategoryMigrationSummary();

        foreach (var name in SeededCategoryNames)
        {
            if (data.Categories.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                summary.CountCategoryAlreadyPresent();
                continue;
            }

            var isInvestment = string.Equals(name, InvestmentCategoryName, StringComparison.OrdinalIgnoreCase);
            var isTithe = string.Equals(name, TitheCategoryName, StringComparison.OrdinalIgnoreCase);

            data.AddCategory(Category.Create(name, isInvestment: isInvestment, isTithe: isTithe));
            summary.CountCategorySeeded();
        }

        return summary;
    }
}
