# Implementation Plan: F01. Sortable Columns — Web

**Prerequisites:**
- `Financial.Web` dev environment (`npm install`, Node/npm already configured per repo `README.md`)
- No new libraries — implemented with existing React, TypeScript, and `@fluentui/react-icons` (for the sort arrow glyph)

### Stage 1: Shared Sort Infrastructure

**1. Sort Hook** - Build the generic `useSortableRows` hook: it holds the active column/direction state, advances it through the unsorted → ascending → descending → unsorted cycle, and returns rows sorted via a caller-supplied per-column value accessor, with type-aware comparison and null values always sorting last.

**2. Sortable Header Cell Component** - Build the `SortableColumnHeader` presentational component: a full-cell clickable `<th>` that shows an ascending/descending arrow glyph when it's the active sort column, calls back into the hook's sort request on click, and exposes a slot for a future filter icon so F03 can compose into the same cell later.

**3. Component and Hook Tests** - Cover the hook's cycle transitions, type-aware comparisons (numeric, date, string), and null-last ordering; cover the header cell's click handling and glyph rendering per state.

### Stage 2: CashFlow Grid Integration

**4. Shared Totals Grid** - Wire sorting into `TotalsGrid`, the component shared by the Banks, Category Totals, and Incoming grids, using each grid's existing `columns` config as the accessor source.

**5. Transaction-Level CashFlow Grids** - Wire sorting into the Expenses, Income, and Bank Operations grids, each with an accessor map matching its columns.

**6. Remaining CashFlow Grids** - Wire sorting into the Cards grid, Controle Mãe, the Reserva Balances grid (not Movements, which is excluded per the PRD), Investment Snapshots, the Mensais bill tables, and the three Annual Summary tabs, keeping each grid's existing footer/total/spacer rows pinned outside the sortable row set.

**7. CashFlow Integration Tests** - Extend each touched grid's existing test file with sort-click assertions, and add a new test file for `TotalsGrid` itself since it currently has none; verify the Reserva Movements grid's headers remain non-interactive.

### Stage 3: Investment Grid Integration

**8. Transaction-Style Investment Grids** - Wire sorting into the Transactions, Price History, and Credits grids.

**9. Portfolio Summary Grid** - Wire sorting into the Portfolio Summary grid, the largest and most computed-column-heavy grid, ensuring every derived column (Current Value, Profit %, XIRR, and similar) gets a correct numeric accessor rather than sorting by its formatted display text.

**10. Current Values and Dividend Check Grids** - Wire sorting into the Current Values grid and both Dividend Check grids, replacing the latter's existing hardcoded sort with the interactive hook seeded at the same initial order.

**11. Investment Integration Tests** - Extend each touched grid's existing test file with sort-click assertions, with particular attention to Portfolio Summary's computed columns.

### Stage 4: Verification and Polish

**12. Cross-Grid Manual Pass** - Run the app locally and click through every in-scope grid's headers to confirm the 3-state cycle, arrow glyph, and pinned-row behavior all match the PRD's Experience description end to end.

**13. Accessibility Check** - Confirm each sortable header cell is keyboard-operable (reachable via Tab, activatable via Enter/Space) and exposes a visible focus indicator, per the project's WCAG 2.2 AA baseline.

**14. Full Verification** - Run the complete `Financial.Web` test suite, lint, and build (`npm test`, `npm run lint`, `npm run build`) to confirm no regressions across the ~19 touched grids.
