using System.Text;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.CreditCards;

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
