# Implementation Plan: Monthly Summary Sub-Tab

**Prerequisites:**
- F01 (Monthly Tab Navigation Shell) merged — this feature builds on the Summary tab guard it introduced in `MonthlyPage.tsx`.

### Stage 1: Grid Extraction

**1. Category Totals and Cards Grid Components** - Extract the Category Totals and Cards grids from `MonthlyPage.tsx` into their own components, carrying over their existing markup, props, and (for Cards) Mark/Unmark Paid interaction unchanged.

**2. Banks and Incoming Grid Components** - Extract the Banks and Incoming totals grids from `MonthlyPage.tsx` into their own components, carrying over their existing markup and props unchanged.

### Stage 2: Two-Row Grouping and Test Coverage

**3. Compose the Summary Tab into Two Rows** - Replace the inline grid JSX in `MonthlyPage.tsx`'s Summary tab block with the 4 new components arranged in 2 grouped rows (Category Totals + Cards, then Banks + Incoming), with no heading or label between the rows.

**4. Two-Row Layout Styling** - Add the CSS wrapper needed to stack the 2 grid rows with appropriate spacing, reusing the existing per-row and per-grid classes unchanged.

**5. Update Test Coverage** - Add a component test file for each of the 4 new grids, and update `MonthlyPage.test.tsx` to verify the Summary tab's 2-row grouping structure and that Mark/Unmark Paid still works after the regrouping.
