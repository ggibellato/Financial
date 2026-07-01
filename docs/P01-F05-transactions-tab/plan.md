# Implementation Plan: F05 — Portfolio Navigator: Transactions Tab

**Prerequisites:**
- F02 implemented and merged (`SelectedNodeContext` available) ✅
- All transaction DTOs and API client methods already exist in `types.ts` and `financialApiClient.ts` ✅
- `TransactionsController` and `TransactionService` already exist on the backend ✅
- `LoadingState` and `ErrorState` shared components available ✅

---

### Phase 1: Hook — `useTransactions`

**1. State, actions, and reducer** — Create `Financial.Web/src/hooks/useTransactions.ts` and define the full state shape covering asset loading, form visibility, all form field values, save/delete error tracking, and a retry counter. Implement the reducer that handles all state transitions — reset on node change, fetch lifecycle, and every form action.

**2. Data fetching** — Wire a `useEffect` that watches the selected node from `SelectedNodeContext` and calls `getAssetDetails` when an asset node is selected, following the same trigger and cancellation pattern as `useAssetSummary`. Include `retryCount` as a dependency so the retry action causes a re-fetch.

**3. Form state actions** — Implement `showNewForm` (opens a blank form with type defaulting to Buy), `showEditForm` (populates fields from a `TransactionDto` and converts the ISO date to `yyyy-MM-dd` for the date input), `cancelForm` (hides and resets the form), and `setFormField` (updates individual field values).

**4. Mutation operations** — Implement `saveForm` (validates required fields, builds the correct DTO based on whether `editingId` is set, calls `addTransaction` or `updateTransaction`, then on success dispatches the returned `AssetDetailsDto` as `FETCH_SUCCESS` and hides the form) and `deleteTransaction` (prompts via `window.confirm`, calls `deleteTransaction` on the API client, and dispatches accordingly).

**5. Sorted output** — Derive `transactions` by sorting `asset.transactions` by date descending and expose it through the returned interface. Return the full hook interface including all state flags and action callbacks.

---

### Phase 2: Component and Styles

**6. `TransactionsTab` component skeleton** — Create `Financial.Web/src/components/TransactionsTab.tsx`. Import `useTransactions`, `LoadingState`, and `ErrorState`. Render the loading guard, error guard with retry callback, and the non-asset placeholder message. Confirm the guards work before building the table.

**7. Transaction table** — Render the full table with action icon columns followed by Date, Type, Quantity, Unit Price, Fees, and Total. Format each cell using the local utility functions (`formatDate`, `formatN8`, `formatN2`). Apply `transactions-tab__type--buy` (green bold) to Buy cells and `transactions-tab__type--sell` (red bold) to Sell cells. Wire the edit icon to `showEditForm` and the delete icon to `deleteTransaction`.

**8. Inline form** — Render the add/edit form above the table when `isFormVisible` is true. Include controlled inputs for all fields with the correct input types and step attributes from the spec. Show the form title as "New transaction" or "Edit transaction" based on `editingId`. Wire the Save button to `saveForm` with disabled state and "Saving..." label while `isSaving` is true. Wire Cancel to `cancelForm`.

**9. Error messages** — Render the `saveError` message below the form (when set and form is visible) and the `deleteError` message below the table (when set). Both should be styled as inline error text matching the project's colour scheme.

**10. Styles** — Create `Financial.Web/src/components/TransactionsTab.css` with BEM classes covering the toolbar row, form container and fields, table column widths, type colour modifiers, right-aligned amount columns, bold total column, and both error message placements. Match colour values (`#2e7d32` for green, `#c62828` for red) to the existing theme.

---

### Phase 3: Integration

**11. Wire into `DetailPanel`** — In `Financial.Web/src/components/DetailPanel.tsx`, replace the two transactions placeholder blocks (the "coming in F05" placeholder and the non-asset message) with a single `{activeTab === 'transactions' && <TransactionsTab />}` expression and add the import statement. The non-asset vs asset branching moves inside `TransactionsTab` via the hook.

---

### Phase 4: Tests

**12. Hook unit tests** — Create `Financial.Web/src/hooks/useTransactions.test.ts` mocking `createFinancialApiClient`. Cover the fetch lifecycle (initial state, fetches on asset node, resets on non-asset node, retry), sorted transactions output, all form state transitions (new, edit, cancel, field change), each mutation path (add success, update success, delete success), and all error paths (save error, delete error, validation block). See spec Section 7 for the full test function list.

**13. Component unit tests** — Create `Financial.Web/src/components/__tests__/TransactionsTab.test.tsx` mocking `useTransactions`. Cover all render states (loading, error, non-asset placeholder, empty table, populated table), column formatting (date `dd/MM/yyyy`, N8 quantity, N2 prices, bold total), type colour classes (Buy green, Sell red), form interactions (New button, form visibility, title, Save disabled state), and error message placements. See spec Section 7 for the full test function list.
