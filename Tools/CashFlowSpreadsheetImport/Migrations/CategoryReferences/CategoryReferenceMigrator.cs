using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations;
using Financial.CashFlow.Infrastructure.Persistence;
using System.Text.Json;
using static Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.RawJsonMigrationHelpers;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.CategoryReferences;

/// <summary>
/// One-time raw-JSON rewrite for the F02 legacy shape: a data file whose <c>Expenses</c> still
/// carry a <c>Category</c> enum-name string instead of a <c>CategoryId</c> reference. Mirrors
/// <c>CreditCardReferenceMigrator</c>'s detect/backup/rewrite/save structure but is deliberately
/// its own, smaller class rather than folded into <c>EntityReferenceMigrator</c> - see F02 spec
/// Decision 2. Must run before <c>CashFlowLoader.LoadSync</c>, since the typed deserializer throws
/// on exactly the shape this migrator exists to fix. If the file predates even F01 (no
/// <c>Categories</c> array yet), the 14 canonical categories are bootstrapped as part of the same
/// pass, reusing <c>CategoryMigrator</c>'s seed table. Naturally a no-op on a second run. An
/// unresolved legacy category name aborts the whole run (per this PRD's explicit error-handling
/// requirement) rather than being skipped and flagged for manual review.
/// </summary>
public static class CategoryReferenceMigrator
{
    public static CategoryReferenceMigrationSummary Migrate(string dataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataPath);

        if (!File.Exists(dataPath))
        {
            return CategoryReferenceMigrationSummary.NoOp();
        }

        var rawJson = File.ReadAllText(dataPath);
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;

        if (!NeedsMigration(root))
        {
            return CategoryReferenceMigrationSummary.NoOp();
        }

        var summary = new CategoryReferenceMigrationSummary();
        var unresolvedOptions = CreateElementOptions(context: null);

        var banks = DeserializeCollection<Bank>(root, "Banks", unresolvedOptions);
        var incomeSources = DeserializeCollection<IncomeSource>(root, "IncomeSources", unresolvedOptions);
        var investmentAccounts = DeserializeCollection<InvestmentAccount>(root, "InvestmentAccounts", unresolvedOptions);
        var reserveBuckets = DeserializeCollection<ReserveBucket>(root, "ReserveBuckets", unresolvedOptions);
        var creditCards = DeserializeCollection<CreditCard>(root, "CreditCards", unresolvedOptions);
        var categories = ResolveCategories(root, unresolvedOptions, summary);

        var context = BuildContext(banks, incomeSources, investmentAccounts, reserveBuckets, creditCards, categories);

        var resolvedOptions = CreateElementOptions(context);

        var data = CashFlowData.Create();
        AddBaseCollections(data, banks, incomeSources, investmentAccounts, reserveBuckets, creditCards, categories);

        foreach (var movement in DeserializeCollection<ReserveMovement>(root, "ReserveMovements", resolvedOptions)) data.AddReserveMovement(movement);
        foreach (var statement in DeserializeCollection<CardStatement>(root, "CardStatements", resolvedOptions)) data.AddCardStatement(statement);
        foreach (var bill in DeserializeCollection<RecurringBill>(root, "RecurringBills", resolvedOptions)) data.AddRecurringBill(bill);
        foreach (var entry in DeserializeCollection<MaeLedgerEntry>(root, "MaeLedgerEntries", resolvedOptions)) data.AddMaeLedgerEntry(entry);
        foreach (var snapshot in DeserializeCollection<InvestmentSnapshot>(root, "InvestmentSnapshots", resolvedOptions)) data.AddInvestmentSnapshot(snapshot);
        foreach (var income in DeserializeCollection<Income>(root, "Incomes", resolvedOptions)) data.AddIncome(income);
        foreach (var transfer in DeserializeCollection<Transfer>(root, "Transfers", resolvedOptions)) data.AddTransfer(transfer);
        foreach (var adjustment in DeserializeCollection<BalanceAdjustment>(root, "BalanceAdjustments", resolvedOptions)) data.AddBalanceAdjustment(adjustment);

        var categoriesByName = categories.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);
        MigrateExpenses(root, categoriesByName, resolvedOptions, data, summary);

        return SaveAndReturn(dataPath, data, summary);
    }

    private static bool NeedsMigration(JsonElement root) => HasLegacyField(root, "Expenses", "Category");

    private static bool HasLegacyField(JsonElement root, string collectionName, string legacyFieldName)
    {
        if (!root.TryGetProperty(collectionName, out var collection) || collection.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return collection.EnumerateArray().Any(item =>
            item.TryGetProperty(legacyFieldName, out var value) && value.ValueKind == JsonValueKind.String);
    }

    private static List<Category> ResolveCategories(
        JsonElement root, JsonSerializerOptions unresolvedOptions, CategoryReferenceMigrationSummary summary) =>
        ResolveOrBootstrap(
            root, "Categories", unresolvedOptions,
            data => Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Categories.CategoryMigrator.Migrate(data),
            data => data.Categories,
            summary.SetCategoriesBootstrappedCount);

    private static void MigrateExpenses(
        JsonElement root,
        IReadOnlyDictionary<string, Category> categoriesByName,
        JsonSerializerOptions resolvedOptions,
        CashFlowData data,
        CategoryReferenceMigrationSummary summary)
    {
        if (!root.TryGetProperty("Expenses", out var expenses) || expenses.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var unresolvedNames = new List<string>();

        foreach (var item in expenses.EnumerateArray())
        {
            var id = item.TryGetProperty("Id", out var idElement) ? idElement.GetGuid() : Guid.Empty;
            var legacyCategoryName = item.TryGetProperty("Category", out var categoryElement) && categoryElement.ValueKind == JsonValueKind.String
                ? categoryElement.GetString()
                : null;

            if (legacyCategoryName is null || !categoriesByName.TryGetValue(legacyCategoryName, out var category))
            {
                unresolvedNames.Add($"Expense {id}: Category='{legacyCategoryName}'");
                continue;
            }

            var expense = JsonSerializer.Deserialize<Expense>(RewriteCategoryField(item, category.Id), resolvedOptions)!;
            data.AddExpense(expense);
            summary.CountExpenseMigrated();
        }

        if (unresolvedNames.Count > 0)
        {
            throw new InvalidOperationException(
                "Category reference migration aborted - the following expenses reference a category name with no matching seeded Category:\n"
                + string.Join('\n', unresolvedNames));
        }
    }

    private static string RewriteCategoryField(JsonElement item, Guid categoryId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in item.EnumerateObject())
            {
                if (property.NameEquals("Category"))
                {
                    writer.WriteString("CategoryId", categoryId);
                    continue;
                }

                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}
