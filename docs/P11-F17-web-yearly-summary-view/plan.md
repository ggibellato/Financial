# Implementation Plan: F17. Web — Yearly Summary View

**Prerequisites:**
- F09 (yearly expense-category totals and month-over-month investment-account diffs) backend endpoints already exist and are tested
- F11's CashFlow app shell and routing already exist, with a `Yearly Summary` placeholder route to replace

### Stage 1: API Client Extensions

**1. DTO types** - Add the typed response shapes for category yearly totals and investment yearly diffs to the shared `types.ts`, matching the backend DTOs field-for-field.

**2. Client methods** - Add the 2 client methods (get category totals for year, get investment diffs for year) to the shared `financialApiClient`, following the existing method style used elsewhere.

### Stage 2: Yearly Summary View State and Page

**3. useYearlySummary hook** - Add the state hook covering year selection (defaulting to the current year) and fetching both endpoints together, with loading/error state, re-fetching on year change.

**4. YearlySummaryPage component** - Add the presentational page: a year picker, the category-totals table (monthly columns plus a yearly total column), and the investment-diffs table (monthly value/diff columns per account plus the net-position row and its full-year net change).

**5. Routing** - Replace the `Yearly Summary` placeholder route in `main.tsx` with the new `YearlySummaryPage`.

### Stage 3: Verification

**6. Full-suite validation** - Run the web app's lint, typecheck, and test suite, confirming no regression to the existing CashFlow or Investments views.

**7. Manual verification** - Run the app against the local API and select a year, confirming both tables render real F09-computed figures correctly.
