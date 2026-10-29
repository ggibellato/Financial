using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.IncomeSources;

/// <summary>
/// Idempotently seeds the 4 tracked income sources and audits every income's existing source
/// name against them. No income field is ever rewritten: an income's stored source name already
/// matches a seeded source's name, so the audit only counts resolved vs. unresolved names.
/// </summary>
public static class IncomeSourceMigrator
{
    private static readonly (string Name, IncomeGroup Group, bool SplitsToReserva)[] SeededIncomeSources =
    [
        ("Gleison", IncomeGroup.Salary, false),
        ("Ariana", IncomeGroup.Salary, true),
        ("Lottery", IncomeGroup.NonReportable, false),
        ("DividendoJuros", IncomeGroup.DividendoJuros, false)
    ];

    public static IncomeSourceMigrationSummary Migrate(CashFlowData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var summary = new IncomeSourceMigrationSummary();

        SeedIncomeSources(data, summary);
        AuditIncomes(data, summary);

        return summary;
    }

    private static void SeedIncomeSources(CashFlowData data, IncomeSourceMigrationSummary summary)
    {
        var reserva = ReservaAccount.Find(data.Accounts);
        foreach (var (name, group, splitsToReserva) in SeededIncomeSources)
        {
            if (data.IncomeSources.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                summary.CountSourceAlreadyPresent();
                continue;
            }

            data.AddIncomeSource(IncomeSource.Create(name, group, potsAccount: splitsToReserva ? reserva : null));
            summary.CountSourceSeeded();
        }
    }

    private static void AuditIncomes(CashFlowData data, IncomeSourceMigrationSummary summary)
    {
        foreach (var income in data.Incomes)
        {
            var resolves = data.IncomeSources.Any(s => s.Id == income.IncomeSource.Id);

            if (resolves)
            {
                summary.CountIncomeResolved();
            }
            else
            {
                summary.FlagUnresolvedIncome(income);
            }
        }
    }
}
