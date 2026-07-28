# Implementation Plan: Category Totals Sub-Tab

**Prerequisites:**
- F01 (Annual Summary Navigation Shell) merged to main — this feature builds inside the Category Totals tab guard F01 introduced.
- No new dependencies, environment variables, or configuration.

### Stage 1: Derived Data Layer

**1. Average Utility** - Add `Financial.Web/src/utils/math.ts` exporting `average(values: number[]): number`, computing the arithmetic mean using the array's own length (no hardcoded month count).

**2. Total Despesas and Resultado in the Hook** - Extend `useAnnualSummary.ts` with memoized `totalDespesasMonthly`/`totalDespesasAnnualTotal` (sum of all fetched categories' monthly totals) and `resultadoMonthly`/`resultadoAnnualTotal` (net income minus every category except Investimento, per the PRD formula), derived from the already-fetched `categoryTotals` and `incomeSummary`.

**3. Hook Test Coverage** - Add test cases to `useAnnualSummary.test.ts` verifying both derived value sets against a multi-category fixture that includes `Investimento`.

### Stage 2: Merged Category Totals Table

**4. Combine Category Totals and Income Summary** - Replace the Category Totals tab's two separate `<section>` blocks with one, rendering a single table whose rows follow the fixed order: Salary, Salary After Taxes, Tax Difference, spacer, Dividendo/Juros, spacer, the 14 category rows, spacer, Resultado (R-D-Inv), Total despesas.

**5. Average Column** - Add an Average `<th>`/`<td>` to every row in the merged table, positioned between the Dec column and the Annual Total column, using the new `average()` utility.

**6. Emphasis Styling** - Rename `.annual-summary-page__net-position-row` to `.annual-summary-page__emphasized-row` in `AnnualSummaryPage.css` (and its one existing usage on the Investments tab's Net Position row), then apply the same class to the new Resultado and Total despesas rows.

### Stage 3: Test Coverage

**7. Update Page Test Coverage** - Replace the existing separate Category Totals / Income Summary test expectations with coverage for the merged table's row order, the Average column, Resultado, Total despesas, and the emphasized-row styling, and confirm the Investments tab and tab-switching behavior from F01 are unaffected by the merge.
