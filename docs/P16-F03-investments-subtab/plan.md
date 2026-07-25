# Implementation Plan: Investments Sub-Tab

**Prerequisites:**
- F01 (Yearly Summary Navigation Shell) merged to main — this feature reshapes content inside the Investments tab guard F01 introduced.
- No new dependencies, environment variables, or configuration.

### Stage 1: Reshaped Investments Table

**1. Investment Row Component** - Add a local `InvestmentRow` component to `YearlySummaryPage.tsx` rendering a label plus 12 month cells, where a `null` value renders a blank cell (used for Month Result's January column) and an `emphasized` flag bolds the row.

**2. Account Rows** - Replace the current Jan + 11-diff account rows with full 12-month balance rows using each account's existing `monthlyValues` array, changing the liability marker from `(liability)` to `(-)`.

**3. Total and Month Result Rows** - Add a Total row (`netPosition.monthlyValues`) and a Month Result row (`netPosition.monthlyDiffs`, with a blank January cell), both emphasized, replacing the current single "Net Position" row.

**4. Summary Figures** - Add a labeled block below the table showing Year Progress (`netPosition.fullYearNetChange`), Average Month Result (mean of `netPosition.monthlyDiffs`), and Sum of Month Results (sum of `netPosition.monthlyDiffs`), plus the CSS for that block.

### Stage 2: Test Coverage

**5. Update Investments Tab Test Coverage** - Replace the existing diff-table assertions with coverage for the 12-month account rows, the `(-)` suffix, the Total row, the Month Result row, and the three summary figures, and confirm the Category Totals tab and tab-switching behavior are unaffected by the reshape.
