# Implementation Plan: Monthly Incoming Sub-Tab

**Prerequisites:**
- F01 (Monthly Tab Navigation Shell) merged — this feature only extracts a component F01 already scoped correctly to the Incoming tab.

### Stage 1: Extract the Income Form Component

**1. IncomeForm Component** - Move the `IncomeForm` local function, its option list (`INCOME_SOURCES`), and its field-mapping constants out of `MonthlyPage.tsx` into their own file, with no change to props, markup, or behavior.

**2. Wire MonthlyPage to the Extracted Component** - Update `MonthlyPage.tsx`'s Incoming tab block to import and render the extracted `IncomeForm`, removing the now-unused local definitions.

### Stage 2: Test Coverage

**3. Add IncomeForm Component Tests** - Add a dedicated test file covering the form's create/edit and gross-value-conditional rendering branches in isolation.

**4. Re-verify Incoming Tab Behavior** - Re-run the existing Incoming-tab tests in `MonthlyPage.test.tsx` to confirm New/Edit/Delete, the gross-value toggle, validation errors, and tab-switch form discarding all still pass unchanged against the extracted component.
