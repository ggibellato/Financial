using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.InvestmentAccounts;

/// <summary>
/// Idempotently seeds the 19 known investment accounts (11 currently active, 8 historical
/// accounts confirmed present in Resumo sheets 2017-2024 but no longer active in 2026) and
/// audits every snapshot's existing account reference against them. No snapshot field is
/// ever rewritten. <see cref="Name"/> is the exact label these accounts are seeded and
/// matched under; the handful of older Resumo-sheet label variants that don't match a
/// current Name (e.g. a pre-rename label, or a spreadsheet typo) are recognized only by
/// <see cref="SheetImporters.ResumoValidationReader"/>'s own label lookup, not stored here.
/// </summary>
public static class InvestmentAccountMigrator
{
    private static readonly (string Name, bool IsActive, bool IsLiability)[] SeededAccounts =
    [
        ("Blue Rewards Saver", true, false),
        ("Platinum Visa 8003", true, true),
        ("Platinum Visa 6007", true, true),
        ("Chase Master 4023", true, true),
        ("BA Amex", true, true),
        ("Paypal credit", true, true),
        ("Chip Cash ISA Gleison", true, false),
        ("Chase save", true, false),
        ("Chip Cash ISA Ariana", true, false),
        ("Trading 212 Invested", true, false),
        ("Reservas pessoais", true, true),
        ("Everyday Saver", false, false),
        ("Instant ISA Issue 1", false, false),
        ("Ariana ISA", false, false),
        ("Barclays Blue Rewards", false, false),
        ("Help to Buy ISA GGS", false, false),
        ("Help to Buy ISA AACS", false, false),
        ("Chip Easy access", false, false),
        ("Chip Easy access Ariana", false, false)
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
        foreach (var (name, isActive, isLiability) in SeededAccounts)
        {
            var account = data.InvestmentAccounts.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

            if (account is null)
            {
                account = InvestmentAccount.Create(name, isActive: isActive, isLiability: isLiability);
                data.AddInvestmentAccount(account);
                summary.CountAccountSeeded();
            }
            else
            {
                summary.CountAccountAlreadyPresent();
            }
        }
    }

    private static void AuditSnapshots(CashFlowData data, InvestmentAccountMigrationSummary summary)
    {
        foreach (var snapshot in data.InvestmentSnapshots)
        {
            var resolves = data.InvestmentAccounts.Any(a => a.Id == snapshot.Account.Id);

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
