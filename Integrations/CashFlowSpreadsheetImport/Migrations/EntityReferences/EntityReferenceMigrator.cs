using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations;
using Financial.CashFlow.Infrastructure.Persistence;
using System.Text.Json;
using static Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.RawJsonMigrationHelpers;
using CreditCardEntity = Financial.CashFlow.Domain.Entities.CreditCard;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.EntityReferences;

/// <summary>
/// The first *rewriting* migrator in this codebase: reads a data file still in the pre-F01/F02
/// legacy shape (Bank records with no Id; Income/Expense/Transfer/BalanceAdjustment/
/// InvestmentSnapshot records carrying a raw name string instead of an *Id field) and rewrites it
/// into the current Id-based shape. Must run before <c>CashFlowLoader.LoadSync</c>, since F02's
/// typed deserializer throws on exactly the shape this migrator exists to fix - unlike every
/// other migrator in this tool, this one operates on the file path directly rather than an
/// already-loaded CashFlowData. Naturally a no-op on a second run: once the file is already in
/// the current shape, nothing is read or written.
/// </summary>
public static class EntityReferenceMigrator
{
    public static EntityReferenceMigrationSummary Migrate(string dataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataPath);

        if (!File.Exists(dataPath))
        {
            return EntityReferenceMigrationSummary.NoOp();
        }

        var rawJson = File.ReadAllText(dataPath);
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;

        if (!NeedsMigration(root))
        {
            return EntityReferenceMigrationSummary.NoOp();
        }

        var summary = new EntityReferenceMigrationSummary();
        var elementOptions = CreateElementOptions();

        var banks = ReadLegacyBanks(root, summary);
        var banksByName = banks.ToDictionary(b => b.Name, b => b, StringComparer.OrdinalIgnoreCase);
        var incomeSources = DeserializeCollection<IncomeSource>(root, "IncomeSources", elementOptions);
        var incomeSourcesByName = incomeSources.ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);
        var investmentAccounts = DeserializeCollection<InvestmentAccount>(root, "InvestmentAccounts", elementOptions);
        var investmentAccountsByName = investmentAccounts.ToDictionary(a => a.Name, a => a, StringComparer.OrdinalIgnoreCase);
        // ReserveBucket has no reference properties of its own, so it deserializes fine with the
        // null-context options - but ReserveMovement.Bucket (F02) is now a reference to it, so a
        // resolution context built from this collection is required before ReserveMovements can
        // be read below, and the buckets themselves must be carried into `data` or they would be
        // silently dropped from the rewritten file.
        var reserveBuckets = DeserializeCollection<ReserveBucket>(root, "ReserveBuckets", elementOptions);
        // A file this old also predates F01's CreditCard seed migration, so the seeded cards are
        // bootstrapped here too (mirroring ReserveBucketReferenceMigrator's own bootstrap), rather
        // than assuming a "CreditCards" array already exists in this legacy shape.
        var creditCards = ResolveCreditCards(root, elementOptions);
        var cardsByName = creditCards.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);
        // A file this old also predates F01's Category seed migration, so the seeded categories
        // are bootstrapped here too, mirroring the identical CreditCard bootstrap above.
        var categories = ResolveCategories(root, elementOptions);
        var categoriesByName = categories.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);
        var referenceContext = new ReferenceResolutionContext();
        foreach (var bucket in reserveBuckets) referenceContext.ReserveBuckets[bucket.Id] = bucket;
        foreach (var card in creditCards) referenceContext.CreditCards[card.Id] = card;
        foreach (var category in categories) referenceContext.Categories[category.Id] = category;
        var resolvedOptions = CreateElementOptions(referenceContext);

        var data = CashFlowData.Create();
        foreach (var bank in banks) data.AddBank(bank);
        foreach (var incomeSource in incomeSources) data.AddIncomeSource(incomeSource);
        foreach (var account in investmentAccounts) data.AddInvestmentAccount(account);
        foreach (var bucket in reserveBuckets) data.AddReserveBucket(bucket);
        foreach (var card in creditCards) data.AddCreditCard(card);
        foreach (var category in categories) data.AddCategory(category);
        foreach (var movement in DeserializeCollection<ReserveMovement>(root, "ReserveMovements", resolvedOptions)) data.AddReserveMovement(movement);
        foreach (var statement in DeserializeCollection<CardStatement>(root, "CardStatements", resolvedOptions)) data.AddCardStatement(statement);
        foreach (var bill in DeserializeCollection<RecurringBill>(root, "RecurringBills", elementOptions)) data.AddRecurringBill(bill);
        foreach (var entry in DeserializeCollection<MaeLedgerEntry>(root, "MaeLedgerEntries", elementOptions)) data.AddMaeLedgerEntry(entry);

        MigrateIncomes(root, banksByName, incomeSourcesByName, data, summary);
        MigrateExpenses(root, banksByName, cardsByName, categoriesByName, data, summary);
        MigrateTransfers(root, banksByName, data, summary);
        MigrateBalanceAdjustments(root, banksByName, data, summary);
        MigrateInvestmentSnapshots(root, investmentAccountsByName, data, summary);

        MigrationBackup.Create(dataPath);
        var serializer = new CashFlowSerializerAdapter();
        File.WriteAllText(dataPath, serializer.Serialize(data));

        return summary;
    }

    private static bool NeedsMigration(JsonElement root)
    {
        if (root.TryGetProperty("Banks", out var banks) && banks.ValueKind == JsonValueKind.Array)
        {
            foreach (var bank in banks.EnumerateArray())
            {
                if (!bank.TryGetProperty("Id", out _))
                {
                    return true;
                }
            }
        }

        return HasLegacyField(root, "Incomes", "Bank")
            || HasLegacyField(root, "Incomes", "IncomeSource")
            || HasLegacyField(root, "Expenses", "PaymentSource")
            || HasLegacyField(root, "Transfers", "SourceBank")
            || HasLegacyField(root, "Transfers", "DestinationBank")
            || HasLegacyField(root, "BalanceAdjustments", "Bank")
            || HasLegacyField(root, "InvestmentSnapshots", "Account");
    }

    private static bool HasLegacyField(JsonElement root, string collectionName, string legacyFieldName)
    {
        if (!root.TryGetProperty(collectionName, out var collection) || collection.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return collection.EnumerateArray().Any(item => item.TryGetProperty(legacyFieldName, out _));
    }

    private static List<Bank> ReadLegacyBanks(JsonElement root, EntityReferenceMigrationSummary summary)
    {
        var banks = new List<Bank>();

        if (!root.TryGetProperty("Banks", out var banksElement) || banksElement.ValueKind != JsonValueKind.Array)
        {
            return banks;
        }

        foreach (var item in banksElement.EnumerateArray())
        {
            var name = item.GetProperty("Name").GetString()!;
            var roundUpEnabled = item.GetProperty("RoundUpEnabled").GetBoolean();

            var bank = Bank.Create(name, roundUpEnabled);
            summary.CountBankIdAssigned();

            if (item.TryGetProperty("OpeningBalance", out var openingBalanceElement)
                && item.TryGetProperty("OpeningBalanceDate", out var openingBalanceDateElement)
                && openingBalanceDateElement.ValueKind == JsonValueKind.String)
            {
                var openingBalance = openingBalanceElement.GetDecimal();
                var openingBalanceDate = DateOnly.Parse(openingBalanceDateElement.GetString()!);
                if (openingBalance != 0m || openingBalanceDate != default)
                {
                    bank.SetOpeningBalance(openingBalance, openingBalanceDate);
                }
            }

            banks.Add(bank);
        }

        return banks;
    }

    private static void MigrateIncomes(
        JsonElement root,
        IReadOnlyDictionary<string, Bank> banksByName,
        IReadOnlyDictionary<string, IncomeSource> incomeSourcesByName,
        CashFlowData data,
        EntityReferenceMigrationSummary summary)
    {
        if (!root.TryGetProperty("Incomes", out var incomes) || incomes.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in incomes.EnumerateArray())
        {
            var id = item.GetProperty("Id").GetGuid();
            var incomeSourceName = item.GetProperty("IncomeSource").GetString()!;
            var bankName = item.GetProperty("Bank").GetString()!;

            if (!incomeSourcesByName.TryGetValue(incomeSourceName, out var incomeSource)
                || !banksByName.TryGetValue(bankName, out var bank))
            {
                summary.FlagUnresolvedIncome(id, $"IncomeSource='{incomeSourceName}' Bank='{bankName}'");
                continue;
            }

            var date = ReadDate(item, "Date");
            var grossValue = ReadNullableDecimal(item, "GrossValue");
            var netValue = item.GetProperty("NetValue").GetDecimal();

            var income = Income.Create(date, incomeSource, grossValue, netValue, bank);
            SetId(income, id);
            data.AddIncome(income);
            summary.CountIncomeMigrated();
        }
    }

    private static List<CreditCardEntity> ResolveCreditCards(JsonElement root, JsonSerializerOptions unresolvedOptions)
    {
        if (root.TryGetProperty("CreditCards", out var element) && element.ValueKind == JsonValueKind.Array)
        {
            return DeserializeCollection<CreditCardEntity>(root, "CreditCards", unresolvedOptions);
        }

        var bootstrapData = CashFlowData.Create();
        Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.CreditCards.CreditCardMigrator.Migrate(bootstrapData);
        return bootstrapData.CreditCards.ToList();
    }

    private static List<Category> ResolveCategories(JsonElement root, JsonSerializerOptions unresolvedOptions)
    {
        if (root.TryGetProperty("Categories", out var element) && element.ValueKind == JsonValueKind.Array)
        {
            return DeserializeCollection<Category>(root, "Categories", unresolvedOptions);
        }

        var bootstrapData = CashFlowData.Create();
        Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.Categories.CategoryMigrator.Migrate(bootstrapData);
        return bootstrapData.Categories.ToList();
    }

    private static void MigrateExpenses(
        JsonElement root,
        IReadOnlyDictionary<string, Bank> banksByName,
        IReadOnlyDictionary<string, CreditCardEntity> cardsByName,
        IReadOnlyDictionary<string, Category> categoriesByName,
        CashFlowData data,
        EntityReferenceMigrationSummary summary)
    {
        if (!root.TryGetProperty("Expenses", out var expenses) || expenses.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in expenses.EnumerateArray())
        {
            var id = item.GetProperty("Id").GetGuid();
            var paymentSourceName = ReadNullableString(item, "PaymentSource");
            Bank? paymentSourceBank = null;
            if (paymentSourceName is not null && !banksByName.TryGetValue(paymentSourceName, out paymentSourceBank))
            {
                summary.FlagUnresolvedExpense(id, $"PaymentSource='{paymentSourceName}'");
                continue;
            }

            var legacyCardName = ReadNullableString(item, "CardTag");
            CreditCardEntity? creditCard = null;
            if (legacyCardName is not null && !cardsByName.TryGetValue(legacyCardName, out creditCard))
            {
                summary.FlagUnresolvedExpense(id, $"CardTag='{legacyCardName}'");
                continue;
            }

            var legacyCategoryName = item.GetProperty("Category").GetString()!;
            if (!categoriesByName.TryGetValue(legacyCategoryName, out var category))
            {
                summary.FlagUnresolvedExpense(id, $"Category='{legacyCategoryName}'");
                continue;
            }

            var date = ReadDate(item, "Date");
            var description = item.GetProperty("Description").GetString()!;
            var value = item.GetProperty("Value").GetDecimal();
            var invoiceDate = ReadNullableDate(item, "InvoiceDate");

            var expense = Expense.Create(date, description, value, category, paymentSourceBank, creditCard, invoiceDate);
            SetId(expense, id);
            data.AddExpense(expense);
            summary.CountExpenseMigrated();
        }
    }

    private static void MigrateTransfers(
        JsonElement root,
        IReadOnlyDictionary<string, Bank> banksByName,
        CashFlowData data,
        EntityReferenceMigrationSummary summary)
    {
        if (!root.TryGetProperty("Transfers", out var transfers) || transfers.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in transfers.EnumerateArray())
        {
            var id = item.GetProperty("Id").GetGuid();
            var sourceBankName = item.GetProperty("SourceBank").GetString()!;
            var destinationBankName = item.GetProperty("DestinationBank").GetString()!;

            if (!banksByName.TryGetValue(sourceBankName, out var sourceBank)
                || !banksByName.TryGetValue(destinationBankName, out var destinationBank))
            {
                summary.FlagUnresolvedTransfer(id, $"SourceBank='{sourceBankName}' DestinationBank='{destinationBankName}'");
                continue;
            }

            var date = ReadDate(item, "Date");
            var amount = item.GetProperty("Amount").GetDecimal();
            var note = ReadNullableString(item, "Note");

            var transfer = Transfer.Create(date, sourceBank, destinationBank, amount, note);
            SetId(transfer, id);
            data.AddTransfer(transfer);
            summary.CountTransferMigrated();
        }
    }

    private static void MigrateBalanceAdjustments(
        JsonElement root,
        IReadOnlyDictionary<string, Bank> banksByName,
        CashFlowData data,
        EntityReferenceMigrationSummary summary)
    {
        if (!root.TryGetProperty("BalanceAdjustments", out var adjustments) || adjustments.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in adjustments.EnumerateArray())
        {
            var id = item.GetProperty("Id").GetGuid();
            var bankName = item.GetProperty("Bank").GetString()!;

            if (!banksByName.TryGetValue(bankName, out var bank))
            {
                summary.FlagUnresolvedBalanceAdjustment(id, $"Bank='{bankName}'");
                continue;
            }

            var date = ReadDate(item, "Date");
            var targetBalance = item.GetProperty("TargetBalance").GetDecimal();
            var delta = item.GetProperty("Delta").GetDecimal();
            var note = ReadNullableString(item, "Note");

            var adjustment = BalanceAdjustment.Create(date, bank, targetBalance, delta, note);
            SetId(adjustment, id);
            data.AddBalanceAdjustment(adjustment);
            summary.CountBalanceAdjustmentMigrated();
        }
    }

    private static void MigrateInvestmentSnapshots(
        JsonElement root,
        IReadOnlyDictionary<string, InvestmentAccount> investmentAccountsByName,
        CashFlowData data,
        EntityReferenceMigrationSummary summary)
    {
        if (!root.TryGetProperty("InvestmentSnapshots", out var snapshots) || snapshots.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in snapshots.EnumerateArray())
        {
            var id = item.GetProperty("Id").GetGuid();
            var accountName = item.GetProperty("Account").GetString()!;

            if (!investmentAccountsByName.TryGetValue(accountName, out var account))
            {
                summary.FlagUnresolvedInvestmentSnapshot(id, $"Account='{accountName}'");
                continue;
            }

            var year = item.GetProperty("Year").GetInt32();
            var month = item.GetProperty("Month").GetInt32();
            var value = item.GetProperty("Value").GetDecimal();

            var snapshot = InvestmentSnapshot.Create(account, year, month, value);
            SetId(snapshot, id);
            data.AddInvestmentSnapshot(snapshot);
            summary.CountInvestmentSnapshotMigrated();
        }
    }

    private static DateOnly ReadDate(JsonElement item, string propertyName) =>
        DateOnly.Parse(item.GetProperty(propertyName).GetString()!);

    private static DateOnly? ReadNullableDate(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateOnly.Parse(element.GetString()!);
    }

    private static decimal? ReadNullableDecimal(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return element.GetDecimal();
    }

    private static string? ReadNullableString(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return element.GetString();
    }
}
