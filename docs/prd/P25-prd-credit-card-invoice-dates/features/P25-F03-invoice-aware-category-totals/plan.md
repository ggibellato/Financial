# Implementation Plan: F03. Invoice-Aware Category Totals

**Prerequisites:**
- F01 (Expense Payment-Date Domain Model Rework) merged to `main` — provides `ChargeDate`/`InvoiceDate`
- No new environment variables or configuration files

### Stage 1: Effective-Date Grouping

**1. AnnualSummaryService Effective-Date Rework** - Add the effective-date helper and apply it to `BuildAllCategorySeriesForYear`'s year filter and current-month-average cutoff, `BuildCategoryMonthlyTotals`'s grouping key, and `GetHistoricCategoriesAverageFromYear`'s year filter and grouping keys, per the spec's §3 Technical Decisions on year-boundary handling.

**2. ExpenseService Category-Totals Effective-Date Rework** - Add the equivalent helper and apply it to `GetCategoryTotalsByMonth` only, leaving `GetExpensesByMonth`/`GetUnpaidCardChargesByMonth` on their existing `ChargeDate ?? Date` rule, per the spec's §1 scope boundary.

### Stage 2: Test Coverage

**3. AnnualSummaryService Test Coverage** - Add the unpaid/settled/bank grouping tests and the December-charge-invoiced-in-January year-boundary test described in the spec's §7 Testing Strategy.

**4. ExpenseService Test Coverage** - Add the mirrored unpaid/settled/bank/no-double-counting tests for `GetCategoryTotalsByMonth`.

### Stage 3: Full Verification

**5. Full Solution Build and Test Pass** - Build and test every affected project to confirm the regrouping introduces no regressions in the Monthly page or Annual Summary reporting elsewhere in the solution.
