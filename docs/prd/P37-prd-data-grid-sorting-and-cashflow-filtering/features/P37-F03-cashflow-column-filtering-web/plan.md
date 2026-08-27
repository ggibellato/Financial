# Implementation Plan: F03. CashFlow Column Filtering — Web

**Prerequisites:**
- `Financial.Web` dev environment (already configured per repo `README.md`)
- No new libraries — `Popover`, `Checkbox`, and `SearchBox` are all already part of `@fluentui/react-components` (ADR-004); this is their first use in the codebase

### Stage 1: Shared Filter Infrastructure

**1. Column Filter Hook** - Build `useColumnFilters`: computes each filterable column's distinct available values from the full unfiltered dataset, tracks which values are checked per column, filters rows by the AND of every active column (and, within a column, the OR of a row's values when its accessor returns more than one), and exposes toggle/clear actions.

**2. Column Filter Menu Component** - Build `ColumnFilterMenu`: a filter icon that visibly indicates an active filter, opening a Fluent `Popover` with an "(All)" checkbox, a checklist of the column's available values, and a search box shown only past the 10-value threshold. Designed to render inside `SortableColumnHeader`'s existing `children` slot from F01.

**3. Component and Hook Tests** - Cover the hook's toggle/clear/multi-column/array-accessor behavior and the menu's checklist rendering, search-box threshold, and active-icon state.

### Stage 2: Grid Integration

**4. Shared Totals Grid Filtering** - Add an optional per-column filter slot to `TotalsGrid`, then wire `useColumnFilters` + `ColumnFilterMenu` into `BanksGrid` (Bank) and `CategoryTotalsGrid` (Category), composing in front of each grid's existing F01 sort.

**5. Income and Expenses Grids** - Wire `useColumnFilters` into `IncomeSection` (Bank) and `ExpensesSection` (Category and Card, combined with AND).

**6. Cards Grid** - Wire `useColumnFilters` into `CardsGrid` (Card).

**7. Bank Operations Grid Refactor** - Replace `BankOperationsSection`'s single-select `<select>` Bank filter with the new header-based checklist: strip the filtering logic out of `useBankOperations` (it now returns every fetched operation), move filtering into the component via an array-accessor that captures both banks on a transfer row, and drop the two now-removed props from `MonthlyPage`'s call site.

**8. Annual Summary Tabs** - Wire `useColumnFilters` for Category into the Category Totals and Historic Summary Average tabs of `AnnualSummaryPage`, composing in front of their existing F01 sort hooks; the Investments tab is untouched (no in-scope column).

**9. Integration Tests** - Extend every touched grid's test file with filter-specific assertions; rewrite `BankOperationsSection`'s and `useBankOperations`'s filter-related tests for the new shape; verify `MonthlyPage`'s existing tests still pass with the dropped props; add the sort-and-filter-coexist test for the PRD's Cross-Feature Integration criterion.

### Stage 3: Verification and Polish

**10. Cross-Grid Manual Pass** - Run the app locally and exercise every in-scope grid's filter menu — single-column, multi-column, the >10-value search box (Category typically qualifies), the empty-result message, and the one-action clear — confirming behavior matches the PRD's Experience description end to end, including alongside F01's sorting on the same header cell.

**11. Accessibility Check** - Confirm the filter icon and its `Popover` are keyboard-operable (reachable via Tab, opens on Enter/Space, checklist items are focusable and toggleable via keyboard, `Popover` dismisses on Escape) per the project's WCAG 2.2 AA baseline.

**12. Full Verification** - Run the complete `Financial.Web` test suite, lint, and build (`npm test`, `npm run lint`, `npm run build`) to confirm no regressions across every touched grid and the `useBankOperations`/`BankOperationsSection`/`MonthlyPage` refactor.
