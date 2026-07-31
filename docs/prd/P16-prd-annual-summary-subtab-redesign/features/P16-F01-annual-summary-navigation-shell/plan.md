# Implementation Plan: Annual Summary Navigation Shell

**Prerequisites:**
- None — this feature only touches existing frontend files (`Financial.Web/src/pages/AnnualSummaryPage.tsx`, `AnnualSummaryPage.css`, `pages/__tests__/AnnualSummaryPage.test.tsx`); no new dependencies, environment variables, or configuration.

### Stage 1: Tab Shell Foundation

**1. Tab State and Type Definitions** - Add a `AnnualSummaryTabId` union and a `TABS` array of `{id, label}` to `AnnualSummaryPage.tsx`, plus `useState` for the active tab defaulting to Category Totals, following the same shape as `MonthlyPage.tsx`'s tab-state definitions.

**2. Tab Button Row** - Render a row of tab buttons below the existing year picker header, each switching the active tab on click and visually reflecting which tab is currently active, reusing the button/active-class pattern already established by `MonthlyPage`.

**3. Tab Bar Styling** - Add the CSS rules for the tab row and its active/inactive button states to `AnnualSummaryPage.css`, visually matching the styling already defined for `MonthlyPage`'s tab bar.

### Stage 2: Content Relocation and Test Coverage

**4. Relocate Existing Content Behind Tab Guards** - Wrap the current Category Totals section and Income Summary section together in a conditional guarded by the Category Totals tab, and the current Investment Diffs section in a conditional guarded by the Investments tab, with no other change to their existing markup or behavior, so exactly one tab's content renders at a time.

**5. Preserve Shared Loading/Error State** - Confirm the existing loading and error states keep rendering above/instead of the tab content regardless of which tab is active, since they reflect the one data fetch shared by both tabs.

**6. Update Test Coverage** - Extend the Annual Summary test suite with coverage for the default active tab, tab switching, active-tab visual state, and the independence of year selection from tab state, and add a tab-click step to the existing test that interacts with Investment Diffs content.
