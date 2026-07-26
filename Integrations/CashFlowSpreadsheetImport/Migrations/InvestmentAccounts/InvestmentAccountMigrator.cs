using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.InvestmentAccounts;

/// <summary>
/// Idempotently seeds the 11 currently-active investment accounts (replacing the retired
/// InvestmentAccount enum) and audits every snapshot's existing account reference against
/// them. No snapshot field is ever rewritten: a snapshot's stored account name already
/// matches a seeded account's name (both come from the same original enum member names),
/// so the audit only counts resolved vs. unresolved references.
/// </summary>
public static class InvestmentAccountMigrator
{
    private static readonly (string Name, bool IsLiability)[] SeededAccounts =
    [
        ("BlueRewardsSaver", false),
        ("PlatinumVisa8003", true),
        ("PlatinumVisa6007", true),
        ("ChaseMaster4023", true),
        ("BaAmex", true),
        ("PaypalCredit", true),
        ("ChipCashIsaGleison", false),
        ("ChaseSave", false),
        ("ChipCashIsaAriana", false),
        ("Trading212Invested", false),
        ("ReservasPessoais", true)
    ];

    public static InvestmentAccountMigrationSummary Migrate(CashFlowData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var summary = new InvestmentAccountMigrationSummary();

        SeedAccounts(data, summary);
        AuditSnapshots(data, summary);

        return summary;
    }

    private static void SeedAccounts(CashFlowData data, InvestmentAccountMigrationSummary summary)
    {
        foreach (var (name, isLiability) in SeededAccounts)
        {
            if (data.InvestmentAccounts.Any(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                summary.CountAccountAlreadyPresent();
                continue;
            }

            data.AddInvestmentAccount(InvestmentAccount.Create(name, isActive: true, isLiability: isLiability));
            summary.CountAccountSeeded();
        }
    }

    private static void AuditSnapshots(CashFlowData data, InvestmentAccountMigrationSummary summary)
    {
        foreach (var snapshot in data.InvestmentSnapshots)
        {
            var resolves = data.InvestmentAccounts.Any(a =>
                string.Equals(a.Name, snapshot.Account, StringComparison.OrdinalIgnoreCase));

            if (resolves)
            {
                summary.CountSnapshotResolved();
            }
            else
            {
                summary.FlagUnresolvedSnapshot(snapshot);
            }
        }
    }
}
