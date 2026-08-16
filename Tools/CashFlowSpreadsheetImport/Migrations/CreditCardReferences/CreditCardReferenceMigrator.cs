using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations;
using Financial.CashFlow.Infrastructure.Persistence;
using System.Text.Json;
using static Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.RawJsonMigrationHelpers;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.CreditCardReferences;

/// <summary>
/// One-time raw-JSON rewrite for the F02 legacy shape: a data file whose <c>Expenses</c>/
/// <c>CardStatements</c> still carry a <c>CardTag</c>/<c>Card</c> enum-name string instead of a
/// <c>CreditCardId</c> reference. Mirrors <c>ReserveBucketReferenceMigrator</c>'s detect/backup/
/// rewrite/save structure but is deliberately its own, smaller class rather than folded into
/// <c>EntityReferenceMigrator</c> - see F02 spec Decision 2. Must run before
/// <c>CashFlowLoader.LoadSync</c>, since the typed deserializer throws on exactly the shape this
/// migrator exists to fix. If the file predates even F01 (no <c>CreditCards</c> array yet), the 5
/// canonical cards are bootstrapped as part of the same pass, reusing <c>CreditCardMigrator</c>'s
/// seed table. Naturally a no-op on a second run. Unlike the ReserveBucket equivalent, an
/// unresolved legacy card name aborts the whole run (per this PRD's explicit error-handling
/// requirement) rather than being skipped and flagged for manual review.
/// </summary>
public static class CreditCardReferenceMigrator
{
    public static CreditCardReferenceMigrationSummary Migrate(string dataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataPath);

        if (!File.Exists(dataPath))
        {
            return CreditCardReferenceMigrationSummary.NoOp();
        }

        var rawJson = File.ReadAllText(dataPath);
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;

        if (!NeedsMigration(root))
        {
            return CreditCardReferenceMigrationSummary.NoOp();
        }

        var summary = new CreditCardReferenceMigrationSummary();
        var unresolvedOptions = CreateElementOptions(context: null);

        var banks = DeserializeCollection<Bank>(root, "Banks", unresolvedOptions);
        var incomeSources = DeserializeCollection<IncomeSource>(root, "IncomeSources", unresolvedOptions);
        var investmentAccounts = DeserializeCollection<InvestmentAccount>(root, "InvestmentAccounts", unresolvedOptions);
        var reserveBuckets = DeserializeCollection<ReserveBucket>(root, "ReserveBuckets", unresolvedOptions);
        var creditCards = ResolveCreditCards(root, unresolvedOptions, summary);
        // Categories (F01/F02) are read as-is, not bootstrapped: this migrator only owns the
        // CardTag/Card -> CreditCardId transition. A file old enough to still need that transition
        // is, in this codebase's actual history, already past the Category migration (P30 landed
        // after P29), so Expenses here always carry a resolved CategoryId already - this lookup
        // exists purely so the full Expense/CardStatement JSON deserialize below can resolve it.
        var categories = DeserializeCollection<Category>(root, "Categories", unresolvedOptions);

        var context = new ReferenceResolutionContext();
        foreach (var bank in banks) context.Banks[bank.Id] = bank;
        foreach (var incomeSource in incomeSources) context.IncomeSources[incomeSource.Id] = incomeSource;
        foreach (var account in investmentAccounts) context.InvestmentAccounts[account.Id] = account;
        foreach (var bucket in reserveBuckets) context.ReserveBuckets[bucket.Id] = bucket;
        foreach (var card in creditCards) context.CreditCards[card.Id] = card;
        foreach (var category in categories) context.Categories[category.Id] = category;

        var resolvedOptions = CreateElementOptions(context);

        var data = CashFlowData.Create();
        foreach (var bank in banks) data.AddBank(bank);
        foreach (var incomeSource in incomeSources) data.AddIncomeSource(incomeSource);
        foreach (var account in investmentAccounts) data.AddInvestmentAccount(account);
        foreach (var bucket in reserveBuckets) data.AddReserveBucket(bucket);
        foreach (var card in creditCards) data.AddCreditCard(card);
        foreach (var category in categories) data.AddCategory(category);

        foreach (var movement in DeserializeCollection<ReserveMovement>(root, "ReserveMovements", resolvedOptions)) data.AddReserveMovement(movement);
        foreach (var bill in DeserializeCollection<RecurringBill>(root, "RecurringBills", resolvedOptions)) data.AddRecurringBill(bill);
        foreach (var entry in DeserializeCollection<MaeLedgerEntry>(root, "MaeLedgerEntries", resolvedOptions)) data.AddMaeLedgerEntry(entry);
        foreach (var snapshot in DeserializeCollection<InvestmentSnapshot>(root, "InvestmentSnapshots", resolvedOptions)) data.AddInvestmentSnapshot(snapshot);
        foreach (var income in DeserializeCollection<Income>(root, "Incomes", resolvedOptions)) data.AddIncome(income);
        foreach (var transfer in DeserializeCollection<Transfer>(root, "Transfers", resolvedOptions)) data.AddTransfer(transfer);
        foreach (var adjustment in DeserializeCollection<BalanceAdjustment>(root, "BalanceAdjustments", resolvedOptions)) data.AddBalanceAdjustment(adjustment);

        var cardsByName = creditCards.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);
        MigrateExpenses(root, cardsByName, resolvedOptions, data, summary);
        MigrateCardStatements(root, cardsByName, data, summary);

        MigrationBackup.Create(dataPath);
        var serializer = new CashFlowSerializerAdapter();
        File.WriteAllText(dataPath, serializer.Serialize(data));

        return summary;
    }

    private static bool NeedsMigration(JsonElement root)
    {
        return HasLegacyField(root, "Expenses", "CardTag") || HasLegacyField(root, "CardStatements", "Card");
    }

    private static bool HasLegacyField(JsonElement root, string collectionName, string legacyFieldName)
    {
        if (!root.TryGetProperty(collectionName, out var collection) || collection.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return collection.EnumerateArray().Any(item => item.TryGetProperty(legacyFieldName, out _));
    }

    private static List<CreditCard> ResolveCreditCards(
        JsonElement root, JsonSerializerOptions unresolvedOptions, CreditCardReferenceMigrationSummary summary) =>
        ResolveOrBootstrap(
            root, "CreditCards", unresolvedOptions,
            data => Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.CreditCards.CreditCardMigrator.Migrate(data),
            data => data.CreditCards,
            summary.SetCardsBootstrappedCount);

    private static void MigrateExpenses(
        JsonElement root,
        IReadOnlyDictionary<string, CreditCard> cardsByName,
        JsonSerializerOptions resolvedOptions,
        CashFlowData data,
        CreditCardReferenceMigrationSummary summary)
    {
        if (!root.TryGetProperty("Expenses", out var expenses) || expenses.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var unresolvedNames = new List<string>();

        foreach (var item in expenses.EnumerateArray())
        {
            var id = item.TryGetProperty("Id", out var idElement) ? idElement.GetGuid() : Guid.Empty;
            var legacyCardName = item.TryGetProperty("CardTag", out var cardTagElement) && cardTagElement.ValueKind == JsonValueKind.String
                ? cardTagElement.GetString()
                : null;

            CreditCard? creditCard = null;
            if (legacyCardName is not null && !cardsByName.TryGetValue(legacyCardName, out creditCard))
            {
                unresolvedNames.Add($"Expense {id}: CardTag='{legacyCardName}'");
                continue;
            }

            var expense = JsonSerializer.Deserialize<Expense>(RewriteCardField(item, "CardTag", creditCard?.Id), resolvedOptions)!;
            data.AddExpense(expense);
            if (creditCard is not null)
            {
                summary.CountExpenseMigrated();
            }
        }

        if (unresolvedNames.Count > 0)
        {
            throw new InvalidOperationException(
                "Credit card reference migration aborted - the following expenses reference a card name with no matching seeded CreditCard:\n"
                + string.Join('\n', unresolvedNames));
        }
    }

    private static void MigrateCardStatements(
        JsonElement root,
        IReadOnlyDictionary<string, CreditCard> cardsByName,
        CashFlowData data,
        CreditCardReferenceMigrationSummary summary)
    {
        if (!root.TryGetProperty("CardStatements", out var statements) || statements.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var unresolvedNames = new List<string>();

        foreach (var item in statements.EnumerateArray())
        {
            var id = item.TryGetProperty("Id", out var idElement) ? idElement.GetGuid() : Guid.Empty;
            var legacyCardName = item.GetProperty("Card").GetString()!;

            if (!cardsByName.TryGetValue(legacyCardName, out var creditCard))
            {
                unresolvedNames.Add($"CardStatement {id}: Card='{legacyCardName}'");
                continue;
            }

            var year = item.GetProperty("Year").GetInt32();
            var month = item.GetProperty("Month").GetInt32();
            var isPaid = item.TryGetProperty("IsPaid", out var isPaidElement) && isPaidElement.GetBoolean();

            var statement = CardStatement.Create(creditCard, year, month);
            SetId(statement, id);
            if (isPaid)
            {
                statement.MarkPaid();
            }

            data.AddCardStatement(statement);
            summary.CountCardStatementMigrated();
        }

        if (unresolvedNames.Count > 0)
        {
            throw new InvalidOperationException(
                "Credit card reference migration aborted - the following card statements reference a card name with no matching seeded CreditCard:\n"
                + string.Join('\n', unresolvedNames));
        }
    }

    private static string RewriteCardField(JsonElement item, string legacyFieldName, Guid? creditCardId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            var wroteCreditCardId = false;
            foreach (var property in item.EnumerateObject())
            {
                if (property.NameEquals(legacyFieldName))
                {
                    WriteCreditCardId(writer, creditCardId);
                    wroteCreditCardId = true;
                    continue;
                }

                property.WriteTo(writer);
            }

            // A legacy record with no card association at all may never have carried the
            // CardTag key (as opposed to carrying it with a null value) - CreditCardId is
            // required on read, so it must still be added even when there was nothing to rename.
            if (!wroteCreditCardId)
            {
                WriteCreditCardId(writer, creditCardId);
            }

            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCreditCardId(Utf8JsonWriter writer, Guid? creditCardId)
    {
        writer.WritePropertyName("CreditCardId");
        if (creditCardId is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(creditCardId.Value);
        }
    }
}
