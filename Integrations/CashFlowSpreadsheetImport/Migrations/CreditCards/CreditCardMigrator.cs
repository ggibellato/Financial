using Financial.CashFlow.Domain.Entities;
using CreditCardEnum = Financial.CashFlow.Domain.Enums.CreditCard;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.CreditCards;

/// <summary>
/// Idempotently seeds the 5 tracked credit cards, carried over by name from the legacy
/// <see cref="CreditCardEnum"/> enum, all active with no due date. Names are derived from the
/// enum itself rather than re-typed, so the two can never silently drift apart while the enum
/// still exists.
/// </summary>
public static class CreditCardMigrator
{
    private static readonly string[] SeededCardNames = Enum.GetNames<CreditCardEnum>();

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
