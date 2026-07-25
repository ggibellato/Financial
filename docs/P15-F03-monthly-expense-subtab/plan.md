# Implementation Plan: Monthly Expense Sub-Tab

**Prerequisites:**
- F01 (Monthly Tab Navigation Shell) merged — this feature only extracts a component F01 already scoped correctly to the Expense tab.

### Stage 1: Extract the Expense Form Component

**1. ExpenseForm Component** - Move the `ExpenseForm` local function, its option lists (`CATEGORIES`, `CARDS`), and its field-mapping constants out of `MonthlyPage.tsx` into their own file, with no change to props, markup, or behavior.

**2. Wire MonthlyPage to the Extracted Component** - Update `MonthlyPage.tsx`'s Expense tab block to import and render the extracted `ExpenseForm`, removing the now-unused local definitions.

### Stage 2: Test Coverage

**3. Add ExpenseForm Component Tests** - Add a dedicated test file covering the form's create/edit, settled, and payment-mode/round-up rendering branches in isolation.

**4. Re-verify Expense Tab Behavior** - Re-run the existing Expense-tab tests in `MonthlyPage.test.tsx` to confirm New/Edit/Delete, the settlement note, bank/card mode switching, round-up handling, and tab-switch form discarding all still pass unchanged against the extracted component.
