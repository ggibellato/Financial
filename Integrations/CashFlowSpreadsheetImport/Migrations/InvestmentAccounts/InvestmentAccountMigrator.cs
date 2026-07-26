using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.InvestmentAccounts;

/// <summary>
/// Idempotently seeds the 19 known investment accounts (11 currently active, 8 historical
/// accounts confirmed present in Resumo sheets 2017-2024 but no longer active in 2026) and
/// audits every snapshot's existing account reference against them. No snapshot field is
/// ever rewritten. Every account's known spreadsheet-label aliases are (re-)added on every
/// run, whether the account is newly created or already present, so accounts seeded before
/// aliases existed (e.g. by an F01-era run) get backfilled instead of staying alias-less.
/// </summary>
public static class InvestmentAccountMigrator
{
    private static readonly (string Name, bool IsActive, bool IsLiability, string[] Aliases)[] SeededAccounts =
    [
        ("BlueRewardsSaver", true, false, ["Blue Rewards Saver"]),
        ("PlatinumVisa8003", true, true, ["Platinum Visa 8003"]),
        ("PlatinumVisa6007", true, true, ["Platinum Visa 6007"]),
        ("ChaseMaster4023", true, true, ["Chase Master 4023"]),
        ("BaAmex", true, true, ["BA Amex"]),
        ("PaypalCredit", true, true, ["Paypal credit"]),
        ("ChipCashIsaGleison", true, false, ["Chip Cash ISA Gleison", "Chip Cash ISA"]),
        ("ChaseSave", true, false, ["Chase save"]),
        ("ChipCashIsaAriana", true, false, ["Chip Cash ISA Ariana"]),
        ("Trading212Invested", true, false, ["Trading 212 Invested"]),
        ("ReservasPessoais", true, true, ["Reservas pessoais"]),
        ("EverydaySaver", false, false, ["Everyday Saver"]),
        ("InstantIsaIssue1", false, false, ["Instant ISA Issue 1", "Instant ISE Issue 1"]),
        ("ArianaIsa", false, false, ["Ariana ISA"]),
        ("BarclaysBlueRewards", false, false, ["Barclays Blue Rewards"]),
        ("HelpToBuyIsaGgs", false, false, ["Help to Buy ISA GGS"]),
        ("HelpToBuyIsaAacs", false, false, ["Help to Buy ISA AACS"]),
        ("ChipEasyAccess", false, false, ["Chip Easy access"]),
        ("ChipEasyAccessAriana", false, false, ["Chip Easy access Ariana"])
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
        foreach (var (name, isActive, isLiability, aliases) in SeededAccounts)
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

            foreach (var alias in aliases)
            {
                account.AddAlias(alias);
            }
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
