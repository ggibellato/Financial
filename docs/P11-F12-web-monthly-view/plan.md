# Implementation Plan: F12. Web — Monthly View

**Prerequisites:**
- F03 (Expense CRUD) and F04 (per-card outstanding totals) backend endpoints already exist and are tested
- F11's CashFlow app shell and routing already exist, with a `Monthly` placeholder route to replace

### Stage 1: API Client Extensions

**1. DTO types** - Add the typed request/response shapes for expenses, category totals, and card statements to the shared `types.ts`, matching the backend DTOs field-for-field.

**2. Client methods** - Add the 7 client methods (get expenses by month, get category totals, create/update/delete expense, get card statements by month, mark statement paid) to the shared `financialApiClient`, following the existing method style used by F13-F16's endpoints.

### Stage 2: Monthly View State and Page

**3. useMonthly hook** - Add the state hook covering month selection (defaulting to the current month), fetching expenses/category-totals/card-statements together, and the create/edit/delete-expense and mark-paid actions with their own loading/error state, re-fetching after each successful mutation.

**4. MonthlyPage component** - Add the presentational page: a month picker, a category-totals summary, a card-statements summary with the combined adjustment figure and a per-card "mark paid" action, an inline add-expense form, and the expense list with per-row inline edit and delete.

**5. Routing** - Replace the `Monthly` placeholder route in `main.tsx` with the new `MonthlyPage`.

### Stage 3: Verification

**6. Full-suite validation** - Run the web app's lint, typecheck, and test suite, confirming no regression to the existing CashFlow or Investments views.

**7. Manual verification** - Run the app against the local API and walk through selecting a month, creating/editing/deleting an expense, and marking a card statement paid, confirming the totals and adjustment figure update correctly.
