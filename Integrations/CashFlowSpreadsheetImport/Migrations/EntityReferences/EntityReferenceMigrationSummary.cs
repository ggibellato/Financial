using System.Text;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.EntityReferences;

/// <summary>
/// Outcome of one migration run: whether the file already carried the current Id-based shape
/// (no-op), how many Banks got a fresh Id, how many of each referencing entity resolved and were
/// rewritten, and any legacy record whose name didn't resolve against the seeded collections and
/// needs manual review.
/// </summary>
public sealed class EntityReferenceMigrationSummary
{
    private readonly List<(Guid Id, string Details)> _unresolvedIncomes = new();
    private readonly List<(Guid Id, string Details)> _unresolvedExpenses = new();
    private readonly List<(Guid Id, string Details)> _unresolvedTransfers = new();
    private readonly List<(Guid Id, string Details)> _unresolvedBalanceAdjustments = new();
    private readonly List<(Guid Id, string Details)> _unresolvedInvestmentSnapshots = new();

    public bool AlreadyCurrentShape { get; private set; }
    public int BanksIdAssignedCount { get; private set; }
    public int IncomesMigratedCount { get; private set; }
    public int ExpensesMigratedCount { get; private set; }
    public int TransfersMigratedCount { get; private set; }
    public int BalanceAdjustmentsMigratedCount { get; private set; }
    public int InvestmentSnapshotsMigratedCount { get; private set; }

    public IReadOnlyList<(Guid Id, string Details)> UnresolvedIncomes => _unresolvedIncomes;
    public IReadOnlyList<(Guid Id, string Details)> UnresolvedExpenses => _unresolvedExpenses;
    public IReadOnlyList<(Guid Id, string Details)> UnresolvedTransfers => _unresolvedTransfers;
    public IReadOnlyList<(Guid Id, string Details)> UnresolvedBalanceAdjustments => _unresolvedBalanceAdjustments;
    public IReadOnlyList<(Guid Id, string Details)> UnresolvedInvestmentSnapshots => _unresolvedInvestmentSnapshots;

    public static EntityReferenceMigrationSummary NoOp() => new() { AlreadyCurrentShape = true };

    public void CountBankIdAssigned() => BanksIdAssignedCount++;
    public void CountIncomeMigrated() => IncomesMigratedCount++;
    public void CountExpenseMigrated() => ExpensesMigratedCount++;
    public void CountTransferMigrated() => TransfersMigratedCount++;
    public void CountBalanceAdjustmentMigrated() => BalanceAdjustmentsMigratedCount++;
    public void CountInvestmentSnapshotMigrated() => InvestmentSnapshotsMigratedCount++;

    public void FlagUnresolvedIncome(Guid id, string details) => _unresolvedIncomes.Add((id, details));
    public void FlagUnresolvedExpense(Guid id, string details) => _unresolvedExpenses.Add((id, details));
    public void FlagUnresolvedTransfer(Guid id, string details) => _unresolvedTransfers.Add((id, details));
    public void FlagUnresolvedBalanceAdjustment(Guid id, string details) => _unresolvedBalanceAdjustments.Add((id, details));
    public void FlagUnresolvedInvestmentSnapshot(Guid id, string details) => _unresolvedInvestmentSnapshots.Add((id, details));

    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Entity reference migration summary");

        if (AlreadyCurrentShape)
        {
            builder.AppendLine("  Data file already in the current Id-based shape - nothing to migrate.");
            return builder.ToString();
        }

        builder.AppendLine($"  Banks: {BanksIdAssignedCount} assigned a fresh Id");
        builder.AppendLine($"  Incomes: {IncomesMigratedCount} migrated");
        builder.AppendLine($"  Expenses: {ExpensesMigratedCount} migrated");
        builder.AppendLine($"  Transfers: {TransfersMigratedCount} migrated");
        builder.AppendLine($"  Balance adjustments: {BalanceAdjustmentsMigratedCount} migrated");
        builder.AppendLine($"  Investment snapshots: {InvestmentSnapshotsMigratedCount} migrated");

        RenderUnresolved(builder, "Incomes", _unresolvedIncomes);
        RenderUnresolved(builder, "Expenses", _unresolvedExpenses);
        RenderUnresolved(builder, "Transfers", _unresolvedTransfers);
        RenderUnresolved(builder, "Balance adjustments", _unresolvedBalanceAdjustments);
        RenderUnresolved(builder, "Investment snapshots", _unresolvedInvestmentSnapshots);

        return builder.ToString();
    }

    private static void RenderUnresolved(StringBuilder builder, string label, IReadOnlyList<(Guid Id, string Details)> unresolved)
    {
        if (unresolved.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine($"{label} whose legacy name does not match any seeded record (skipped, review manually):");
        foreach (var (id, details) in unresolved)
        {
            builder.AppendLine($"  {id} {details}");
        }
    }
}
