using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.ReserveBuckets;

/// <summary>
/// Idempotently seeds the 4 tracked reserve buckets and audits every reserve movement's bucket
/// against them. <see cref="ReserveMovement.Bucket"/> is a real <see cref="ReserveBucket"/>
/// reference (F02), so a movement resolves exactly when its bucket is one of the seeded instances.
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
            if (data.ReserveBuckets.Contains(movement.Bucket))
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
