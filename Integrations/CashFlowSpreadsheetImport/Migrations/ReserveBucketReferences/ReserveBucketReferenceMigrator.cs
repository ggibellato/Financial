using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations;
using Financial.CashFlow.Infrastructure.Persistence;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.ReserveBucketReferences;

/// <summary>
/// One-time raw-JSON rewrite for the F02 legacy shape: a data file whose <c>ReserveMovements</c>
/// still carry a <c>Bucket</c> name string instead of a <c>BucketId</c> reference. Mirrors
/// <c>EntityReferenceMigrator</c>'s detect/backup/rewrite/save structure (from a prior PRD's
/// equivalent Bank/IncomeSource/InvestmentAccount transition) but is deliberately its own,
/// smaller class rather than folded into that one - see F02 spec Decision 2. Must run before
/// <c>CashFlowLoader.LoadSync</c>, since the typed deserializer throws on exactly the shape this
/// migrator exists to fix. If the file predates even F01 (no <c>ReserveBuckets</c> array yet),
/// the canonical 4 buckets are bootstrapped as part of the same pass, reusing
/// <see cref="Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.ReserveBuckets.ReserveBucketMigrator"/>'s
/// seed table. Naturally a no-op on a second run.
/// </summary>
public static class ReserveBucketReferenceMigrator
{
    public static ReserveBucketReferenceMigrationSummary Migrate(string dataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataPath);

        if (!File.Exists(dataPath))
        {
            return ReserveBucketReferenceMigrationSummary.NoOp();
        }

        var rawJson = File.ReadAllText(dataPath);
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;

        if (!NeedsMigration(root))
        {
            return ReserveBucketReferenceMigrationSummary.NoOp();
        }

        var summary = new ReserveBucketReferenceMigrationSummary();
        var unresolvedOptions = CreateElementOptions(context: null);

        var banks = DeserializeCollection<Bank>(root, "Banks", unresolvedOptions);
        var incomeSources = DeserializeCollection<IncomeSource>(root, "IncomeSources", unresolvedOptions);
        var investmentAccounts = DeserializeCollection<InvestmentAccount>(root, "InvestmentAccounts", unresolvedOptions);
        var reserveBuckets = ResolveReserveBuckets(root, unresolvedOptions, summary);

        var context = new ReferenceResolutionContext();
        foreach (var bank in banks) context.Banks[bank.Id] = bank;
        foreach (var incomeSource in incomeSources) context.IncomeSources[incomeSource.Id] = incomeSource;
        foreach (var account in investmentAccounts) context.InvestmentAccounts[account.Id] = account;
        foreach (var bucket in reserveBuckets) context.ReserveBuckets[bucket.Id] = bucket;

        var resolvedOptions = CreateElementOptions(context);

        var data = CashFlowData.Create();
        foreach (var bank in banks) data.AddBank(bank);
        foreach (var incomeSource in incomeSources) data.AddIncomeSource(incomeSource);
        foreach (var account in investmentAccounts) data.AddInvestmentAccount(account);
        foreach (var bucket in reserveBuckets) data.AddReserveBucket(bucket);

        foreach (var expense in DeserializeCollection<Expense>(root, "Expenses", resolvedOptions)) data.AddExpense(expense);
        foreach (var statement in DeserializeCollection<CardStatement>(root, "CardStatements", resolvedOptions)) data.AddCardStatement(statement);
        foreach (var bill in DeserializeCollection<RecurringBill>(root, "RecurringBills", resolvedOptions)) data.AddRecurringBill(bill);
        foreach (var entry in DeserializeCollection<MaeLedgerEntry>(root, "MaeLedgerEntries", resolvedOptions)) data.AddMaeLedgerEntry(entry);
        foreach (var snapshot in DeserializeCollection<InvestmentSnapshot>(root, "InvestmentSnapshots", resolvedOptions)) data.AddInvestmentSnapshot(snapshot);
        foreach (var income in DeserializeCollection<Income>(root, "Incomes", resolvedOptions)) data.AddIncome(income);
        foreach (var transfer in DeserializeCollection<Transfer>(root, "Transfers", resolvedOptions)) data.AddTransfer(transfer);
        foreach (var adjustment in DeserializeCollection<BalanceAdjustment>(root, "BalanceAdjustments", resolvedOptions)) data.AddBalanceAdjustment(adjustment);

        var bucketsByName = reserveBuckets.ToDictionary(b => b.Name, b => b, StringComparer.OrdinalIgnoreCase);
        MigrateReserveMovements(root, bucketsByName, data, summary);

        MigrationBackup.Create(dataPath);
        var serializer = new CashFlowSerializerAdapter();
        File.WriteAllText(dataPath, serializer.Serialize(data));

        return summary;
    }

    private static bool NeedsMigration(JsonElement root)
    {
        if (!root.TryGetProperty("ReserveMovements", out var movements) || movements.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return movements.EnumerateArray().Any(item => item.TryGetProperty("Bucket", out _));
    }

    private static List<ReserveBucket> ResolveReserveBuckets(
        JsonElement root, JsonSerializerOptions unresolvedOptions, ReserveBucketReferenceMigrationSummary summary)
    {
        if (root.TryGetProperty("ReserveBuckets", out var element) && element.ValueKind == JsonValueKind.Array)
        {
            return DeserializeCollection<ReserveBucket>(root, "ReserveBuckets", unresolvedOptions);
        }

        var bootstrapData = CashFlowData.Create();
        Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.ReserveBuckets.ReserveBucketMigrator.Migrate(bootstrapData);
        var bootstrapped = bootstrapData.ReserveBuckets.ToList();
        summary.SetBucketsBootstrappedCount(bootstrapped.Count);

        return bootstrapped;
    }

    private static void MigrateReserveMovements(
        JsonElement root,
        IReadOnlyDictionary<string, ReserveBucket> bucketsByName,
        CashFlowData data,
        ReserveBucketReferenceMigrationSummary summary)
    {
        if (!root.TryGetProperty("ReserveMovements", out var movements) || movements.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in movements.EnumerateArray())
        {
            var id = item.GetProperty("Id").GetGuid();
            var bucketName = item.GetProperty("Bucket").GetString()!;

            if (!bucketsByName.TryGetValue(bucketName, out var bucket))
            {
                summary.FlagUnresolvedMovement(id, $"Bucket='{bucketName}'");
                continue;
            }

            var amount = item.GetProperty("Amount").GetDecimal();
            var date = DateOnly.Parse(item.GetProperty("Date").GetString()!);
            var description = item.GetProperty("Description").GetString()!;

            var movement = ReserveMovement.Create(bucket, amount, date, description);
            SetId(movement, id);
            data.AddReserveMovement(movement);
            summary.CountMovementMigrated();
        }
    }

    private static JsonSerializerOptions CreateElementOptions(ReferenceResolutionContext? context) => new()
    {
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new CashFlowTypeInfoResolver(context)
    };

    private static List<T> DeserializeCollection<T>(JsonElement root, string propertyName, JsonSerializerOptions options)
    {
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<T>>(element.GetRawText(), options) ?? [];
    }

    private static void SetId(object entity, Guid id) =>
        entity.GetType().GetProperty("Id")!.SetMethod!.Invoke(entity, [id]);
}
