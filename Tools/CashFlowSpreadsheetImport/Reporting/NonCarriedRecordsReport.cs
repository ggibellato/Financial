using System.Text.Json;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Categories;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Reporting;

public sealed record NonCarriedRecordsReport(
    int Transfers,
    int BalanceAdjustments,
    int TitheCarryForwards,
    string? TitheEffectiveFrom,
    int UserCategories)
{
    // Counted from raw JSON, not the typed document: the typed loader rejects legacy shapes that
    // the migrations only fix later, and reading raw keeps this check free of side effects.
    public static NonCarriedRecordsReport FromJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var userCategories = ArrayItems(root, "Categories")
            .Count(category => !CategoryMigrator.IsSeededCategoryName(NameOf(category)));

        var effectiveFrom = root.TryGetProperty("TitheCarryForwardEffectiveFrom", out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

        return new NonCarriedRecordsReport(
            CountOf(root, "Transfers"),
            CountOf(root, "BalanceAdjustments"),
            CountOf(root, "TitheCarryForwards"),
            effectiveFrom,
            userCategories);
    }

    public string Render()
    {
        var tithe = TitheEffectiveFrom is null
            ? $"Tithe carry-forwards {TitheCarryForwards}"
            : $"Tithe carry-forwards {TitheCarryForwards} (effective from {TitheEffectiveFrom})";

        return $"Not carried over: Transfers {Transfers}, BalanceAdjustments {BalanceAdjustments}, {tithe}, Categories {UserCategories}";
    }

    private static int CountOf(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var array) && array.ValueKind == JsonValueKind.Array
            ? array.GetArrayLength()
            : 0;

    private static IEnumerable<JsonElement> ArrayItems(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var array) && array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray()
            : [];

    private static string NameOf(JsonElement category) =>
        category.ValueKind == JsonValueKind.Object
        && category.TryGetProperty("Name", out var name)
        && name.ValueKind == JsonValueKind.String
            ? name.GetString() ?? string.Empty
            : string.Empty;
}
