using System.Text;
using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ReserveBuckets;

/// <summary>
/// Outcome of one migration run: how many reserve buckets were seeded, how every existing
/// reserve movement's bucket name audited against them, and whether the active buckets' split
/// percentages sum to ~100% - reported as a warning, never a failure.
/// </summary>
public sealed class ReserveBucketMigrationSummary : MigrationSummaryBase
{
    private const decimal ExpectedActiveSplitPercentageSum = 100m;
    private const decimal SplitPercentageTolerance = 0.01m;

    private readonly List<ReserveMovement> _unresolvedMovements = new();

    public int BucketsSeededCount => SeededCount;
    public int BucketsAlreadyPresentCount => AlreadyPresentCount;
    public int MovementsResolvedCount { get; private set; }
    public decimal ActiveSplitPercentageSum { get; private set; }

    public bool ActiveSplitPercentageIsBalanced =>
        Math.Abs(ActiveSplitPercentageSum - ExpectedActiveSplitPercentageSum) <= SplitPercentageTolerance;

    public IReadOnlyList<ReserveMovement> UnresolvedMovements => _unresolvedMovements;

    public void CountBucketSeeded() => CountSeeded();
    public void CountBucketAlreadyPresent() => CountAlreadyPresent();
    public void CountMovementResolved() => MovementsResolvedCount++;

    public void FlagUnresolvedMovement(ReserveMovement movement) => _unresolvedMovements.Add(movement);

    public void SetActiveSplitPercentageSum(decimal sum) => ActiveSplitPercentageSum = sum;

    public string Render()
    {
        var builder = new StringBuilder();
        AppendHeader(builder, "Reserve bucket", "Reserve buckets");
        builder.AppendLine($"  Reserve movements: {MovementsResolvedCount} resolved");

        if (!ActiveSplitPercentageIsBalanced)
        {
            builder.AppendLine();
            builder.AppendLine($"  WARNING: active buckets' split percentages sum to {ActiveSplitPercentageSum:F2}%, not 100%.");
        }

        AppendUnresolvedSection(builder,
            "Reserve movements whose bucket name does not match any seeded bucket (review manually):",
            _unresolvedMovements, movement => $"{movement.Id} {movement.Date:yyyy-MM-dd} [{movement.Bucket}]");

        return builder.ToString();
    }
}
