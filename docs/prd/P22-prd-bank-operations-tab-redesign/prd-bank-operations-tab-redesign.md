# Bank Operations Tab Redesign

## 1. Executive Summary

This feature redesigns how bank transfer and balance-correction operations are presented within the Financial app's Monthly view, across both of its frontends: the React web app (Financial.Web) and the WPF desktop app (Financial.App). Financial is a self-hosted personal finance tracker, installed as a single copy per user, that manages UK and Brazil bank balances, expenses, income, and investments.

Today, the Monthly/Summary tab's Banks grid mixes two concerns: showing each bank's balance, and letting the user expand a row to see its transfer/adjustment history and trigger "Move Money" or "Correct Balance" actions inline. This redesign separates those concerns. The Summary tab's Banks grid becomes a plain, read-only balance overview. A new "Bank" tab, sibling to the existing Summary/Expense/Income tabs, becomes the dedicated home for bank operations: two entry-point actions ("New Transfer" and "New Balance Correction") and a single flat list of every transfer and balance adjustment recorded for the selected month, across all banks, filterable down to one bank at a time.

The feature reuses the domain model and API/service surface already established by prior work (bank transfers and balance reconciliation, and WPF cash-flow parity) without introducing any new backend concepts. It ships as two independent, parallel features — one per frontend — so that Web and WPF reach the same redesigned experience without depending on each other's implementation timeline.

## 2. Problem and Opportunity

**The Problem**

- **Cluttered Summary view**: the Banks grid combines a balance overview with expandable per-row history tables and two action buttons per row, pushing the Cards and Income grids further down the page and forcing the user to expand a row just to see whether anything happened that month.
- **No cross-bank visibility**: history can only be viewed one bank at a time (via expand); there is no single view of "everything that happened to my money this month" across all accounts.
- **No filtering**: the user cannot narrow activity down to one bank while still being able to see others without collapsing/expanding rows one by one.
- **Broken WPF presentation**: the WPF app's current transfer/balance-correction UI does not work correctly today, breaking parity between the desktop and web experiences the user relies on interchangeably.

**The Opportunity**

- Splitting "view balances" (Summary tab) from "operate on balances" (new Bank tab) gives each view a single responsibility: Summary answers "where do I stand right now," Bank answers "what moved and let me move more."
- A flat, month-scoped, bank-filterable operations list directly solves the cross-bank visibility and filtering gaps, replacing N expandable per-bank tables with one list and one dropdown.
- Rebuilding the WPF side fresh (rather than patching the broken version) restores Web/WPF parity in one pass, using the already-proven Web design as the reference.

## 3. Target Audience

### Primary Users

**Self-Hosted Personal Finance Owner**
- Runs a personal instance of the Financial app to track UK and Brazil bank balances, expenses, income, and investments; the app has exactly one user per installation.
- Performs occasional bank operations — transfers between their own accounts, manual balance corrections after reconciling against a real bank statement — infrequently, but wants them fast to record and easy to review across all accounts at once.
- Switches between the web app and the WPF desktop app depending on device and context, and expects both to behave consistently.

## 4. Objectives

**Product Objectives**

- **Simplify** the Monthly/Summary Banks grid down to a pure balance overview with no interactive controls beyond the existing month picker.
- **Consolidate** all bank operations — transfers and balance corrections — into a single, filterable, cross-bank list.
- **Preserve** full create/edit/delete capability for both operation types through the redesign, with no functional regression versus the current Web implementation.
- **Achieve parity** between the Web and WPF frontends for this feature, including matching filter behavior and entry points.
- **Avoid backend changes**, keeping this a Presentation-layer-only redesign that introduces no new API endpoints or domain entities.

**Success Metrics**

- The Summary Banks grid renders zero expand controls and zero action buttons, verified visually and via component tests, measured immediately after the redesign ships.
- The Bank tab's operations list surfaces 100% of the selected month's transfer and adjustment records in one view, with a one-click filter to any single bank, verified by test coverage comparing list contents against the underlying API data.
- Create, edit, and delete remain available for both Transfers and Balance Adjustments in both frontends, verified by acceptance criteria in F01 and F02 passing with no regression against existing P20/P21 test suites.
- F01 and F02 both pass their acceptance criteria independently, with 0 shared code changes required between them beyond each frontend's own Presentation layer.
- 0 new API endpoints, controllers, or domain entities are added, verified by code review against the existing P20 Application/Infrastructure layers remaining untouched.

## 5. User Stories

### F01. Web Bank Operations Tab
- As a user, I want the Summary tab's Banks grid to show just bank name, balance, and round-up total so that I can quickly scan my balances without clutter.
- As a user, I want a dedicated "Bank" tab so that I know exactly where to go to move money or correct a balance.
- As a user, I want to click "+ New Transfer" and pick source/destination banks inline so that I can record money moved between my accounts.
- As a user, I want to click "+ New Balance Correction", pick a bank, see its current calculated balance, and enter the real balance so that I can reconcile against my bank statement.
- As a user, I want to see a single list of every transfer and balance correction for the selected month, across all my banks, so that I don't have to expand each bank individually.
- As a user, I want to filter that list down to one bank so that I can review just that account's activity.
- As a user, I want to edit or delete a past transfer or balance correction directly from the list so that I can fix mistakes without hunting through per-bank views.

### F02. WPF Bank Operations Tab
- As a user, I want the WPF Monthly/Summary Banks grid to show just bank name, balance, and round-up total, matching the web app, so that both frontends feel consistent.
- As a user, I want a dedicated "Bank" tab in the WPF app so that I have the same operations workflow as the web app.
- As a user, I want to record a transfer between banks from the WPF Bank tab so that I can manage my accounts from the desktop app.
- As a user, I want to record a balance correction from the WPF Bank tab, picking the bank first and seeing its current calculated balance, so that reconciliation works the same way as on the web.
- As a user, I want to see and filter a combined list of transfers and balance corrections by bank in the WPF app so that I have full parity with the web experience.
- As a user, I want to edit or delete a past transfer or balance correction from the WPF Bank tab's list so that corrections are just as easy on desktop as on web.

## 6. Functionalities

### F01. Web Bank Operations Tab

**Capabilities:**
- Summary tab's Banks grid displays exactly Bank, Balance, and Round-Up columns plus a totals row; no expand affordance, no action buttons, no per-row click behavior.
- A "Bank" tab is added as a 4th entry in the Monthly page's tab strip (Summary, Expense, Income, Bank), after the existing tabs.
- Two entry-point buttons render at the top of the Bank tab: "+ New Transfer" and "+ New Balance Correction".
- The "+ New Balance Correction" form requires a bank to be selected before its remaining fields (date, target balance, note) activate; once selected, the form shows that bank's current calculated balance, re-fetched/re-computed if the selected bank changes.
- The operations list combines Transfers and Balance Adjustments dated within the currently selected month (the same month picker shared with Summary/Expense/Income), sorted newest-date-first.
- The bank filter is a single-select dropdown offering "All Banks" (default, no filter) plus each configured bank name; changing it re-filters the already-fetched list client-side with no additional network request.
- Filter matching: a Transfer row matches the selected bank if it equals `sourceBank` OR `destinationBank`; an Adjustment row matches if the selected bank equals its `bank`.
- Each list row shows: Date, Type ("Transfer" or "Adjustment"), Bank(s) — "{sourceBank} → {destinationBank}" for transfers, the single bank name for adjustments — Amount/Delta (signed for adjustments), Note (or blank), and working Edit/Delete controls.
- No new API endpoints: reuses the existing month-scoped transfers endpoint (all banks in one call) and the existing per-bank adjustments endpoint (one call per known bank), combined client-side into one flat, filterable array — the same data-fetch shape used today, reshaped from a per-bank grouping into a single list.

**Experience:**
- Summary tab: the Banks grid renders immediately as a static table reflecting the selected month's balances; the only interactive control on the page remains the existing month picker.
- Bank tab: the two action buttons render at top, followed by the bank filter dropdown, followed by the operations list (or its empty state).
- New Transfer: clicking "+ New Transfer" opens the existing Transfer form inline with source/destination dropdowns, amount, date (defaulted to today), and note; saving creates the transfer, closes the form, and refreshes both the Summary balances and the Bank tab's list.
- New Balance Correction: clicking "+ New Balance Correction" opens a form where only the Bank dropdown is initially enabled; selecting a bank reveals "Current calculated balance for {bank}: £{amount}" plus date (defaulted to today), target balance, and note; saving shows the existing "Balance Corrected" confirmation with the resulting delta, then closes and refreshes.
- Filtering: selecting a bank instantly narrows the visible rows with no loading state (data is already fetched); selecting "All Banks" restores the full list.
- Editing: clicking the edit icon on a row opens the corresponding form pre-filled with that entry's data, exactly as today's per-bank history edit flow does; for adjustments, the bank field is fixed and not re-selectable.
- Deleting: clicking the delete icon shows a confirmation prompt, then removes the entry and refreshes the list and Summary balances on confirm.
- Switching away from the Bank tab while a create/edit form is open cancels the open form, matching the existing tab-switch behavior already used for the Expense/Income forms.

**Error Handling:**
- Fetching the month's transfers/adjustments fails: the Bank tab shows an error state with a retry action; no partial or stale list is shown.
- Saving a transfer or balance correction fails (validation or server error): the open form shows the existing inline field-level or general error message, stays open, and retains entered values.
- The "+ New Balance Correction" form's Save action is disabled until a bank has been selected; no request is sent without one.
- Deleting a transfer or adjustment fails (e.g. already removed by a prior action): an inline error message appears above the list and the list is refreshed to reflect actual server state.

### F02. WPF Bank Operations Tab

**Capabilities:**
- Mirrors F01's capabilities in WPF terms: the Summary sub-tab's Banks grid is trimmed to Bank, Balance, and Round-Up columns only, with no expand column and no action columns.
- A 4th `TabItem` ("Bank") is added to the Monthly view's tab control, after the existing Summary/Expense/Income tabs, hosting a new dedicated Bank section view.
- The Bank tab exposes two buttons ("Move Money", "Correct Balance") that open with no bank pre-selected, replacing today's per-row-triggered versions.
- The Transfer form is reused unchanged — it already supports inline source/destination bank selection.
- The Balance Correction form gains a bank-selection control as its first field; the remaining fields (date, target balance, note) activate only once a bank is chosen, at which point the form displays that bank's current calculated balance using the same balance-calculation logic the Summary grid uses.
- A new flat, filterable operations collection combines Transfers and Balance Adjustments (reshaped from the current per-bank grouping into one list), with a bank-filter selection defaulting to "All Banks", using the same source/destination-OR / single-bank matching semantics as F01.
- The operations list is scoped to the same selected month as the other Monthly sub-tabs, sorted newest-first.
- Edit and delete actions are available per row, rebound to the new flat list instead of the previous per-bank expandable grid rows.
- No new service-layer or API methods: reuses the existing transfer/balance-adjustment service calls and bank-balance lookup already available to the Monthly view.

**Experience:**
- Mirrors F01's experience end-to-end, adapted to WPF's command/binding model: Summary sub-tab shows a static balances table; Bank tab shows the two action buttons, a bank filter control, and the operations list (or empty state) below.
- Move Money, Correct Balance (bank-picker-first), filtering, editing, and deleting behave identically to F01's Experience description, laid out following the existing reference row pattern already used in this app's forms (error text kept in its own row, not overlapping form fields).

**Error Handling:**
- Mirrors F01: a failed fetch shows an inline error state with retry; a failed save keeps the form open with the existing validation error display; the Correct Balance form's save action stays disabled until a bank is chosen; a failed delete shows an inline error message and the list is refreshed to reflect actual server state.

## 7. Out of Scope

**Backend / API**
- No new API endpoints, controllers, or query parameters — all combining and filtering of transfers/adjustments happens client-side over the existing endpoints.
- No changes to the Transfer or Balance Adjustment domain entities, the bank balance calculation engine, or any existing Application-layer service contracts.

**Carried-over scope boundaries**
- No "all-time" history view — the operations list stays scoped to the currently selected month, matching today's behavior.
- No bulk edit or bulk delete of multiple operations at once.
- No CSV or other export of the operations list.

**WPF**
- No investigation or root-cause fix of the previously reported "not working" WPF transfer/adjustment behavior — F02 replaces the existing embedded UI wholesale rather than patching it.
- No changes to the WPF Expense or Income sub-tabs beyond removing/relocating the Banks grid's action columns.

**Cross-cutting**
- No new bank-management capability (adding, removing, or renaming banks) — the set of available banks is assumed to already exist in the app's current configuration.
- No additional frontend surfaces (e.g. mobile) beyond the existing Web and WPF apps.

## 8. Dependency Graph

| # | Feature | Priority | Dependencies |
|---|---------|----------|--------------|
| F01 | Web Bank Operations Tab | 1 | None |
| F02 | WPF Bank Operations Tab | 1 | None |

### Execution Waves
Features within the same wave can be built in parallel. A wave starts only after every feature in earlier waves is complete.

- **Wave 1**: F01, F02

### Priority levels
- **1** = Essential — product does not work without it
- **2** = Important — significant value addition
- **3** = Desirable — incremental improvement

```mermaid
graph TD
  F01[Web Bank Tab]
  F02[WPF Bank Tab]
```

## 9. Acceptance Criteria

### F01. Web Bank Operations Tab
- [x] Summary tab's Banks grid renders only Bank, Balance, and Round-Up columns (plus totals row), with no expand control and no action buttons.
- [x] A "Bank" tab appears in the Monthly page's tab strip after Summary/Expense/Income.
- [x] Clicking "+ New Transfer" opens the existing Transfer form with source/destination selectable inline; saving creates the transfer, refreshes Summary balances, and adds the entry to the Bank tab's operations list.
- [x] Clicking "+ New Balance Correction" opens a form where only the Bank dropdown is initially enabled; other fields activate only after a bank is chosen, and the form then displays that bank's current calculated balance.
- [x] Saving a balance correction opened from the generic entry point updates the correct bank's balance and shows the resulting delta, matching today's confirmation behavior.
- [x] The operations list shows every Transfer and Balance Adjustment dated within the selected month, across all banks, sorted newest-first.
- [x] Each list row displays Date, Type, Bank(s) involved, Amount/Delta, Note, and working Edit/Delete controls.
- [x] Selecting a bank in the filter dropdown narrows the list to Transfers where that bank is source or destination, and Adjustments for that bank; selecting "All Banks" restores the full list, with no additional network request on filter change.
- [x] Editing a row opens the corresponding form pre-filled with that entry's data; for adjustments, the bank field is fixed and not editable.
- [x] Deleting a row prompts for confirmation, then removes the entry from the list and updates Summary balances on confirm.
- [x] When the list is empty, it shows "No transfers or balance corrections this month." (unfiltered) or the equivalent filtered-by-bank message, instead of an empty table.
- [x] A failed fetch of the month's operations shows an error state with a working retry action.
- [x] A failed save shows the existing inline field-level or general error message without closing the form or discarding entered values.
- [x] A failed delete shows an inline error message and the list reflects actual server state afterward.

### F02. WPF Bank Operations Tab
- [x] Summary sub-tab's Banks grid renders only Bank, Balance, and Round-Up columns, with no expand control and no action buttons.
- [x] A "Bank" tab appears in the Monthly view's tab control after Summary/Expense/Income.
- [x] Clicking "Move Money" opens the Transfer form with source/destination banks selectable inline; saving creates the transfer, refreshes Summary balances, and adds the entry to the Bank tab's operations list.
- [x] Clicking "Correct Balance" opens a form where only the bank picker is initially enabled; other fields activate only after a bank is chosen, and the form then displays that bank's current calculated balance.
- [x] Saving a balance correction opened from the generic entry point updates the correct bank's balance and shows the resulting delta.
- [x] The operations list shows every Transfer and Balance Adjustment dated within the selected month, across all banks, sorted newest-first.
- [x] Each list row displays Date, Type, Bank(s) involved, Amount/Delta, Note, and working Edit/Delete controls.
- [x] Selecting a bank in the filter control narrows the list using the same source/destination-OR matching semantics as F01; selecting "All Banks" restores the full list.
- [x] Editing a row opens the corresponding form pre-filled with that entry's data; for adjustments, the bank field is fixed.
- [x] Deleting a row prompts for confirmation, then removes the entry from the list and updates Summary balances on confirm.
- [x] When the list is empty, it shows empty-state text equivalent to F01, scoped to the active filter.
- [x] A failed fetch shows an inline error state consistent with the existing WPF Monthly view error handling.
- [x] A failed save keeps the form open and shows the existing validation error display.
- [x] A failed delete shows an inline error message and the list reflects actual server state afterward.

### Cross-Feature Integration
F01 and F02 are independent, parallel implementations of the same redesign on two separate frontends. Neither declares a Consumes dependency on the other — each consumes only its own frontend's already-existing endpoints and services directly — so no cross-feature integration criteria apply between them.
