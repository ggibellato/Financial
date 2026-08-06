# Implementation Plan: F03. Live-Data Reference Migration

**Prerequisites:**
- .NET SDK matching the existing `Financial.CashFlow.*`/`Integrations.CashFlowSpreadsheetImport` projects
- No new NuGet packages or environment variables

### Stage 1: Entity Reference Migrator

**1. Legacy Shape Detection and Bank Id Assignment** - Add the migrator's entry point that reads a data file, detects whether it still carries the pre-F01/F02 legacy shape (no-ops cleanly if not), and assigns a fresh `Id` to every existing `Bank` record when it does.

**2. Legacy Record Resolution and Rewrite** - Extend the migrator to read each `Income`/`Expense`/`Transfer`/`BalanceAdjustment`/`InvestmentSnapshot` record's legacy name field(s) from the raw JSON, resolve them case-insensitively against the seeded collections, reconstruct each record through its entity's normal `Create` factory, carry every untouched collection through as-is, and write the rewritten file back out through the existing F02 serializer. Add the migration summary reporting counts and any unresolved records.

### Stage 2: Pipeline Wiring and Backfill Cleanup

**3. Program.cs Sequencing** - Run the new migrator on the data file path immediately after the existing backup step and before the data file is loaded through the typed path, so a genuinely legacy-shaped file no longer crashes the tool.

**4. Remove the Completed Income Backfill** - Delete `IncomeBackfillImporter` and its dedicated tests, and drop the now-unused `workbook` parameter from `IncomeMigrator.Migrate`, updating its one call site.

### Stage 3: Test Coverage

**5. Entity Reference Migrator Tests** - Cover legacy-shape detection, Bank Id assignment, per-entity-type resolution and rewrite, idempotency on a second run, the pre-write backup, and the unresolved-name reporting path, all against a hand-built legacy-shaped fixture file.

**6. Income Migrator Test Updates** - Update `IncomeMigratorTests` to the single-parameter signature.
