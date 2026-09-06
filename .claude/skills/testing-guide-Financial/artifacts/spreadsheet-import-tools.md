> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Spreadsheet Import and Migration Tools (`Tools/CashFlowSpreadsheetImport/**/*.cs`, `Tools/InvestmentSpreadsheetImport/*.cs`)

`CashFlowSpreadsheetImport` is the one consolidated migration/import command (five earlier
tools merged, `project_cashflow_migrations_consolidated`); `InvestmentSpreadsheetImport`
reads asset metadata from Google Sheets. `Tools/ImportGoogleSpreadSheets` (WPF GUI) is out of
scope by decision (2026-09-06). Both tested tools are excluded from the coverage gate in
`coverlet.runsettings`, but not from the requirement to be tested.

## What to test

- **Parsing/resolvers** (`Parsing/`): `CategoryResolver.TryResolve` known name, historical typo
  (`"Casas"` → `Casa`), unknown → `false`; `ColumnResolver`, `NumericCellReader` (comma
  decimals, blanks, text), `SheetNameParser` (`PortugueseMonthAbbreviations`),
  `ReserveBucketNameResolver`.
- **Sheet importers** (`SheetImporters/`): rows imported into `CashFlowData`, `ImportReport`
  records skipped/invalid rows with the reason, duplicate detection, a sheet with a missing
  column → report entry, not exception.
- **Migrators** (`Migrations/`): idempotent (`Migrate_CalledTwice_ProducesTheSameResult`),
  correct counts in the summary, references re-pointed to entities (`*ReferenceMigrator`),
  `MigrationBackup.Create` copies to a timestamped sibling and never overwrites.
- **Sheets reader** (`GoogleSheetsAssetReader`): correct range requested, valid row → asset
  fields, malformed row → reported/skipped.
- Negative: empty workbook, empty data file, unknown bank/category names, a backup target that
  already exists.

## Layer assignment

- **Unit** for resolvers, cell readers, name parsers, `AssetClassificationLookup`,
  `CountryCodeResolver`, `AssetMetadataResolver`.
- **Integration** for importers and migrators: real `CashFlowData` + real in-memory
  `XLWorkbook` (ClosedXML) + real `ImportReport`; `MigrationBackup` against a
  `Directory.CreateTempSubdirectory()`; migrators that read/write JSON use a temp file through
  `LocalJsonStorage` + `CashFlowSerializerAdapter`. Google Sheets is external —
  `IGoogleSheetsDataSource` is stubbed (`GoogleSheetsAssetReaderTests.StubDataSource`).
- **Never run against `data/data-cashflow.json`** — verify against a temp copy first
  (`feedback_never_run_migrations_against_live_data`); a container restart does not run a
  migration (`feedback_container_restart_not_migration`).
- No E2E.

## Setup pattern

From `Tests/Financial.CashFlowSpreadsheetImport.Tests/SheetImporters/MonthlyExpenseSheetImporterTests.cs`:

```csharp
public class MonthlyExpenseSheetImporterTests : IDisposable
{
    private readonly XLWorkbook _workbook;
    private readonly ImportReport _report;

    public MonthlyExpenseSheetImporterTests()
    {
        _workbook = new XLWorkbook();
        _report = new ImportReport();
    }

    public void Dispose() => _workbook.Dispose();

    private static readonly IReadOnlyCollection<Bank> Banks =
    [
        Bank.Create("Barclays", roundUpEnabled: false),
        Bank.Create("Trading212", roundUpEnabled: true),
        Bank.Create("Chase", roundUpEnabled: true)
    ];
}
```

`XLWorkbook` holds unmanaged resources — dispose it (`IDisposable` on the class, as above).
For backups (`MigrationBackupTests.Create_CopiesTheDataFileToATimestampedSibling`): create a
temp subdirectory, write the fixture, call `MigrationBackup.Create(dataPath)`, assert the
sibling exists with the `data-cashflow.backup-migration-` prefix, delete the directory in
`finally`.

## When to skip

- `Program.cs` argument plumbing of the console tool beyond one "unknown command" test.
- `RawJsonMigrationHelpers` beyond its own branches.
- Re-testing domain factories (`Expense.Create`) through the importer.

## Examples from project

- `Tests/Financial.CashFlowSpreadsheetImport.Tests/Parsing/CategoryResolverTests.cs` — Unit.
- `Tests/Financial.CashFlowSpreadsheetImport.Tests/SheetImporters/MonthlyExpenseSheetImporterTests.cs`, `ReservasSheetImporterTests.cs` — Integration (in-memory workbook).
- `Tests/Financial.CashFlowSpreadsheetImport.Tests/Migrations/Incomes/IncomeMigratorTests.cs` — Integration; idempotency.
- `Tests/Financial.CashFlowSpreadsheetImport.Tests/Migrations/MigrationBackupTests.cs` — Integration; temp directory.
- `Tests/Financial.InvestmentSpreadsheetImport.Tests/GoogleSheetsAssetReaderTests.cs` — Unit with the Sheets source stubbed.
