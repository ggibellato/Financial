# Implementation Plan: F04. Year-Scoped Investment Account Display

**Prerequisites:**
- .NET 10 SDK (existing solution target)
- F01, F02, F03 merged to main
- No new NuGet packages, environment variables, or configuration files required
- Branch `feat/P18-F04-year-scoped-investment-account-display`, already created from `main`

### Stage 1: Domain Layer

**1. Year-Scoped Account Resolver** - Add a pure business rule that, given the full account registry, all snapshots, a target year, and the current year, returns exactly the accounts applicable to that year: active accounts only for the current year, accounts with at least one snapshot that year for any past year.

### Stage 2: Application Layer

**2. Investment Snapshot Service** - Apply the resolver so a month's snapshot request only auto-creates zero-value rows for, and only returns, accounts that belong to the requested year, replacing the current unconditional all-accounts behavior.

**3. Yearly Summary Service** - Apply the same resolver so a year's investment diff computation only builds rows for, and only sums net position over, accounts that belong to that year.

### Stage 3: Test Suite Alignment

**4. Domain Test Coverage** - Add unit tests for the resolver covering current-year (active-only), past-year (presence-only, independent of active status), and future-year (treated like current-year) cases.

**5. Application Test Updates** - Update the snapshot service and yearly summary service test suites so their repository test doubles seed a mix of active and disabled accounts with varying snapshot years, and assert the year-scoped results.

**6. Full Suite Verification** - Run the complete test suite to confirm the new scoping behaves correctly and nothing else regresses.
