# Implementation Plan: F06 — Portfolio Navigator: Credits Tab

**Prerequisites:**
- F02 implemented and merged (`SelectedNodeContext` available) ✅
- All credit DTOs and API client methods exist in `types.ts` and `financialApiClient.ts` ✅
- `CreditsController` and `CreditService` already exist on the backend ✅
- `LoadingState` and `ErrorState` shared components available ✅
- Recharts v3.8.1 already installed ✅

---

### Phase 1: Hook — `useCredits`

**1. State, actions, and reducer** — Create `Financial.Web/src/hooks/useCredits.ts`. Define the
full state shape: credits array, loading/error flags, retry counter, `selectedFilter`
(`FilterOption`), `selectedMode` (`ViewMode`), a `filterPersistence` Map keyed by selection key,
and inline form fields (`isFormVisible`, `editingId`, `formDate`, `formType`, `formValue`,
`isSaving`, `saveError`, `deleteError`). Define the discriminated union of actions and implement
the reducer handling all state transitions — including reset on node change, fetch lifecycle, form
actions, and filter/mode updates with persistence.

**2. Data fetching — all node types** — Wire a `useEffect` watching `SelectedNodeContext`. For
asset nodes call `getAssetDetails` and extract `AssetDetailsDto.credits`; for broker nodes call
`getCreditsByBroker`; for portfolio nodes call `getCreditsByPortfolio`. On each node change,
compute the selection key, look up saved preferences in the persistence Map, and restore them (or
apply defaults `last-year` + `Stacked` if unseen). Reset credits and form state when node is null.
Include `retryCount` as a `useEffect` dependency so `retry()` triggers a re-fetch.

**3. Filter/mode and persistence** — Implement `setFilter` and `setMode` actions that update the
active filter/mode in reducer state and simultaneously write the new preference into the
persistence Map under the current selection key. Implement a `buildSelectionKey(node)` pure
function that returns `Asset|{b}|{p}|{n}`, `Portfolio|{b}|{n}`, or `Broker|{n}`.

**4. Derived chart data** — Using `useMemo` in the hook body (not in the reducer), derive
`filteredCredits` by applying the active `FilterOption` against the current date, then derive
`chartData` (`MonthBucket[]`) by grouping filtered credits by month (MM/yyyy) and summing values
per credit type. Sort buckets chronologically so the X-axis is ordered oldest-to-newest. Return
both derived values as part of the hook interface.

**5. Mutation operations** — Implement `showNewForm` (opens blank form with `formType: 'Dividend'`
as default), `showEditForm` (populates fields from `CreditDto`, converts ISO date to `yyyy-MM-dd`
for the date input), `cancelForm` (hides and resets the form), `setFormField` (updates individual
field values), `saveForm` (validates date required and value > 0, calls `addCredit` or
`updateCredit` depending on `editingId`, updates credits from the returned `AssetDetailsDto` on
success, hides form), and `deleteCredit` (prompts via `window.confirm`, calls API `deleteCredit`,
updates credits on success). Return the full hook interface.

---

### Phase 2: Component and Styles

**6. `CreditsTab` component skeleton** — Create `Financial.Web/src/components/CreditsTab.tsx`.
Import `useCredits`, `LoadingState`, and `ErrorState`. Render the loading guard first, then the
error guard with retry callback. Confirm guards work correctly before adding further content.

**7. Filter and mode controls** — Render the filter toolbar above the main content area with 5
buttons ("This month", "Last 3 months", "Last 6 months", "Last year", "All") and 2 mode toggles
("Stacked", "Grouped"). Wire each to `setFilter`/`setMode`. Apply a `--active` CSS class modifier
to the currently selected option in each group.

**8. Inner horizontal split (asset) and chart-only layout (broker/portfolio)** — For asset nodes,
render a resizable horizontal split: credits section on the left (flex: 2 default) and chart
section on the right (flex: 1 default), separated by a draggable divider. Implement the drag
handle using `onMouseDown`/`onMouseMove`/`onMouseUp` on the document, storing the left panel
pixel width in component `useState`, following the same pattern as `SplitPanel.tsx`. For
broker/portfolio nodes, render the chart section full-width without the table or split.

**9. Credits table** — Inside the left panel (asset only), render the credits table with columns:
edit/delete icon buttons, Date, Type, Value. Format cells with local utility functions
(`formatDate`, `formatN2`). Apply type CSS classes (`credits-tab__type--dividend`,
`credits-tab__type--rent`). Apply a bold class to the Value cell. Wire the edit icon to
`showEditForm(credit)` and the delete icon to `deleteCredit(credit.id)`. Render a "New credit"
button in the toolbar above the table; clicking it calls `showNewForm()`.

**10. Inline form** — Render the add/edit form (above the table, when `isFormVisible` is true)
with controlled inputs: Date (`<input type="date">`), Type (`<select>` with Dividend/Rent
options), Value (`<input type="number" min="0" step="0.01">`). Show "New credit" or "Edit credit"
as the form title based on whether `editingId` is set. Wire Save to `saveForm()` (disabled +
label "Saving..." while `isSaving`), Cancel to `cancelForm()`. Render `saveError` below the form
when set and the form is visible. Render `deleteError` below the table when set.

**11. Bar chart** — Inside the right panel (all node types), render a Recharts
`ResponsiveContainer` containing a `BarChart` bound to `chartData`. For Stacked mode, both `Bar`
components (Dividend, Rent) share `stackId="credits"`. For Grouped mode, omit `stackId`. Format
X-axis ticks as MM/yyyy. Format Y-axis ticks with `formatN2`. Add `LabelList` on each bar
segment. Display "Credits by Month" as a heading above the chart container.

**12. Styles** — Create `Financial.Web/src/components/CreditsTab.css` with BEM classes covering:
outer container, filter toolbar and active modifier, mode toggles and active modifier, inner split
container, left/right panels, drag handle cursor/appearance, credits table, type colour modifiers
(blue family for both Dividend and Rent, visually differentiated), bold value column, chart
heading, chart container height, save and delete error message placements. Use CSS variable values
from `index.css` for colours and typography.

---

### Phase 3: Integration

**13. Wire into `DetailPanel`** — In `Financial.Web/src/components/DetailPanel.tsx`, replace the
credits tab placeholder blocks with `{activeTab === 'credits' && <CreditsTab />}` and add the
import statement. Node-type branching (table vs chart-only) is handled inside `CreditsTab` via
the hook, so `DetailPanel` requires no further changes.

---

### Phase 4: Tests

**14. Hook unit tests** — Create `Financial.Web/src/hooks/useCredits.test.ts` mocking
`createFinancialApiClient`. Cover: fetch lifecycle for all 3 node types (asset/broker/portfolio),
null node reset, retry, filter/mode state updates, persistence save and restore across
re-selections, default filter/mode on first visit, all form state transitions (new, edit, cancel,
field change), each mutation path (add success, update success, delete success), all error paths
(save error, delete error, validation blocks for date required and value > 0), and sorted credits
output. See spec Section 7 for the complete test function list.

**15. Component unit tests** — Create
`Financial.Web/src/components/__tests__/CreditsTab.test.tsx` mocking both `useCredits` and
`recharts`. Cover: loading state, error state, chart-only for broker/portfolio nodes, table + chart
for asset, date formatting (dd/MM/yyyy), N2 bold value, type CSS classes (dividend/rent), filter
button rendering and active state, mode toggle rendering and active state, click callbacks for
filter and mode, New button visibility (asset only), form visibility and titles, Save disabled
state, edit/delete icon callbacks, save/delete error messages, and empty table. See spec Section 7
for the complete test function list.
