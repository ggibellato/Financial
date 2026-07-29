# Implementation Plan: F05. Category Totals & Investments Tabs Consume New Endpoints

**Prerequisites:**
- .NET solution and frontend build pass on `main` (F01-F04 already merged)
- No new NuGet/npm packages, environment variables, or configuration files required
- Branch `feat/P19-F05-category-totals-investments-tabs-consume-new-endpoints`, already created from `main`

### Stage 1: Backend Route and Dead-Code Removal

**1. Remove Old Controller Actions** - Delete the `expense-categories`, `income-summary`, and `investment-diffs` actions/routes from `AnnualSummaryController`.

**2. Trim the Service Interface** - Remove `GetCategoryTotalsForYear`, `GetIncomeSummaryForYear`, and `GetInvestmentDiffsForYear` from `IAnnualSummaryService` (no longer called by any Presentation-layer consumer).

**3. Delete the Superseded Investment Method and DTO** - Delete `GetInvestmentDiffsForYear` from `AnnualSummaryService` and delete `InvestmentDiffsAnnualDTO` entirely — both fully superseded by `GetInvestmentAnnualResultForYear`/`InvestmentAnnualResultDTO`. `GetCategoryTotalsForYear`/`GetIncomeSummaryForYear` stay as-is (still genuinely reused internally).

### Stage 2: Backend Test Migration

**4. Port the Investment Diffs Test Suite** - Rename and retarget every `GetInvestmentDiffsForYear_*` unit test to call `GetInvestmentAnnualResultForYear` instead, preserving each scenario's coverage of the shared account/diff computation; remove the now-redundant F03-added tests that only existed to compare against the now-deleted method.

**5. Add Route-Removal and Retarget Integration Tests** - Replace the old `GetExpenseCategoryTotals_*`/`GetIncomeSummary_*`/`GetInvestmentDiffs_*` integration tests with three `404` tests for the removed routes.

### Stage 3: Frontend Contract Update

**6. API Types and Client** - Add `CategoryTotalsAnnualDto`/`InvestmentAnnualResultDto` to `types.ts` and the corresponding `financialApiClient` methods; remove the now-unused `InvestmentDiffsAnnualDto` and the three old client methods.

### Stage 4: Frontend Hook and Page Migration

**7. Rewrite `useAnnualSummary`** - Fetch `category-totals` and `investment-annual-result` instead of the three old endpoints; populate all Category Totals tab fields directly from the `category-totals` response with no client-side computation; rename the exposed investments field to `investmentAnnualResult`.

**8. Update `AnnualSummaryPage`** - Rename `investmentDiffs` references to `investmentAnnualResult`; no other rendering change.

### Stage 5: Frontend Test Migration

**9. Update Hook and Page Tests** - Retarget mocks to the two new endpoints; drop the client-computation-specific hook tests (no longer applicable); update Resultado/Total despesas expectations to the values the mock declares directly rather than a recomputed formula.

### Stage 6: Full Suite Verification

**10. Backend and Frontend Suites** - Run `dotnet test`, `npm run lint`, `npm test`, and `npm run build`, confirming no regressions across either stack — this is the first PR in the P19 sequence where old routes are actually gone, so this is the first point the CI browser smoke test exercises the fully-migrated state end to end.
