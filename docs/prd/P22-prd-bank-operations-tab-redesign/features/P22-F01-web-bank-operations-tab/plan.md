# Implementation Plan: F01. Web Bank Operations Tab

**Prerequisites:**
- Existing Financial.Web toolchain (Vite, TypeScript, Vitest, React Testing Library) already set up
- No new libraries, environment variables, or configuration
- Existing endpoints only: `GET/POST/PUT/DELETE /transfers*`, `GET/POST/PUT/DELETE /banks/{bankName}/adjustments*`, `GET /banks`, `GET /banks/month/{year}/{month}/balances`

### Stage 1: Simplify the Summary Banks Grid

**1. Trim BanksGrid to a read-only balances table** - Remove the expand/collapse affordance, the nested per-bank history table, and the two per-row action buttons from `BanksGrid`, leaving only the Bank/Balance/Round-Up columns and the totals row. Drop the props and internal state this behavior required.

**2. Prune BanksGrid styling** - Remove the now-unused expand-button, action-button, and history-table CSS rules, keeping only what the read-only table needs.

### Stage 2: Flat, Filterable Operations Data Layer

**3. Build the combined operations hook** - Introduce a new hook that fetches the month's transfers (all banks) and each bank's adjustments (filtered to the selected month), combines them into one flat array of operations sorted newest-first, and replaces the per-bank grouped hook it supersedes. Include delete actions and a retryable error/loading state, per the spec.

**4. Add bank-filter behavior to the new hook** - Add filter state (defaulting to "All Banks") and the source/destination-OR / bank-equality matching rule described in the spec, applied client-side with no extra network calls.

**5. Retire the superseded hook** - Remove the old per-bank grouped hook and update any leftover references to it (e.g. type imports used only for fixtures).

### Stage 3: Balance Correction's Bank-First Flow

**6. Add the Bank dropdown to BalanceAdjustmentForm** - Render a bank selector as the form's first field; gate the current-balance reference line and the date/target-balance/note fields so they only appear once a bank is chosen; keep Save disabled until then; render the bank as fixed text instead of a dropdown while editing.

**7. Support deferred bank selection in useBalanceAdjustmentForm** - Allow opening the create form with no bank pre-selected, and resolve the chosen bank's current calculated balance from already-fetched bank totals as the user picks one, with no new network request.

### Stage 4: Bank Tab Assembly

**8. Build the Bank tab's section component** - Create the component that renders the two entry-point buttons, the bank-filter dropdown, and the operations list with per-row Edit/Delete, including the unfiltered and filtered-by-bank empty states described in the spec.

**9. Style the new section** - Add the CSS needed for the Bank tab's header, filter control, and list, following this app's existing table/section conventions.

**10. Wire the Bank tab into MonthlyPage** - Add "Bank" as the 4th tab; mount the new section, `TransferForm`, and `BalanceAdjustmentForm` under it; connect the new operations hook; scope everything to the page's existing month picker; extend the tab-switch-cancels-open-form behavior to the Bank tab, matching the Expense/Income pattern.

**11. Wire cross-tab refresh** - Ensure a successful transfer or balance-correction save/delete refreshes both the Summary tab's balances and the Bank tab's operations list, matching the current app's refresh behavior for these operations.

### Stage 5: Verification

**12. Add and update unit and integration tests** - Rewrite `BanksGrid.test.tsx` for the read-only grid; add tests for the new operations hook and the new Bank tab section component; update `BalanceAdjustmentForm.test.tsx`/`useBalanceAdjustmentForm.test.ts` for the bank-first flow; update `MonthlyPage.test.tsx` for the Bank tab end-to-end, covering the scenarios in the spec's Testing Strategy and PRD Section 9's F01 acceptance criteria.

**13. Full frontend build and test pass** - Run `tsc -b --noEmit` and the Vitest suite for `Financial.Web`, confirming zero regressions against the existing test suite.
