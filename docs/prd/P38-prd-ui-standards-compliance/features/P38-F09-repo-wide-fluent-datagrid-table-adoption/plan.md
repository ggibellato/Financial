# Implementation Plan: F09. Repo-wide Fluent DataGrid/Table Adoption

**Prerequisites:**
- F01-F07 merged (tokens, shared primitives, and the proven `Table`-primitive migration pattern from F04
  are all available).
- No new tools/libraries — `Table`/`TableHeader`/`TableRow`/`TableHeaderCell`/`TableBody`/`TableCell`
  are already part of the installed `@fluentui/react-components` package.
- This feature ships as **three separate PRs**, each its own branch off `main`, merged before the next
  starts — see spec.md Decision D1. Each stage below corresponds to one PR.

### Stage (a): Income + Transfer lists — PR 1

**1. Migrate `IncomeSection.tsx`** - Swap the native table markup for Fluent `Table` primitives per
spec.md Decision D3; swap the raw `✏` edit button for the `EditRegular` icon-button pattern.

**2. Migrate `BankOperationsSection.tsx`** - Same treatment: `Table` primitives, `EditRegular` icon swap.

**3. Test suite alignment (Stage a)** - Confirm existing sort/filter/action tests for both grids pass
after the migration; fix only what the DOM-shape change actually breaks.

### Stage (b): Investment Transactions + Credits grids — PR 2

**4. Migrate `TransactionsTab.tsx`'s grid** - `Table` primitives for the grid portion only (the file's
own embedded create/edit form stays untouched); `EditRegular` icon swap.

**5. Migrate `CreditsTab.tsx`'s grid** - Same treatment as `TransactionsTab.tsx`.

**6. Test suite alignment (Stage b)** - Confirm existing sort/filter/action tests for both grids pass
after the migration.

### Stage (c): Price History grid + Investment Snapshot grid — PR 3

**7. Migrate `PriceHistoryTab.tsx`'s grid** - `Table` primitives for the grid portion only; `EditRegular`
icon swap.

**8. Migrate `InvestmentSnapshotsPage.tsx`'s two tables** - `Table` primitives for both the value grid
and its totals-row table; `EditRegular` icon swap on the value grid's edit action.

**9. Test suite alignment (Stage c) and manual verification** - Confirm existing sort/filter/action tests
for both grids pass after the migration. Manually verify all six migrated grids across both stages'
prior PRs plus this one, on Web, per `docs/ui/review-checklist.md` — sort, filter, row actions, keyboard
navigation — since this final stage is where F09's PRD acceptance criteria are confirmed and checked off
as a whole.
