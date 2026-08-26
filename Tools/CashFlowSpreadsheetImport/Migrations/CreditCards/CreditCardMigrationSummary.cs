using System.Text;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.CreditCards;

public sealed class CreditCardMigrationSummary : MigrationSummaryBase
{
    public int CardsSeededCount => SeededCount;
    public int CardsAlreadyPresentCount => AlreadyPresentCount;

    public void CountCardSeeded() => CountSeeded();
    public void CountCardAlreadyPresent() => CountAlreadyPresent();

    public string Render()
    {
        var builder = new StringBuilder();
        AppendHeader(builder, "Credit card", "Credit cards");

        return builder.ToString();
    }
}
