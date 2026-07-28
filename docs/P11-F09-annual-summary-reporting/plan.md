# Implementation Plan: F09. Annual Summary & Month-over-Month Reporting

**Prerequisites:**
- Existing `Financial.CashFlow.Domain`/`.Application`/`.Infrastructure` projects and `data-cashflow.json` repository pattern from F02
- F03's `Expense` entity and F08's `InvestmentSnapshot` entity/`InvestmentAccountClassification` rule already exist
- No new NuGet packages
- No Domain or repository changes needed — this feature only reads F03/F08's existing collections via the existing `GetExpenses`/`GetInvestmentSnapshots`

### Stage 1: Application Layer

**1. Annual summary DTOs** - Add the per-category annual totals read model, the per-account annual diffs read model, the combined net-position read model, and the wrapper that groups the 11 accounts with the net-position row.

**2. Annual summary service** - Add `IAnnualSummaryService`/`AnnualSummaryService` covering per-category annual totals (summed from that year's expenses) and per-account/combined-net-position month-over-month diffs (derived from that year's investment snapshots, applying the existing liability classification for the net-position subtraction).

**3. Register the new service** - Add `IAnnualSummaryService` to the existing `CashFlowApplicationServiceCollectionExtensions`.

**4. Application-layer tests** - Add service tests covering the full-14-category and full-11-account coverage guarantees, the annual-total-equals-sum-of-months assertion, the per-account and net-position diff math, missing-snapshot-defaults-to-zero behavior, and the full-year net change.

### Stage 2: Presentation Layer

**5. Annual summary controller** - Add HTTP endpoints for the annual expense-category totals and the annual investment diffs.

**6. API integration tests** - Add endpoint tests covering both endpoints' full round trip over HTTP against seeded data.

### Stage 3: Verification

**7. Full-suite validation** - Build the entire solution and run every test project, confirming no regression to Investments or the existing CashFlow features from F01-F08.

**8. Manual verification** - Run `Financial.Api` locally, seed a few months of expenses and investment snapshots across a year via the existing F03/F08 endpoints, then exercise the new endpoints directly to confirm the computed category totals and investment diffs (including the net-position row and full-year change) match the underlying data end-to-end.
