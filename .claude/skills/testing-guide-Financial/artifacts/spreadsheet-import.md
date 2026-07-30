> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Spreadsheet Import (`Integrations/CashFlowSpreadsheetImport/**`)

Covers `Parsing/*`, `Reporting/*`, and `SheetImporters/*` — the tool that reads `Despesas.xlsx` via ClosedXML into `data-cashflow.json`, consolidating what were previously five separate migration tools.

## What to test

- Each importer's row-parsing logic: date formats it must handle (full `dd/MM/yyyy`, month-year-only like "Dez/2018"), value columns, currency detection
- Malformed/unexpected rows land in `ImportReport.RowIssues` rather than throwing or silently dropping data
- `Parsing/` helpers (`CategoryResolver`, `ColumnResolver`, `NumericCellReader`) — same criteria as `artifacts/application-parsers.md`: every recognized case, blank/invalid case

## Layer assignment

Unit/Integration blend, but notably **no file I/O at all** — tests build the spreadsheet **in memory** with the real ClosedXML library rather than reading a static `.xlsx` fixture from disk. This proves the parsing logic against a real `IXLWorksheet` (catching any ClosedXML API misuse) without any filesystem dependency or fixture-file maintenance burden.

## Setup pattern

```csharp
public class ControleMaeSheetImporterTests
{
    [Fact]
    public void Import_FullDdMmYyyyDate_ParsesDateAndValues()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Controle mae");
        sheet.Cell(1, 1).Value = "Deposito feito na conta do Gabriel em 28/05/2019";
        sheet.Cell(1, 2).Value = 300.0;
        sheet.Cell(1, 3).Value = 60.0;
        var report = new ImportReport();

        var entries = ControleMaeSheetImporter.Import(sheet, report);

        entries.Should().ContainSingle();
        entries[0].Date.Should().Be(new DateOnly(2019, 5, 28));
        report.RowIssues.Should().BeEmpty();
    }

    [Fact]
    public void Import_MonthYearOnlyDate_ParsesToFirstOfMonth()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Controle mae");
        sheet.Cell(1, 1).Value = "Feito acerto em Dez/2018, seguro pago ate Dez/2018";
        // ...

        var entries = ControleMaeSheetImporter.Import(sheet, new ImportReport());

        entries[0].Date.Should().Be(new DateOnly(2018, 12, 1));
    }
}
```

`using var workbook = new XLWorkbook()` — always dispose; ClosedXML workbooks hold unmanaged resources.

## When to skip

- Don't test ClosedXML's own cell-reading behavior (trust the library) — only test *this project's* interpretation of cell contents (date-format disambiguation, category/column resolution, currency detection)
- Don't add a test requiring the real `Despesas.xlsx` file — building the minimal worksheet in-test keeps tests independent of that file's evolving real-world content

## Examples from project

- `ControleMaeSheetImporterTests` — date-format branching (full date vs month-year-only)
- `CategoryResolverTests`, `ColumnResolverTests`, `NumericCellReaderTests` — parser-style unit tests per `artifacts/application-parsers.md`'s criteria
