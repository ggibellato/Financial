using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ReserveBuckets;

/// <summary>
/// Idempotently seeds the 5 tracked reserve buckets and audits every reserve movement's bucket
/// against them. <see cref="ReserveMovement.Bucket"/> is a real <see cref="ReserveBucket"/>
/// reference (F02), so a movement resolves exactly when its bucket is one of the seeded instances.
/// </summary>
public static class ReserveBucketMigrator
{
    private const string ReservaName = "Reserva";

    private static readonly (string Name, decimal SplitPercentage)[] SeededBuckets =
    [
        ("Investimento", 20m),
        ("HouseTreats", 20m),
        ("Ariana", 20m),
        ("Gleison", 20m),
        ("Samuel", 20m),
    ];

    public static ReserveBucketMigrationSummary Migrate(CashFlowData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var summary = new ReserveBucketMigrationSummary();

        SeedReserveBuckets(data, summary);
        AssignReservaCategory(data);
        AuditReserveMovements(data, summary);
        EvaluateActiveSplitPercentageSum(data, summary);

        return summary;
    }

    private static void SeedReserveBuckets(CashFlowData data, ReserveBucketMigrationSummary summary)
    {
        var reserva = ReservaOf(data);
        foreach (var (name, splitPercentage) in SeededBuckets)
        {
            if (reserva.Pots!.Any(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                summary.CountBucketAlreadyPresent();
                continue;
            }

            reserva.AddPot(ReserveBucket.Create(name, splitPercentage));
            summary.CountBucketSeeded();
        }
    }

    private static void AssignReservaCategory(CashFlowData data)
    {
        var reserva = ReservaOf(data);
        if (reserva.Category is not null)
        {
            return;
        }

        var category = data.Categories.FirstOrDefault(c => string.Equals(c.Name, ReservaName, StringComparison.OrdinalIgnoreCase));
        if (category is not null)
        {
            reserva.SetCategory(category);
        }
    }

    private static Account ReservaOf(CashFlowData data)
    {
        if (ReservaAccount.Find(data.Accounts) is { } reserva)
        {
            return reserva;
        }

        var created = Account.Create(ReservaName, AccountTemplate.Pots, Currency.GBP);
        data.AddAccount(created);
        return created;
    }

    private static IReadOnlyList<ReserveBucket> PotsOf(CashFlowData data) => ReservaAccount.Find(data.Accounts)?.Pots ?? [];

    private static void AuditReserveMovements(CashFlowData data, ReserveBucketMigrationSummary summary)
    {
        var pots = PotsOf(data);
        foreach (var movement in data.ReserveMovements)
        {
            if (pots.Contains(movement.Bucket))
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
        var activeSum = PotsOf(data).Where(b => b.IsActive).Sum(b => b.SplitPercentage);

        summary.SetActiveSplitPercentageSum(activeSum);
    }
}
