using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Reporting;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Reporting;

public class ImportReportTests
{
    [Fact]
    public void Render_ListsEverySheetOutcomeAndRowIssue()
    {
        var report = new ImportReport();
        report.SheetImported("Jul2026");
        report.SheetSkipped("Janeiro 2017", "Out of scope: pre-2017 legacy layout");
        report.RowFlagged("Out2017", 14, "Category", "Casas", "Resolved via known-typo normalization");
        report.ValidationWarning("Resumo2026: category total mismatch for Mercado");

        var rendered = report.Render();

        rendered.Should().Contain("Jul2026").And.NotContain("Sheets imported: 0");
        rendered.Should().Contain("Janeiro 2017").And.Contain("Out of scope");
        rendered.Should().Contain("Out2017").And.Contain("Casas");
        rendered.Should().Contain("Resumo2026");
    }

    [Fact]
    public void Render_WithNoIssues_OmitsEmptySections()
    {
        var report = new ImportReport();
        report.SheetImported("Jul2026");

        var rendered = report.Render();

        rendered.Should().NotContain("--- Skipped sheets ---");
        rendered.Should().NotContain("--- Row issues ---");
        rendered.Should().NotContain("--- Resumo validation warnings ---");
    }

    [Fact]
    public void Collections_ExposeEveryRecordedEntry()
    {
        var report = new ImportReport();
        report.SheetImported("Jul2026");
        report.SheetSkipped("Casa", "Not a monthly tab");
        report.RowFlagged("Out2017", 14, "Category", "Casas", "typo");

        report.ImportedSheets.Should().ContainSingle("Jul2026");
        report.SkippedSheets.Should().ContainSingle(s => s.Sheet == "Casa");
        report.RowIssues.Should().ContainSingle(i => i.RawValue == "Casas");
    }
}
