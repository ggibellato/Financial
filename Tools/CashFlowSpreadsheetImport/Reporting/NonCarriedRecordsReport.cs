using System.Text.Json;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Categories;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Reporting;

public sealed class NonCarriedRecordsReport
{
    private readonly int _transfers;
    private readonly int _balanceAdjustments;
    private readonly int _titheCarryForwards;
    private readonly string? _titheEffectiveFrom;
    private readonly int _userCategories;

    private NonCarriedRecordsReport(int transfers, int balanceAdjustments, int titheCarryForwards, string? titheEffectiveFrom, int userCategories)
    {
        _transfers = transfers;
        _balanceAdjustments = balanceAdjustments;
        _titheCarryForwards = titheCarryForwards;
        _titheEffectiveFrom = titheEffectiveFrom;
        _userCategories = userCategories;
    }

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
            ArrayItems(root, "Transfers").Count(),
            ArrayItems(root, "BalanceAdjustments").Count(),
            ArrayItems(root, "TitheCarryForwards").Count(),
            effectiveFrom,
            userCategories);
    }

    public string Render()
    {
        var tithe = _titheEffectiveFrom is null
            ? $"Tithe carry-forwards {_titheCarryForwards}"
            : $"Tithe carry-forwards {_titheCarryForwards} (effective from {_titheEffectiveFrom})";

        return $"Not carried over: Transfers {_transfers}, BalanceAdjustments {_balanceAdjustments}, {tithe}, Categories {_userCategories}";
    }

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
