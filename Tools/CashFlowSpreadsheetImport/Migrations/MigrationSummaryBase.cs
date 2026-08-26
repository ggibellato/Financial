using System.Text;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations;

/// <summary>
/// Shared counter storage and Render() boilerplate every *MigrationSummary repeats: how many
/// records were seeded vs. already present, the header line, and the "list of unresolved items"
/// section. Each summary keeps its own domain-specific property/method names as thin pass-throughs -
/// only the mechanical counting/rendering lives here.
/// </summary>
public abstract class MigrationSummaryBase
{
    public int SeededCount { get; private set; }
    public int AlreadyPresentCount { get; private set; }

    protected void CountSeeded() => SeededCount++;
    protected void CountAlreadyPresent() => AlreadyPresentCount++;

    protected void AppendHeader(StringBuilder builder, string title, string entityLabel)
    {
        builder.AppendLine($"{title} migration summary");
        builder.AppendLine($"  {entityLabel}: {SeededCount} seeded, {AlreadyPresentCount} already present");
    }

    protected static void AppendUnresolvedSection<T>(
        StringBuilder builder, string header, IReadOnlyCollection<T> items, Func<T, string> formatItem)
    {
        if (items.Count == 0) return;

        builder.AppendLine();
        builder.AppendLine(header);
        foreach (var item in items)
        {
            builder.AppendLine($"  {formatItem(item)}");
        }
    }
}
