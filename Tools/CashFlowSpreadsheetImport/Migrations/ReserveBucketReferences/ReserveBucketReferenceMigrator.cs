using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations;
using Financial.CashFlow.Infrastructure.Persistence;
using System.Text.Json;
using static Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.RawJsonMigrationHelpers;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ReserveBucketReferences;

/// <summary>
/// One-time raw-JSON rewrite for the F02 legacy shape: a data file whose <c>ReserveMovements</c>
/// still carry a <c>Bucket</c> name string instead of a <c>BucketId</c> reference. Mirrors
/// <c>EntityReferenceMigrator</c>'s detect/backup/rewrite/save structure (from a prior PRD's
/// equivalent Bank/IncomeSource/InvestmentAccount transition) but is deliberately its own,
/// smaller class rather than folded into that one - see F02 spec Decision 2. Must run before
/// <c>CashFlowLoader.LoadSync</c>, since the typed deserializer throws on exactly the shape this
/// migrator exists to fix. If the file predates even F01 (no <c>ReserveBuckets</c> array yet),
/// the canonical 4 buckets are bootstrapped as part of the same pass, reusing
/// <see cref="Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ReserveBuckets.ReserveBucketMigrator"/>'s
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
        // Categories (F01/F02) are read as-is, not bootstrapped: this migrator only owns the
        // Bucket -> BucketId transition. This lookup exists purely so the full Expense JSON
        // deserialize below can resolve Expense.Category, now a required reference.
        var categories = DeserializeCollection<Category>(root, "Categories", unresolvedOptions);

        var context = BuildContext(banks, incomeSources, investmentAccounts, reserveBuckets, [], categories);

        var resolvedOptions = CreateElementOptions(context);

        var data = CashFlowData.Create();
        AddBaseCollections(data, banks, incomeSources, investmentAccounts, reserveBuckets, [], categories);

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

        return SaveAndReturn(dataPath, data, summary);
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
        JsonElement root, JsonSerializerOptions unresolvedOptions, ReserveBucketReferenceMigrationSummary summary) =>
        ResolveOrBootstrap(
            root, "ReserveBuckets", unresolvedOptions,
            data => Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ReserveBuckets.ReserveBucketMigrator.Migrate(data),
            data => data.ReserveBuckets,
            summary.SetBucketsBootstrappedCount);

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

}
