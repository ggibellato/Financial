# Implementation Plan: Monthly Tab Navigation Shell

**Prerequisites:**
- None — this feature only touches existing frontend files (`Financial.Web/src/pages/MonthlyPage.tsx`, `MonthlyPage.css`, `pages/__tests__/MonthlyPage.test.tsx`); no new dependencies, environment variables, or configuration.

### Stage 1: Tab Shell Foundation

**1. Tab State and Type Definitions** - Add a `MonthlyTabId` union and a `TABS` array of `{id, label}` to `MonthlyPage.tsx`, plus `useState` for the active tab defaulting to Summary, following the same shape as `DetailPanel.tsx`'s tab-state definitions.

**2. Tab Button Row** - Render a row of tab buttons below the existing month/year picker header, each switching the active tab on click and visually reflecting which tab is currently active, reusing the button/active-class pattern already established by `DetailPanel`.

**3. Tab Bar Styling** - Add the CSS rules for the tab row and its active/inactive button states to `MonthlyPage.css`, visually matching the styling already defined for `DetailPanel`'s tab bar.

### Stage 2: Content Relocation and Test Coverage

**4. Relocate Existing Content Behind Tab Guards** - Wrap the current Summary grids block, Expense form/list block, and Incoming form/list block each in a conditional guarded by the active tab, with no other change to their existing markup or behavior, so exactly one block renders at a time.

**5. Preserve Shared Loading/Error State** - Confirm the existing loading and error states keep rendering above/instead of the tab content regardless of which tab is active, since they reflect the one data fetch shared by all three tabs.

**6. Update Test Coverage** - Extend the Monthly tab test suite with coverage for the default active tab, tab switching, active-tab visual state, and the independence of period selection from tab state, and add a tab-click step to every existing test that interacts with expense or income content.
