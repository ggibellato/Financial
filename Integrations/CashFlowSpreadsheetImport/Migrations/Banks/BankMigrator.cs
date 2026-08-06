using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.Banks;

/// <summary>
/// Idempotently seeds the 3 tracked banks and audits every expense's payment source. Since
/// Expense.PaymentSourceBank is a real Bank reference (F01), an expense can no longer hold an
/// unresolvable bank tag - the audit only counts applicable (bank-paid) vs. not-applicable
/// (credit-card charge) expenses.
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
            if (expense.PaymentSourceBank is null)
            {
                summary.CountExpenseNotApplicable();
            }
            else
            {
                summary.CountExpenseResolved();
            }
        }
    }
}
