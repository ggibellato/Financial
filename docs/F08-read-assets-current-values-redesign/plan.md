# Implementation Plan: F08 — Read Assets Current Values Redesign

**Prerequisites:**
- F01 (App Navigation & Layout Restructure) implemented ✅
- No new npm packages required
- `BrokerNodeDto`, `AssetPriceDto` types in `src/api/types.ts` are unchanged
- `getBrokers` and `getCurrentPrice` methods in `src/api/financialApiClient.ts` are unchanged

---

### Stage 1: Configuration and Styles

**1. Portfolio Scope Config** - Create `src/config/portfolioScopeConfig.ts` and export the `FIXED_PORTFOLIO_SCOPE` constant declaring the two fixed broker/portfolio pairs (`XPI/Default` and `XPI/Acoes`). This module is the single source of truth for the fetch scope, mirroring the WPF `appsettings.json` configuration concept.

**2. CurrentValuesPage Styles** - Create `src/pages/CurrentValuesPage.css` with styles for the `.current-values` page layout using the established `flex: 1; min-height: 0; overflow: hidden` pattern so the page never produces a vertical or horizontal scrollbar; apply `overflow-y: auto` only to the results container so the table scrolls independently. Add Price column right-alignment and bold weight. Move any `current-values*` rules currently in `App.css` into this file and update `App.css` accordingly.

---

### Stage 2: Page Redesign

**3. CurrentValuesPage Rewrite** - Modify `src/pages/CurrentValuesPage.tsx` to remove `selectedBroker` and `selectedPortfolio` state, remove the `portfolios` derived value, and replace the dynamic `assetsToCheck` with a version that filters the `brokers` tree against `FIXED_PORTFOLIO_SCOPE`. Remove the broker and portfolio `<select>` controls from the JSX. Remove `asOf` from the `PriceResult` interface, remove the `formatDateTime` helper, and remove the "As of" column from the table header and body. Change the section heading to "Fetch Current Prices". Import and apply `CurrentValuesPage.css`. See the spec for the fixed scope structure, column definitions, progress text format, and error handling behaviour.

---

### Stage 3: Tests

**4. CurrentValuesPage Tests Update** - Update `src/pages/__tests__/CurrentValuesPage.test.tsx` to reflect the redesign. Remove or update any assertion that relied on the broker/portfolio filter controls or the "As of" table column. Add tests for fixed-scope asset resolution (only XPI/Default and XPI/Acoes), inactive asset exclusion, empty-ticker/exchange exclusion, per-asset error row ("—"), progress text format, and the broker tree load failure path (error state visible, Check Prices button absent). Follow the `vi.mock` + `satisfies Partial<FinancialApiClient>` pattern used in the sibling test file.
