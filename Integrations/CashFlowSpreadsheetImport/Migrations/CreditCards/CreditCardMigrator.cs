using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.CreditCards;

/// <summary>
/// Idempotently seeds the 5 tracked credit cards, carried over by name from the legacy
/// <see cref="Financial.CashFlow.Domain.Enums.CreditCard"/> enum, all active with no due date.
/// </summary>
public static class CreditCardMigrator
{
    private static readonly string[] SeededCardNames =
    [
        "BarclaysPlatinumVisa8003",
        "BarclaysPlatinumVisa6007",
        "ChaseMaster4023",
        "BaAmex",
        "PaypalCredit"
    ];

    public static CreditCardMigrationSummary Migrate(CashFlowData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var summary = new CreditCardMigrationSummary();

        foreach (var name in SeededCardNames)
        {
            if (data.CreditCards.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                summary.CountCardAlreadyPresent();
                continue;
            }

            data.AddCreditCard(CreditCard.Create(name));
            summary.CountCardSeeded();
        }

        return summary;
    }
}
