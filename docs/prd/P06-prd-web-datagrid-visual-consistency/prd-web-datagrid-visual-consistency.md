# Web DataGrid Visual Consistency

## 1. Executive Summary

Web DataGrid Visual Consistency is a presentation-only fix for the Financial application's React web frontend (Financial.Web). Today, the five main data tables — Assets Current Prices, Portfolio Summary, Assets Transactions, Assets Credits, and the two Shares Dividend Check tables (Dividend History, By Year) — each implement their own plain-CSS table styling independently, with no shared table stylesheet or component. This has produced real inconsistencies: three of the five grids have no alternating-row (zebra) striping at all, font sizes vary between 18px (inherited, on the reference and Dividend Check grids), 13px (Portfolio Summary), and 12px (Transactions, Credits), and header styling varies between a plain left-aligned label (reference, Dividend Check) and an uppercase, letter-spaced, muted-color label (Portfolio Summary, Transactions, Credits). Numeric-column alignment is already right-aligned everywhere, but implemented five different ways (a dedicated per-component CSS class in most grids, plain positional `nth-child` selection in Portfolio Summary), with no shared definition.

This feature establishes the "Assets Current Prices" grid on the Read Assets Current Values page as the canonical visual standard and brings the other five grids into alignment with it: one shared font size (13px, chosen for legibility and density across all grids rather than the reference's literal inherited 18px, which would be too large for the denser multi-column tables), the same alternating-row zebra pattern, the same plain (non-uppercase) header style, and numeric columns consistently right-aligned. The shared visual rules are centralized in one new stylesheet (`src/styles/data-table.css`), imported once globally, so grids reference shared classes instead of duplicating the same CSS declarations across five separate component stylesheets.

No business logic, data, or layer boundaries change. This is purely a React/CSS Presentation-layer styling change.

---

## 2. Problem and Opportunity

### The Problem

**Inconsistent zebra striping across grids**
- Portfolio Summary has no alternating-row background at all
- Assets Transactions, Assets Credits, Dividend History, By Year, and Assets Current Prices all already have zebra striping, but each defines it independently in its own CSS file with the same hard-coded `#f5f5f5` value, risking future drift

**Inconsistent font sizing**
- Assets Current Prices and the two Dividend Check tables have no explicit table font-size and inherit the page's 18px root font
- Portfolio Summary explicitly sets 13px
- Assets Transactions and Assets Credits explicitly set 12px
- The result is visibly different text density depending on which screen the user is viewing

**Inconsistent header styling**
- Assets Current Prices and Dividend Check headers are plain: left-aligned, same size family as body text, `var(--text-h)` color, default bold from the browser's `<th>` styling
- Portfolio Summary, Assets Transactions, and Assets Credits headers are uppercase, 11px, letter-spaced, and muted-colored — a visually distinct "label" style not used anywhere else

**Numeric alignment already consistent, but duplicated five different ways**
- Every grid already right-aligns its numeric/currency columns, but each does so via its own differently-named CSS class (`current-values__col--price`, `transactions-tab__amount`/`transactions-tab__total`, `credits-tab__value`, `table-number`) or, in Portfolio Summary's case, a positional `td:nth-child(n+3)` rule with no dedicated class at all
- There is no shared definition, so a future change to the alignment convention would require editing five separate files

**No shared table stylesheet or component**
- Each of the five grids is a hand-rolled `<table>` with its own CSS file; there is no shared `Table`/`DataGrid` component or shared stylesheet anywhere in the frontend, so any future visual tweak must be applied in five places or grids will silently drift out of sync

### The Opportunity

- Inconsistent zebra striping → apply one shared zebra rule to every grid, including Portfolio Summary, which currently has none
- Inconsistent font sizing → apply one shared 13px font-size to all six grids (the reference grid's own table also moves from its current inherited 18px to the shared 13px, chosen over the literal 18px for legibility and density in the denser multi-column tables)
- Inconsistent header styling → apply one shared plain header style (matching the reference grid) to all six grids, removing the uppercase/muted "label" style from Portfolio Summary, Transactions, and Credits
- Duplicated numeric-alignment logic → centralize a single shared `.data-table__col--numeric` class (right-aligned) that every grid's numeric `<th>`/`<td>` elements reference
- No shared stylesheet → add one new `src/styles/data-table.css`, imported once globally, containing the shared `.data-table` base rules (font-size, zebra, header, cell padding, numeric alignment) that every grid's `<table>` opts into via `className="data-table"`

---

## 3. Target Audience

### Primary Users

**Personal Investor (web app user)**
- Uses the React web app to review portfolio data across multiple pages in the same session (Portfolio Summary, Transactions, Credits, Dividend Check, Read Assets Current Values)
- Notices visual inconsistency between pages as a polish/quality signal, even though it has no functional impact
- Expects the web app to feel like one coherent product rather than a set of independently styled pages, and expects it to look and behave the same way as the equivalent WPF desktop screens

---

## 4. Objectives

**Establish one visual standard for all web DataGrids**
- Metric: all 6 grids in scope (Assets Current Prices, Portfolio Summary, Assets Transactions, Assets Credits, Dividend History, By Year) render body text and header text at the same 13px font-size

**Apply consistent zebra row striping everywhere**
- Metric: all 6 grids render alternating row backgrounds using the same shared `.data-table` CSS rule, verified by all six `<table>` elements carrying the `data-table` class

**Apply one consistent header style**
- Metric: all 6 grids' `<th>` elements share the same plain (non-uppercase, non-letter-spaced) header style sourced from the shared stylesheet

**Keep numeric columns consistently right-aligned via one shared rule**
- Metric: 100% of numeric/currency-formatted columns across all 6 grids use the shared `.data-table__col--numeric` class (or, where a positional rule is kept for practicality, still resolve to `text-align: right`)

**Eliminate style duplication**
- Metric: the base table/zebra/header/numeric-alignment CSS rules exist exactly once, in `src/styles/data-table.css`, referenced by class name from every grid's markup

---

## 5. User Stories

### F01. Web DataGrid Visual Standardization

- As a user, I want every data table in the web app to use the same font size and style so that the app feels visually consistent as I move between pages
- As a user, I want every data table to show the same alternating row striping so that long rows of data remain easy to scan on every page, not just some
- As a user, I want numeric and currency values to stay right-aligned consistently across every table, sourced from one shared style instead of five separate ad-hoc definitions, so decimal points line up and I don't have to re-adjust my scanning pattern from page to page
- As a user, I want the existing color-coding (profit green/red, transaction type colors, bold totals) to keep working exactly as it does today, since only the shared font/zebra/header presentation is changing, not the information itself or its alignment

---

## 6. Functionalities

### F01. Web DataGrid Visual Standardization

**Capabilities:**
- A new stylesheet `Financial.Web/src/styles/data-table.css` defines a shared `.data-table` class (13px font-size, `border-collapse: collapse`), shared `th`/`td` rules (padding, border, plain header style matching the reference grid: left-aligned, `var(--text-h)` color, no uppercase/letter-spacing/muted-color), a shared zebra rule (`tbody tr:nth-child(even) { background: #f5f5f5 }`), and a shared `.data-table__col--numeric` class (`text-align: right`)
- This stylesheet is imported once globally (in `src/main.tsx`, alongside the existing `index.css` import), making its classes available to every page/component without a per-file import
- Every `<table>` element across the 6 grids in scope gains the `data-table` class (added alongside any existing component-specific class, e.g. `className="portfolio-summary__table data-table"`)
- Every numeric/currency `<th>`/`<td>` in Assets Current Prices, Assets Transactions, Assets Credits, and both Dividend Check tables switches from its own bespoke alignment class (`current-values__col--price`, `transactions-tab__amount`/`transactions-tab__total`, `credits-tab__value`, `table-number`) to the shared `data-table__col--numeric` class; component-specific classes that also carry non-alignment styling (e.g. `transactions-tab__total`'s bold weight, `credits-tab__value`'s bold weight, `current-values__col--price`'s bold weight) keep their own class alongside the shared one for that extra styling
- Portfolio Summary's numeric columns, currently right-aligned via a positional `td:nth-child(n+3)` CSS rule with no dedicated class (13 columns), keep that positional rule as-is rather than retrofitting 13 individual column classNames — the visual outcome (right-aligned, matching the shared standard) is identical, and the positional rule requires no template changes for a feature that is purely visual
- The now-redundant per-component CSS declarations that duplicate the shared stylesheet (`width: 100%`, `border-collapse: collapse`, explicit `font-size`, zebra `nth-child(even)` rules, and the uppercase/muted header style in Portfolio Summary/Transactions/Credits) are removed from `PortfolioSummaryTab.css`, `TransactionsTab.css`, `CreditsTab.css`, `CurrentValuesPage.css`, and `DividendCheckPage.css`
- All existing conditional formatting is preserved unchanged: profit/loss green/red coloring in Portfolio Summary, transaction-type coloring and bold Total column in Assets Transactions, credit-type coloring and bold Value column in Assets Credits, bold Price column in Assets Current Prices

**Experience:**
1. User opens any of the 6 pages/tabs in scope (Read Assets Current Values, Portfolio Summary, Asset Transactions tab, Asset Credits tab, Shares Dividend Check page)
2. Every table renders with the same 13px font, the same zebra row striping, the same plain header style, and the same right-aligned numeric columns
3. Existing interactions (row action buttons, filters, mode toggles, color-coded profit/loss and transaction-type text) behave exactly as before — only font size, zebra consistency, and header style change; numeric alignment is unchanged
4. No new user-facing controls, no new data, no change to what information is shown — purely visual consistency

---

## 7. Out of Scope

**New grid features**
- No new columns, filters, sorting behavior, export, or pagination are introduced by this change

**Non-table controls**
- Buttons, filter/mode toggle bars, form inputs, and charts (Recharts) elsewhere on these pages are not restyled by this feature

**WPF desktop app**
- This feature is web-only; the WPF grids were already standardized in a prior feature (P05-F01) and are not touched here

**Dark mode changes beyond existing tokens**
- The shared stylesheet reuses the existing `var(--border)`/`var(--text-h)` design tokens (which already support the app's existing dark-mode media query); no new dark-mode-specific rules are introduced

**Shared React Table component**
- This feature centralizes CSS only; it does not introduce a shared `<DataTable>` React component to replace the five hand-rolled `<table>` implementations — that is a larger structural refactor outside this feature's presentation-only scope

**Domain/Application/Infrastructure changes**
- No business logic, API contracts, or backend code changes; this is a Presentation-layer-only (frontend) change

---

## 8. Dependency Graph

| # | Feature | Priority | Dependencies |
|---|---------|----------|--------------|
| F01 | Web DataGrid Visual Standardization | 3 | None |

### Execution Waves
Features within the same wave can be built in parallel. A wave starts only after every feature in earlier waves is complete.

- **Wave 1**: F01

### Priority levels
- **1** = Essential — product does not work without it
- **2** = Important — significant value addition
- **3** = Desirable — incremental improvement

```mermaid
graph TD
  F01[Web DataGrid Standardization]
```

---

## 9. Acceptance Criteria

### F01. Web DataGrid Visual Standardization
- [ ] Assets Current Prices, Portfolio Summary, Assets Transactions, Assets Credits, Dividend History, and By Year tables all render body and header text at the same 13px font-size
- [ ] All 6 tables render alternating row background striping using the shared `.data-table` zebra rule (verified by each `<table>` carrying the `data-table` class)
- [ ] All 6 tables use the same plain header style (left-aligned, `var(--text-h)` color, no uppercase/letter-spacing/muted color)
- [ ] Every numeric/currency column across all 6 tables is right-aligned, either via the shared `data-table__col--numeric` class or (Portfolio Summary only) the existing positional rule, both resolving to `text-align: right`
- [ ] Profit/loss green/red coloring in Portfolio Summary renders identically to before this change, with only font-size/header style differing
- [ ] Transaction-type coloring and the bold Total column in Assets Transactions render identically to before this change
- [ ] Credit-type coloring and the bold Value column in Assets Credits render identically to before this change
- [ ] The shared base table/zebra/header/numeric-alignment rules exist exactly once, in `src/styles/data-table.css`; the now-redundant duplicate declarations are removed from the five component/page CSS files
- [ ] Existing row action buttons (update/delete), filters, and mode toggles in Assets Transactions and Assets Credits are unaffected (regression check)

### Cross-Feature Integration
- [ ] N/A — this PRD has a single feature with no functional data dependencies on other features
