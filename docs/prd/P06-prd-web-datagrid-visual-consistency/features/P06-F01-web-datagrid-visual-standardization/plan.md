# F01. Web DataGrid Visual Standardization — Implementation Plan

## Prerequisites

- None (Wave 1, no feature dependencies).

## Phase 1: Create the shared stylesheet

1. **data-table.css** - Create `Financial.Web/src/styles/data-table.css` defining `.data-table` (13px font-size, `border-collapse: collapse`, `width: 100%`), shared `th`/`td` rules (padding, border, plain non-uppercase header style matching the reference grid), a shared zebra rule (`tbody tr:nth-child(even)`, `#f5f5f5`), and `.data-table__col--numeric` (`text-align: right`).
2. **Global import** - Import the new stylesheet once in `Financial.Web/src/main.tsx`, alongside the existing `index.css` import, so its classes are available to every page/component without per-file imports.

## Phase 2: Apply the standard across all six grids

1. **Reference grid (CurrentValuesPage.tsx/.css)** - Add `data-table` to the `<table>` className; add `data-table__col--numeric` to the Price `<th>`/`<td>` alongside the existing `current-values__col--price` class (kept for its bold weight); remove the now-redundant base table/th/td/zebra/alignment rules from `CurrentValuesPage.css`.
2. **Portfolio Summary grid (PortfolioSummaryTab.tsx/.css)** - Add `data-table` to the `<table>` className; remove the redundant `font-size`, uppercase/letter-spaced/muted header style, and base `td`/`th` rules from `PortfolioSummaryTab.css`, keeping the existing `th:nth-child(n+3)`/`td:nth-child(n+3)` positional right-align rule, the profit green/red colors, and the credits-separator accent border untouched.
3. **Assets Transactions grid (TransactionsTab.tsx/.css)** - Add `data-table` to the `<table>` className; switch the 4 numeric `<th>`/`<td>` pairs from `transactions-tab__amount`/`transactions-tab__total` to `data-table__col--numeric` (Total keeps an additional class for bold weight); remove the redundant `font-size`, header uppercase style, base `td`/`th` rules, and zebra rule from `TransactionsTab.css`, keeping the type-color rules untouched.
4. **Assets Credits grid (CreditsTab.tsx/.css)** - Add `data-table` to the `<table>` className; switch the Value `<th>`/`<td>` from `credits-tab__value` to `data-table__col--numeric` plus a slim bold-weight class; remove the redundant `font-size`, header uppercase style, base `td`/`th` rules, and zebra rule from `CreditsTab.css`, keeping the type-color rules untouched.
5. **Dividend History and By Year grids (DividendCheckPage.tsx/.css)** - Add `data-table` to both `<table>` classNames; replace the generic `table-number` class with `data-table__col--numeric` on all numeric `<th>`/`<td>` cells; remove the redundant base table/th/td/zebra/alignment rules from `DividendCheckPage.css`.

## Phase 3: Verification

1. **Typecheck, build, and full test suite** - Run `tsc -b --noEmit`, the frontend build, and the existing Vitest suite to confirm no type errors, no build errors, and no regression in existing component/page tests.
2. **Manual visual pass** - Run the web app locally and visually confirm all 6 grids share the same font-size/zebra pattern/header style, all numeric columns remain right-aligned (including the reference grid's Price column), and all existing conditional coloring, row action buttons, filters, and mode toggles still work as before.
