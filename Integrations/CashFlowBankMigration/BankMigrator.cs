using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowBankMigration;

/// <summary>
/// Idempotently seeds the 3 tracked banks and audits every expense's existing bank tag
/// against them. No expense field is ever rewritten: an expense's stored bank name already
/// matches a seeded bank's name, so the audit only counts resolved vs. unresolved tags.
/// </summary>
public static class BankMigrator
{
    private static readonly (string Name, bool RoundUpEnabled)[] SeededBanks =
    [
        ("Barclays", false),
        ("Trading212", true),
        ("Chase", true)
    ];

    public static BankMigrationSummary Migrate(CashFlowData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var summary = new BankMigrationSummary();

        SeedBanks(data, summary);
        AuditExpenses(data, summary);

        return summary;
    }

    private static void SeedBanks(CashFlowData data, BankMigrationSummary summary)
    {
        foreach (var (name, roundUpEnabled) in SeededBanks)
        {
            if (data.Banks.Any(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                summary.CountBankAlreadyPresent();
                continue;
            }

            data.AddBank(Bank.Create(name, roundUpEnabled));
            summary.CountBankSeeded();
        }
    }

    private static void AuditExpenses(CashFlowData data, BankMigrationSummary summary)
    {
        foreach (var expense in data.Expenses)
        {
            if (expense.PaymentSource is null)
            {
                summary.CountExpenseNotApplicable();
                continue;
            }

            var resolves = data.Banks.Any(b =>
                string.Equals(b.Name, expense.PaymentSource, StringComparison.OrdinalIgnoreCase));

            if (resolves)
            {
                summary.CountExpenseResolved();
            }
            else
            {
                summary.FlagUnresolvedExpense(expense);
            }
        }
    }
}
