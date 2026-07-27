# Yearly Summary Historical Averages Sub-Tab

## 1. Executive Summary

Yearly Summary Historical Averages Sub-Tab adds a third view to `Financial.CashFlow`'s Yearly Summary tab, alongside the existing Category Totals and Investments sub-tabs. It is used by the same single developer-maintainer that all prior CashFlow features serve. Today, seeing how a category or income line has trended across past years requires opening each year's `Resumo/xxxx` spreadsheet tab individually and comparing figures by hand — there is no in-app way to see multiple years side by side. This feature closes that gap by reproducing the spreadsheet's `Q1:Y25`-style historical block directly in the app: one column per year, one row per category/income line, each cell holding that line's yearly average.

At a high level, the new "Historical Summary Averages" sub-tab shares the same year picker as Category Totals and Investments. It shows the exact same row set as Category Totals (income lines, the 14 expense categories, Resultado, Total despesas), but instead of 12 month columns it shows one column per calendar year — starting at the selected year and going backward to the earliest year with any recorded data. Every year's value is computed server-side in a single batch request, so opening the sub-tab never issues one network call per year.

## 2. Problem and Opportunity

### The Problem

**Multi-year trends require manual cross-referencing today**
- Comparing how a single category (e.g. Mercado) trended over the last 5+ years means opening each year's spreadsheet tab or Yearly Summary view one at a time and copying figures down by hand
- There is no single screen showing every year's average for a given row at once

**The app has no cross-year aggregation today**
- The existing Yearly Summary endpoints (`expense-categories`, `investment-diffs`, `income-summary`) all accept exactly one year and return that year's monthly figures — there is no endpoint that reasons across multiple years
- Adding a year to the visible range today would mean manually changing the year picker and re-reading the Category Totals table repeatedly

**In-progress years distort naive averages**
- The current calendar year's most recent month is often only partially recorded (transactions still being entered), so a plain 12-month or to-date average for the current year would understate or overstate the true monthly average by including an incomplete month

**Historical data has gaps and varying start points**
- Early years (approaching 2017) may have fewer recorded months than later years, and some categories may not have existed or been tracked consistently in the earliest years

### The Opportunity

- Manual cross-referencing → the new sub-tab renders every available year's average for every row in one table, eliminating the need to open past years individually
- No cross-year aggregation → a new batch endpoint computes every year's averages server-side in one call, extending the existing per-year computation pattern (`YearlySummaryService`) to loop years internally
- In-progress years distorting averages → when the selected year is the current calendar year, the average excludes the in-progress current month, counting only fully completed months
- Data gaps and varying start points → the earliest displayed year is derived dynamically from actual data (not a hardcoded floor), and any year with zero recorded data anywhere is simply omitted from the range rather than shown as a misleading blank column

## 3. Target Audience

### Primary Users

**Personal Finance Developer-Maintainer**
- The sole user and maintainer of this self-hosted personal finance tool, already familiar with every category, income line, and figure on the Yearly Summary tab
- Periodically reviews long-term spending and income trends per category (e.g. "has Mercado spending grown year over year?") rather than just a single year's figures
- Values a simple, uncluttered layout over configurability — a single fixed row order and column range is preferable to any customization

## 4. Objectives

**Product Objectives**

- **Expose** every category and income line's year-over-year average in a single view, without leaving the Yearly Summary tab or opening past years' spreadsheets.
  Metric: the table renders one column per year from the selected year back to the earliest year with any data, covering 100% of the rows already shown in Category Totals.

- **Compute** all displayed years' averages in one server round trip rather than one request per year.
  Metric: opening the Historical Averages sub-tab issues exactly one network request regardless of how many year columns are displayed.

- **Exclude** in-progress, incomplete months from the current year's average so the figure reflects only fully completed months.
  Metric: when the selected year is the current calendar year, the computed average for every row uses only months 1 through the last fully completed month, verified by a unit test pinned to a fixed "today" date.

- **Adapt** the earliest visible year automatically as more historical data is imported, without requiring a code change to move a hardcoded floor year.
  Metric: the earliest displayed year is derived at request time from actual recorded data, verified by a test asserting the range extends when an earlier year's fixture data is added.

## 5. User Stories

### F01. Historical Averages Sub-Tab
- As the developer, I want a third "Historical Summary Averages" sub-tab next to Category Totals and Investments so that I can review year-over-year trends without leaving the Yearly Summary tab
- As the developer, I want the same shared year picker to control this sub-tab so that I never have to re-select the year separately from the other two sub-tabs
- As the developer, I want the same rows as Category Totals (Salary, Salary After Taxes, Tax Difference, Dividendo/Juros, the 14 expense categories, Resultado, Total despesas) so that the historical view lines up with the single-year view I already know
- As the developer, I want one column per year, starting at the selected year and going backward to the earliest year with any data, so that I see the full available history at a glance
- As the developer, I want each year's cell to be that row's average over however many months are actually recorded that year, so that partial historical years still contribute a useful figure instead of being blank
- As the developer, I want years with absolutely no recorded data omitted from the range, so that the table isn't padded with meaningless empty columns
- As the developer, I want the current year's average to exclude the in-progress current month, so that an incomplete month never skews the figure
- As the system, I want to compute every displayed year's averages in a single response so that the frontend never needs to issue one request per year

## 6. Functionalities

### F01. Historical Averages Sub-Tab

**Capabilities:**
- Adds a third sub-tab, "Historical Summary Averages", after "Investments" on the existing Yearly Summary tab shell; selecting it never resets the shared year picker and never affects the other two sub-tabs' loaded data
- Row set and order identical to Category Totals: Salary, Salary After Taxes, Tax Difference, Dividendo/Juros, then the 14 `Category` enum rows in declaration order (Ariana, Carro, Casa, Estudo, Extras, Familia, Gleison, Mercado, Samuel, Saude, Viagem, Dizimo, Investimento, Reserva), then Resultado (R-D-Inv), then Total despesas — using the same monthly value definitions established for Category Totals (Total despesas = sum of the 14 categories), with one deliberate exception: this sub-tab's Resultado = Salary After Taxes − Total despesas + Investimento, excluding Dividendo/Juros. This differs intentionally from Category Totals' own Resultado (which does include Dividendo/Juros) — a conscious divergence for the historic-average view, not an oversight
- Column set: one column per calendar year, ordered left to right starting at the selected year and decreasing by one each column, continuing down to the earliest year for which at least one row has at least one recorded month; any year within that span with zero recorded data across every row is omitted entirely (no blank filler column)
- Each year's cell value = arithmetic mean of that row's recorded monthly values for that year (partial years average over however many months are actually recorded, consistent with the existing per-row Average column behavior on Category Totals)
- Monthly values are computed exactly as in `GetCategoryTotalsForYear`/`GetIncomeSummaryForYear`: same-month entries are summed together first (per category, or per combined income group for Salary/Salary After Taxes), and only that resulting per-month total feeds the year's average — never an average taken directly over individual transactions. This matches `Resumo{year}`'s own column N (`=AVERAGE(B{row}:M{row})`), where each of B:M is already a pre-summed monthly total, not a raw entry
- Current-year exception: when the selected year equals the current calendar year, that year's column only averages months 1 through the last fully completed month (the in-progress current month is excluded from the average entirely, not treated as zero); if the current year has zero fully completed months yet (today falls in January), the current year column is omitted and the range starts at the previous year instead
- All years' averages are computed and returned by a single new batch endpoint/service call anchored at the selected year — the frontend never issues one request per year
- All monthly and yearly figures use the existing 2-decimal numeric formatting (`formatN2`) used throughout the Yearly Summary tab today

**Experience:**
- Clicking "Historical Summary Averages" swaps the visible content exactly like switching between Category Totals and Investments; the tab is highlighted as active and the shared year picker remains unchanged
- Because this sub-tab's data is not already fetched by the other two sub-tabs, a loading indicator shows on first visit (and on every year change) while the batch request completes, independent of whether Category Totals/Investments data is cached
- The table scrolls horizontally when the year range exceeds the viewport width, consistent with how the existing month-column tables behave on narrow viewports
- A visual separator (blank spacer row) appears between Dividendo/Juros and the first category row, and again before Resultado, mirroring Category Totals' grouping convention
- Resultado and Total despesas rows render visually emphasized (bold), consistent with Category Totals' existing styling
- Changing the shared year picker while on this sub-tab triggers a new fetch, re-anchoring the whole displayed year range at the newly selected year
- If the batch request fails, the existing error/retry pattern used elsewhere on the Yearly Summary tab is shown

## 7. Out of Scope

**Data and business logic**
- No changes to the `Category`, `InvestmentAccount`, or `IncomeSource` enums, or to any other CashFlow entity
- No changes to the existing Category Totals or Investments endpoints, DTOs, or computations — this feature only adds a new endpoint alongside them
- No investment account rows or balances in this sub-tab — investment account history remains exclusive to the Investments sub-tab

**Layout and navigation**
- No user-configurable floor year, row order, or column set — the range and rows described in Section 6 are the only supported layout
- No persistence of the active sub-tab across page reloads — consistent with the existing Yearly Summary tab behavior, it always resets to Category Totals on re-entry
- No URL-based deep-linking to the Historical Averages sub-tab specifically

**Presentation and analysis**
- No charting or graphing of the historical trend — this feature is a table only
- No editing or data-entry capability from this view — strictly read-only, consistent with Category Totals and Investments
- No inflation adjustment, currency conversion, or any normalization of historical figures — raw stored values only, matching the rest of the CashFlow domain
- No caching of computed historical averages beyond a single request/response — each fetch recomputes from source data

## 8. Dependency Graph

| # | Feature | Priority | Dependencies |
|---|---------|----------|--------------|
| F01 | Historical Averages Sub-Tab | 2 | None |

### Execution Waves
Features within the same wave can be built in parallel. A wave starts only after every feature in earlier waves is complete.

- **Wave 1**: F01

### Priority levels
- **1** = Essential — product does not work without it
- **2** = Important — significant value addition
- **3** = Desirable — incremental improvement

```mermaid
graph TD
  F01[Historical Averages]
```

## 9. Acceptance Criteria

### F01. Historical Averages Sub-Tab
- [ ] The Yearly Summary tab shows a third sub-tab, "Historical Summary Averages", after Investments, sharing the same year picker as the other two sub-tabs
- [ ] The table's rows match Category Totals exactly, in the same fixed order: Salary, Salary After Taxes, Tax Difference, Dividendo/Juros, the 14 categories in enum order, Resultado (R-D-Inv), Total despesas
- [ ] The leftmost year column equals the currently selected year; each subsequent column decreases by one year, down to the earliest year with any recorded data across any row
- [ ] Any year in that span with zero recorded data in every row is omitted from the column set entirely
- [ ] Each year's cell equals the arithmetic mean of that row's recorded monthly values for that year
- [ ] A row's monthly value is the sum of same-month entries (per category, or per combined income group for Salary/Salary After Taxes) — a category or income group with more than one transaction in the same month never averages over those transactions directly
- [ ] When the selected year is the current calendar year, that column's average only includes months through the last fully completed month, excluding the in-progress current month
- [ ] If the current year has zero fully completed months (today is in January), the current year column is omitted and the range starts at the previous year
- [ ] Opening the sub-tab (or changing the selected year while it is active) issues exactly one network request that returns every displayed year's figures at once
- [ ] Resultado and Total despesas rows render bold, consistent with Category Totals styling
- [ ] If the batch request fails, the existing Yearly Summary error/retry state is shown
