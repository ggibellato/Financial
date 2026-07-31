# Implementation Plan: F02. Historical Account Seeding and Import Alias Resolution

**Prerequisites:**
- .NET 10 SDK (existing solution target)
- F01 (Investment Account Registry) merged to main
- No new NuGet packages, environment variables, or configuration files required
- Branch `feat/P18-F02-historical-account-seeding-import-aliases`, already created from `main`

### Stage 1: Domain Layer

**1. Investment Account Aliases** - Add a persisted alias collection to the `InvestmentAccount` entity with an idempotent add operation, following the same collection-property shape already used elsewhere in the aggregate.

### Stage 2: Import Pipeline Rework

**2. Registry Seed Table Expansion** - Grow the investment account migrator's seed table from the 11 active accounts to the full 19 (11 active, 8 historical/disabled), each carrying its known spreadsheet label aliases, including the corrected split between Blue Rewards Saver and the newly recognized Barclays Blue Rewards.

**3. Alias Backfill on Re-seed** - Update the migrator so every seeded account, whether newly created or already present from a prior run, has its known aliases added idempotently, so accounts seeded before this feature existed get backfilled rather than left without aliases.

**4. Dynamic Account Resolution** - Rework the Resumo sheet importer to resolve each row's label against the registry's accounts and their aliases instead of the static, enum-derived alias dictionary, sourcing the liability sign-inversion flag from the matched account instead of a separate hardcoded set.

**5. Pipeline Sequencing** - Reorder the import pipeline's entry point so the account registry (existing accounts carried over, then seeded/backfilled) is fully populated before the Resumo sheets are read, since dynamic resolution now depends on it.

### Stage 3: Test Suite Alignment

**6. Domain Test Updates** - Add coverage for the new alias-adding behavior, including duplicate and validation handling.

**7. Migrator Test Updates** - Extend the migrator's test suite for the full 19-account seed table, the alias backfill behavior on pre-existing accounts, and the corrected Blue Rewards Saver / Barclays Blue Rewards split.

**8. Importer Test Updates** - Update the Resumo sheet importer's tests to resolve against a real seeded registry instead of a static dictionary, and add coverage for previously-unrecognized historical labels now resolving, the corrected Barclays Blue Rewards split, and the two confirmed label aliases (2024 Chip Cash ISA, 2017 Instant ISE Issue 1 typo).

**9. Full Suite and Manual Import Verification** - Run the complete test suite, then run the spreadsheet import command against a copy of the existing data file to confirm the registry seeds and backfills as expected and that previously-unmatched historical account labels are now recognized.
