using System.Text;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.ReserveBucketReferences;

/// <summary>
/// Outcome of one migration run: whether the file already carried the current BucketId-based
/// shape (no-op), how many reserve buckets had to be bootstrapped (file predates even F01), how
/// many reserve movements were rewritten, and any movement whose legacy bucket name didn't
/// resolve and needs manual review.
/// </summary>
public sealed class ReserveBucketReferenceMigrationSummary
{
    private readonly List<(Guid Id, string Details)> _unresolvedMovements = new();

    public bool AlreadyCurrentShape { get; private set; }
    public int BucketsBootstrappedCount { get; private set; }
    public int MovementsMigratedCount { get; private set; }

    public IReadOnlyList<(Guid Id, string Details)> UnresolvedMovements => _unresolvedMovements;

    public static ReserveBucketReferenceMigrationSummary NoOp() => new() { AlreadyCurrentShape = true };

    public void SetBucketsBootstrappedCount(int count) => BucketsBootstrappedCount = count;
    public void CountMovementMigrated() => MovementsMigratedCount++;
    public void FlagUnresolvedMovement(Guid id, string details) => _unresolvedMovements.Add((id, details));

    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Reserve bucket reference migration summary");

        if (AlreadyCurrentShape)
        {
            builder.AppendLine("  Data file already in the current BucketId-based shape - nothing to migrate.");
            return builder.ToString();
        }

        if (BucketsBootstrappedCount > 0)
        {
            builder.AppendLine($"  Reserve buckets: {BucketsBootstrappedCount} bootstrapped (file predated the F01 seed migration)");
        }

        builder.AppendLine($"  Reserve movements: {MovementsMigratedCount} migrated");

        if (_unresolvedMovements.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Reserve movements whose legacy bucket name does not match any seeded bucket (skipped, review manually):");
            foreach (var (id, details) in _unresolvedMovements)
            {
                builder.AppendLine($"  {id} {details}");
            }
        }

        return builder.ToString();
    }
}
