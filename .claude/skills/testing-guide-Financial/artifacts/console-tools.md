> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Console Tools (`Tools/CashFlowSpreadsheetImport/**/*.cs`, `Tools/InvestmentSpreadsheetImport/*.cs`, `Tools/InvestmentDataQualityReport/*.cs`)

`CashFlowSpreadsheetImport` is the one consolidated import + data-file-migration command
(`import` from the workbook, `migrate` the JSON between data-file versions V1→V5).
`InvestmentSpreadsheetImport` reads asset metadata from Google Sheets. `InvestmentDataQualityReport`
formats the data-quality findings as text. All three are excluded from the coverage gate in
`coverlet.runsettings`, but not from the requirement to be tested: they rewrite or describe the
user's real data. `Tools/ImportGoogleSpreadSheets` (legacy importer) is out of scope by decision.

## What to test

- **Parsing/resolvers** (`Parsing/`): `CategoryResolver.TryResolve` known name, historical typo
  (`"Casas"` → `Casa`), unknown → `false`; `ColumnResolver`, `NumericCellReader` (comma decimals,
  blanks, text), `SheetNameParser` (`PortugueseMonthAbbreviations`), `ReserveBucketNameResolver`.
- **Sheet importers** (`SheetImporters/`): rows imported into `CashFlowData`; `ImportReport` records
  skipped/invalid rows with the reason; duplicates detected; a missing column → report entry, not
  an exception.
- **Data-file migration steps** (`DataFileMigration/`, one `IDataFileMigrationStep` per version):
  a document at version N becomes version N+1 with every record carried over and the version stamp
  bumped; running on an already-migrated file is a no-op; `DataFileMigrator` applies only the steps
  the file still needs, in order. Each step is tested from a literal fixture of the old shape
  (`V1DataFile`, `V2DataFile`, `V4DataFile`), never from the current serializer.
- **Command-line safety** (`CommandLine/`): argument parsing rejects unknown/missing options;
  `LiveDataFileGuard` refuses the live `data/data-cashflow.json` as input or output;
  `ExistingOutputVersionGuard` refuses an existing output file whose stored version cannot be
  overwritten; `MigrateCommand` refuses the same input and output path and backs up an existing
  output before writing.
- **Migrators and backups** (`Migrations/`): idempotent, correct summary counts;
  `MigrationBackup.Create` copies to a timestamped sibling and never overwrites.
- **Reports**: `ImportReport`, `NonCarriedRecordsReport`, `DataQualityReportFormatter` — each finding
  category, including the empty report.
- **Sheets reader** (`GoogleSheetsAssetReader`): the range requested, a valid row → asset fields, a
  malformed row → reported and skipped.
- Negative: empty workbook, empty data file, unknown bank/category, a backup target that exists, the
  live data path.

## Layer assignment

- **Unit** for resolvers, cell readers, name parsers, argument parsing, report formatters,
  migration steps over in-memory JSON, `AssetClassificationLookup`, `CountryCodeResolver`,
  `AssetMetadataResolver`, and `GoogleSheetsAssetReader` over a stubbed `IGoogleSheetsDataSource`.
- **Integration** for anything touching the disk: `MigrateCommand`, `LiveDataFileGuard`,
  `MigrationBackup`, `FreshImportVersionTests` (a fresh import writes the current version) — temp
  directories via `Directory.CreateTempSubdirectory()`, real `LocalJsonStorage` +
  `CashFlowSerializerAdapter`. Importers run on a real in-memory `XLWorkbook` (ClosedXML).
- **Never run against `data/data-cashflow.json`** — verify against a temp copy first; restarting the
  container never runs a migration.
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

    private static readonly IReadOnlyCollection<Account> Banks =
    [
        TestAccounts.Bank("Barclays", roundUpEnabled: false),
        TestAccounts.Bank("Trading212", roundUpEnabled: true),
        TestAccounts.Bank("Chase", roundUpEnabled: true)
    ];
}
```

`XLWorkbook` holds unmanaged resources — dispose it. For a migration step, build the old-version
JSON from the literal fixture in that folder, run the step, and assert on the resulting JSON nodes
(the new shape, the carried values, the version). For backups: create a temp subdirectory, write
the fixture, call `MigrationBackup.Create(dataPath)`, assert the `data-cashflow.backup-migration-`
sibling, delete the directory in `finally`.

## When to skip

- `Program.cs` argument plumbing beyond what `ImportArguments`/`MigrateArguments` already parse.
- Re-testing domain factories (`Expense.Create`) through the importer.
- One test per historical sheet row variant that takes the same code path.

## Examples from project

- `Tests/Financial.CashFlowSpreadsheetImport.Tests/Parsing/CategoryResolverTests.cs` — Unit.
- `Tests/Financial.CashFlowSpreadsheetImport.Tests/SheetImporters/MonthlyExpenseSheetImporterTests.cs` — in-memory workbook.
- `Tests/Financial.CashFlowSpreadsheetImport.Tests/DataFileMigration/V3ToV4PotsAccountsTests.cs`, `DataFileMigratorTests.cs` — one step, and step ordering.
- `Tests/Financial.CashFlowSpreadsheetImport.Tests/CommandLine/LiveDataFileGuardTests.cs`, `MigrateCommandTests.cs` — Integration; refuses live data, backs up first.
- `Tests/Financial.CashFlowSpreadsheetImport.Tests/Migrations/MigrationBackupTests.cs` — Integration; temp directory.
- `Tests/Financial.InvestmentSpreadsheetImport.Tests/GoogleSheetsAssetReaderTests.cs` — Unit with the Sheets source stubbed.
- `Tests/Financial.InvestmentDataQualityReport.Tests/DataQualityReportFormatterTests.cs` — Unit; every finding category and the empty report.
