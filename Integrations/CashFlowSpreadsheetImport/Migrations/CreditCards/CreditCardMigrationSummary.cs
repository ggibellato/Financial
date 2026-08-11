using System.Text;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.CreditCards;

/// <summary>
/// Outcome of one migration run: how many credit cards were seeded vs. already present.
/// </summary>
public sealed class CreditCardMigrationSummary
{
    public int CardsSeededCount { get; private set; }
    public int CardsAlreadyPresentCount { get; private set; }

    public void CountCardSeeded() => CardsSeededCount++;
    public void CountCardAlreadyPresent() => CardsAlreadyPresentCount++;

    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Credit card migration summary");
        builder.AppendLine($"  Credit cards: {CardsSeededCount} seeded, {CardsAlreadyPresentCount} already present");

        return builder.ToString();
    }
}
