# F01. Web DataGrid Visual Standardization — Technical Specification

## 1. Scope

**Included:**
- Adding one new shared stylesheet, `Financial.Web/src/styles/data-table.css`, defining `.data-table` (base table rules: font-size, border-collapse), shared `th`/`td` rules (padding, border, plain header style), a shared zebra rule, and a shared `.data-table__col--numeric` class (right-aligned)
- Importing that stylesheet once globally from `Financial.Web/src/main.tsx`
- Applying the `data-table` class to every `<table>` element across the 6 grids in scope: Assets Current Prices (`CurrentValuesPage.tsx`), Portfolio Summary (`PortfolioSummaryTab.tsx`), Assets Transactions (`TransactionsTab.tsx`), Assets Credits (`CreditsTab.tsx`), Dividend History and By Year (`DividendCheckPage.tsx`)
- Migrating each grid's numeric-column alignment to the shared `data-table__col--numeric` class (Portfolio Summary keeps its existing positional `nth-child(n+3)` rule instead of adding 13 individual classNames — see Assumptions)
- Removing the now-redundant duplicate CSS declarations (`width: 100%`, `border-collapse: collapse`, explicit `font-size`, zebra `nth-child(even)`, uppercase/muted header style) from `PortfolioSummaryTab.css`, `TransactionsTab.css`, `CreditsTab.css`, `CurrentValuesPage.css`, `DividendCheckPage.css`
- Preserving all existing conditional formatting: profit/loss coloring (`portfolio-summary__profit--green/red`), transaction-type coloring (`transactions-tab__type--buy/sell`), credit-type coloring (`credits-tab__type--dividend/rent`), and bold weights on totals/values

**Out of scope (per PRD Section 7):**
- New columns, filters, sorting, export, or pagination behavior
- Non-table controls (buttons, filter/mode toggle bars, form inputs, Recharts charts)
- The WPF desktop app (already standardized in P05-F01)
- New dark-mode-specific rules beyond the existing `var(--border)`/`var(--text-h)` tokens already in use
- A shared `<DataTable>` React component (CSS-only centralization for this feature)
- Any Domain, Application, or Infrastructure/backend change

## 2. Component Overview

| File | Role |
|---|---|
| `Financial.Web/src/styles/data-table.css` (new) | Defines `.data-table` (13px `font-size`, `border-collapse: collapse`, `width: 100%`), `.data-table th` (plain header: `text-align: left`, `color: var(--text-h)`, padding, border-bottom, `white-space: nowrap`, no uppercase/letter-spacing/muted color), `.data-table td` (padding, border-bottom, `vertical-align: middle`, `white-space: nowrap`), `.data-table tbody tr:nth-child(even)` (zebra, `background: #f5f5f5`), and `.data-table__col--numeric` (`text-align: right`). |
| `Financial.Web/src/main.tsx` | Adds `import './styles/data-table.css'` alongside the existing `import './index.css'`, so the shared classes are available app-wide without per-component imports. |
| `Financial.Web/src/pages/CurrentValuesPage.tsx` / `.css` | Reference grid. `<table>` gains `className="data-table"`. The Price `<th>`/`<td>` keep `current-values__col--price` (for its bold weight) and additionally gain `data-table__col--numeric`. `CurrentValuesPage.css` drops its own `table`/`th`/`td`/zebra/`text-align` base rules (now provided by `.data-table`), keeping only the bold-weight declaration on `current-values__col--price`. |
| `Financial.Web/src/components/PortfolioSummaryTab.tsx` / `.css` | `<table className="portfolio-summary__table">` gains `data-table` (`className="portfolio-summary__table data-table"`). `PortfolioSummaryTab.css` drops its own `font-size`, header uppercase/letter-spacing/muted-color style, and base `td`/`th` padding/border rules (now from `.data-table`); keeps its `th:nth-child(n+3)`/`td:nth-child(n+3)` positional right-align rule (no `data-table__col--numeric` classNames added to 13 individual columns — see Assumptions), the `portfolio-summary__profit--green/red` colors, and the `portfolio-summary__credits-separator` accent border. |
| `Financial.Web/src/components/TransactionsTab.tsx` / `.css` | `<table className="transactions-tab__table">` gains `data-table`. The 4 numeric `<th>`/`<td>` pairs (Quantity, Unit Price, Fees, Total) switch from `transactions-tab__amount`/`transactions-tab__total` to `data-table__col--numeric` (Total keeps an additional class for its bold weight). `TransactionsTab.css` drops its own `font-size`, header uppercase style, base `td`/`th` rules, and zebra rule; keeps `transactions-tab__type--buy/sell` coloring and the bold-weight rule for the Total column. |
| `Financial.Web/src/components/CreditsTab.tsx` / `.css` | `<table className="credits-tab__table">` gains `data-table`. The Value `<th>`/`<td>` switch from `credits-tab__value` to `data-table__col--numeric` plus a slim class for bold weight. `CreditsTab.css` drops its own `font-size`, header uppercase style, base `td`/`th` rules, and zebra rule; keeps `credits-tab__type--dividend/rent` coloring. |
| `Financial.Web/src/pages/DividendCheckPage.tsx` / `.css` | Both `<table>` elements (Dividend History, By Year) gain `className="data-table"`. The `table-number` class on numeric `<th>`/`<td>` is replaced with `data-table__col--numeric`. `DividendCheckPage.css` drops its own `table`/`th`/`td`/zebra/`text-align` base rules (now from `.data-table`). |

No Domain, Application, Infrastructure, or backend API files are touched.

## 3. Requirements / Business Rules

- `.data-table` sets `font-size: 13px` — the shared size decided for all 6 grids (not the reference grid's literal inherited 18px, which would be too large for the denser multi-column tables like Portfolio Summary and Transactions).
- `.data-table th` uses a plain style matching the reference grid exactly: `text-align: left`, `color: var(--text-h)`, standard padding/border, no `text-transform: uppercase`, no `letter-spacing`, no muted color — this removes the distinct "label" header look currently used by Portfolio Summary, Transactions, and Credits.
- `.data-table tbody tr:nth-child(even)` sets `background: #f5f5f5`, matching the exact value already used independently by 4 of the 5 non-Portfolio-Summary grids.
- `.data-table__col--numeric` sets `text-align: right` — every grid's numeric columns keep the alignment they already had; this feature only consolidates the definition into one shared class, it does not change any alignment.
- Component-specific classes that carry additional non-alignment styling (bold weight on `current-values__col--price`, `transactions-tab__total`, `credits-tab__value`) are kept alongside the new shared numeric class rather than deleted, since that styling (bold) is specific to those particular "total" columns, not a general numeric-column rule.
- Portfolio Summary's numeric columns (13 of its 15 columns) keep their existing `th:nth-child(n+3)`/`td:nth-child(n+3)` positional CSS rule rather than being retrofitted with 13 individual `data-table__col--numeric` classNames in the JSX — the rendered result (`text-align: right`) is identical either way, and avoiding the 13-column JSX churn keeps this presentation-only change minimal, consistent with "avoid overengineering."
- The new stylesheet is imported once, globally, in `main.tsx` — no per-component `.css` file needs to `@import` it; component CSS files only need to reference the shared class names they now use in JSX.
- No column changes width (beyond removing the now-redundant `width: 100%` duplicate, which `.data-table` already provides), header text, sort/filter/action behavior, or data — only font-size, header style, and zebra-striping source consolidate into the shared stylesheet.

## 4. UX Flow

1. User opens the Read Assets Current Values page, Portfolio Summary tab, Asset Transactions tab, Asset Credits tab, or Shares Dividend Check page.
2. Every table renders at 13px font-size, with a plain (non-uppercase) header style, alternating `white`/`#f5f5f5` row backgrounds, and right-aligned numeric columns.
3. Existing behaviors are visually unchanged aside from the above: row action buttons (edit/delete icons) still function, filters and Stacked/Grouped or Bar/Line mode toggles still work, and all conditional coloring (profit/loss green/red, transaction-type colors, credit-type colors, bold totals) still renders exactly as before.
4. No new interaction is introduced; this is a pure re-render of existing data with a consistent visual style.

## 5. Error Handling

Not applicable — this is a read-only, presentation-only styling change with no data mutation, network call, or state transition that can fail. Per the PRD's Error Handling inclusion criteria (auth, payments, data loss risk, security, long-running/irreversible operations), none apply here.

## 6. Testing Strategy

The existing frontend test suite (Vitest, colocated `__tests__/` folders per component/page) tests component behavior and rendering, not CSS visual output — this change introduces no new component logic or props, only className additions and CSS file edits. Consistent with that pattern, this feature's testing strategy is:

- **Build/typecheck verification:** `tsc -b --noEmit` (per this project's established convention — see `feedback_typescript_build_validation` — vitest alone doesn't catch type errors that fail the build) and the frontend build (`npm run build` / `vite build`) must succeed after every touched `.tsx`/`.css` file.
- **Full existing test suite regression:** the existing Vitest suite (`npm test` / `vitest run`) must remain green — this change touches no component logic, only `className` values and CSS, so no existing test should be affected; a failure would indicate an unintended side effect (e.g., a test asserting on an exact `className` string).
- **Manual visual verification (acceptance test surrogate):** run the web app (dev server) and visually confirm, for each of the 6 grids, against the PRD's Section 9 acceptance criteria:
  - Same font-size/zebra pattern/header style across all 6 grids
  - Every numeric column still right-aligned, including the reference grid's Price column
  - Profit/loss coloring, transaction-type coloring, credit-type coloring, and bold totals still render correctly
  - Row action buttons, filters, and mode toggles in Transactions and Credits still work

## 7. Assumptions / Decisions

- **Shared font-size: 13px, not the reference grid's literal 18px.** Confirmed with the user before implementation: the reference grid currently has no explicit font-size and inherits the page's 18px root font, but the other grids explicitly use 12-13px for density. Matching literally at 18px would make the denser multi-column tables (Portfolio Summary, Transactions, Credits) noticeably larger and increase horizontal scrolling. 13px was chosen as the shared standard — the same size WPF was standardized on in the prior P05-F01 feature — applied to all 6 grids including the reference grid itself.
- **Numeric alignment: right, unchanged.** Every grid already right-aligns its numeric columns; this feature does not change alignment anywhere, it only consolidates the existing convention into one shared `data-table__col--numeric` class. *(Learned from the equivalent WPF feature, where an initial draft incorrectly proposed left-alignment before being corrected — this web feature request explicitly specified "right" from the start.)*
- **Header style unification.** The uppercase/11px/letter-spaced/muted-color header style currently used by Portfolio Summary, Transactions, and Credits is replaced by the reference grid's plain header style, since the user's request ("same font type, style and size... follow the same as Read Assets current values") extends to header presentation, not just body-cell font-size and alignment.
- **Portfolio Summary numeric columns keep their positional CSS rule** rather than gaining 13 individual `data-table__col--numeric` classNames — a pragmatic scope decision to avoid a large, purely-cosmetic-equivalent JSX diff across every column in that table; the rendered alignment is identical either way.
- **CSS-only centralization, no shared React component.** Building a shared `<DataTable>` component to replace all five hand-rolled `<table>` implementations would be a larger structural refactor than this presentation-only visual-consistency request calls for; a shared stylesheet achieves the same DRY goal for styling with a much smaller, lower-risk diff.
