# Implementation Plan: F07. Existing Data Migration

**Prerequisites:**
- F01 (Expense Payment-Date Domain Model Rework) merged to `main` — provides `ChargeDate`/`InvoiceDate` field definitions
- No new environment variables or configuration files

### Stage 1: Domain Backfill Hook

**1. Expense.MigrateLegacyDates** - Add the one-time backfill method to `Expense`, guarded to run at most once per record and to only touch `Date` when a settled date is supplied for an already-settled expense, per the spec's §4 Component Overview. Cover its guards and happy paths with domain unit tests.

### Stage 2: Legacy-Data Recovery and Reporting

**2. LegacySettledAtExtractor** - Build the raw-JSON recovery helper that reads the pre-migration backup text directly and returns a `Id -> SettledAt` lookup, independent of the (already-lossy) typed model, per the spec's §3 Technical Decisions. Cover extraction correctness and malformed-input tolerance with unit tests.

**3. ExpenseChargeDateMigrationSummary** - Add the report type following the existing migration-summary pattern, with counters for each outcome and flagged lists for the two "skip and review" cases described in the spec's §6 Data Model table.

### Stage 3: Migrator and Wiring

**4. ExpenseChargeDateMigrator** - Implement the orchestration logic described in the spec's §2 data flow diagram: bank expenses and already-migrated records pass through untouched, unpaid charges get the straightforward default, settled charges are matched against paid statements and recovered legacy dates with the two flag-and-skip fallbacks. Cover every scenario in the spec's §7 Testing Strategy.

**5. Program.cs Wiring** - Capture the pre-migration backup's raw text right after the existing backup step, call the new migrator alongside the other "always run, both modes" migrators, and print its summary.

### Stage 4: Full Verification

**6. Full Solution Build and Test Pass, Plus a Temp-Copy Dry Run** - Build and test every affected project, then exercise the migrator against a temporary copy of the live `data-cashflow.json` (never the live file itself, per project convention) to confirm it behaves correctly on real data shapes before this PR is reviewed.
