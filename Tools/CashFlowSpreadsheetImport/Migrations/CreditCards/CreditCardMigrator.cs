using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.CreditCards;

/// <summary>
/// Idempotently seeds the 5 tracked credit cards, all active with no due date. This is the
/// single source of truth for these names - nothing else in the app hardcodes a credit card
/// list; every other consumer resolves against the seeded entities by name.
/// </summary>
public static class CreditCardMigrator
{
    private static readonly string[] SeededCardNames =
    [
        "Platinum Visa 8003",
        "Platinum Visa 6007",
        "Chase Master 4023",
        "BA Amex",
        "Paypal Credit",
    ];

    public static CreditCardMigrationSummary Migrate(CashFlowData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var summary = new CreditCardMigrationSummary();

        foreach (var name in SeededCardNames)
        {
            if (data.Accounts.Any(c => c.Has(Capability.HasStatements) && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                summary.CountCardAlreadyPresent();
                continue;
            }

            data.AddAccount(Account.Create(name, AccountTemplate.CreditCard, Currency.GBP));
            summary.CountCardSeeded();
        }

        return summary;
    }
}
