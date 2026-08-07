using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.ReserveBuckets;

/// <summary>
/// Idempotently seeds the 4 tracked reserve buckets and audits every reserve movement's bucket
/// name against them. <see cref="ReserveMovement.Bucket"/> is still the pre-F02 enum at this
/// point, so the audit compares by name (<c>ToString()</c>) rather than by id - F02 will simplify
/// this to an id comparison once the movement references a real <see cref="ReserveBucket"/>.
/// </summary>
public static class ReserveBucketMigrator
{
    private static readonly (string Name, decimal SplitPercentage)[] SeededBuckets =
    [
        ("Investimento", 33.33m),
        ("HouseTreats", 33.33m),
        ("Ariana", 16.67m),
        ("Gleison", 16.67m)
    ];

    public static ReserveBucketMigrationSummary Migrate(CashFlowData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var summary = new ReserveBucketMigrationSummary();

        SeedReserveBuckets(data, summary);
        AuditReserveMovements(data, summary);
        EvaluateActiveSplitPercentageSum(data, summary);

        return summary;
    }

    private static void SeedReserveBuckets(CashFlowData data, ReserveBucketMigrationSummary summary)
    {
        foreach (var (name, splitPercentage) in SeededBuckets)
        {
            if (data.ReserveBuckets.Any(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                summary.CountBucketAlreadyPresent();
                continue;
            }

            data.AddReserveBucket(ReserveBucket.Create(name, splitPercentage));
            summary.CountBucketSeeded();
        }
    }

    private static void AuditReserveMovements(CashFlowData data, ReserveBucketMigrationSummary summary)
    {
        foreach (var movement in data.ReserveMovements)
        {
            var resolves = data.ReserveBuckets.Any(b =>
                string.Equals(b.Name, movement.Bucket.ToString(), StringComparison.OrdinalIgnoreCase));

            if (resolves)
            {
                summary.CountMovementResolved();
            }
            else
            {
                summary.FlagUnresolvedMovement(movement);
            }
        }
    }

    private static void EvaluateActiveSplitPercentageSum(CashFlowData data, ReserveBucketMigrationSummary summary)
    {
        var activeSum = data.ReserveBuckets.Where(b => b.IsActive).Sum(b => b.SplitPercentage);

        summary.SetActiveSplitPercentageSum(activeSum);
    }
}
