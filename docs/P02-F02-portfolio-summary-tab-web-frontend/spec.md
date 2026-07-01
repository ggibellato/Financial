# Spec: F02 — Portfolio Summary Tab — Web Frontend

## 1. Technical Overview

**What:** Adds a `PortfolioSummaryTab` component to the React frontend that renders when a Portfolio node is selected in the investment tree. The component displays two independent sections: (1) the three aggregated totals (Total Bought, Total Sold, Total Credits) via the existing `useAggregatedSummary` hook unchanged, and (2) a per-asset breakdown table driven by a new `usePortfolioAssetSummary` hook that fetches static data from F01's endpoint and enriches each row with a live current price fetched in parallel.

**Why:** The current `DetailPanel` renders `AggregatedSummaryTab` for both Broker and Portfolio nodes — selecting a Portfolio shows only three aggregate totals with no per-asset breakdown. F01 now provides the server-side per-asset computation; this feature wires that data into the React frontend with automatic price enrichment, following the same pattern already used by `AssetSummaryTab` for individual assets.

**Scope:**

Included:
- `PortfolioAssetSummaryItemDto` type in `Financial.Web/src/api/types.ts`
- `getPortfolioAssetsSummary` method on `FinancialApiClient` interface and implementation in `financialApiClient.ts`
- `usePortfolioAssetSummary` hook in `Financial.Web/src/hooks/`
- `PortfolioSummaryTab` component and co-located CSS in `Financial.Web/src/components/`
- `DetailPanel.tsx` modification to render `PortfolioSummaryTab` for Portfolio nodes and `AggregatedSummaryTab` only for Broker nodes
- Unit tests for `usePortfolioAssetSummary`
- Component tests for `PortfolioSummaryTab`

Excluded:
- Broker-level per-asset breakdown (no change to `AggregatedSummaryTab`)
- Column sorting or filtering in the table
- Manual price refresh (prices are fetched once automatically on portfolio selection)
- Any backend, API endpoint, or data model changes
- Changes to `AssetSummaryTab`, `TransactionsTab`, or `CreditsTab`

---

## 2. Architecture Impact

**Affected components:**

```mermaid
graph TD
    User["User selects Portfolio node"] --> DetailPanel["DetailPanel.tsx (modified)"]
    DetailPanel --> PortfolioSummaryTab["PortfolioSummaryTab (new)"]
    PortfolioSummaryTab --> useAggregatedSummary["useAggregatedSummary (unchanged)"]
    PortfolioSummaryTab --> usePortfolioAssetSummary["usePortfolioAssetSummary (new)"]
    useAggregatedSummary --> apiClient["FinancialApiClient (modified)"]
    usePortfolioAssetSummary --> apiClient
    apiClient --> F01Endpoint["GET /summary/portfolio/{broker}/{portfolio}/assets"]
    apiClient --> PriceEndpoint["GET /prices/current?exchange=...&ticker=..."]
```

---

## 3. Technical Decisions

| Decision | Chosen Approach | Alternative Considered | Trade-off |
|----------|----------------|----------------------|-----------|
| Per-row price state structure | Parallel `rowPrices: RowPriceState[]` indexed by position alongside `items` | Merged `PortfolioAssetSummaryRow[]` with price fields inlined; `Map<string, RowPriceState>` keyed by ticker | Parallel arrays keep the reducer action shape simple (`ROW_PRICE_SUCCESS` / `ROW_PRICE_ERROR` carry only an `index`), avoids creating an extra merged type, and is consistent with `useAssetSummary` which keeps `asset` and `price` as separate state fields |
| Price fetch dispatch | Each price fetch dispatches independently (fire-and-forget per row) | `Promise.allSettled` collecting all prices before updating state | Independent dispatch updates each row as soon as its price arrives, satisfying the PRD requirement that "as each price resolves, the corresponding row updates in place"; `allSettled` would stall every row until the slowest fetch |
| `useAggregatedSummary` reuse in `PortfolioSummaryTab` | Call `useAggregatedSummary()` directly inside `PortfolioSummaryTab` | Lift aggregated state to a parent and pass as props | Follows the existing pattern (`AggregatedSummaryTab` owns its hook); keeps `PortfolioSummaryTab` self-contained; the hook already handles Portfolio node selection correctly without modification |
| Table HTML element | HTML `<table>` with `<thead>` / `<tbody>` | CSS grid (used by `AggregatedSummaryTab` and `AssetSummaryTab`) | The per-asset breakdown is genuinely tabular data (7 columns, variable number of rows); `<table>` provides correct semantics, accessible column headers, and natural column alignment; CSS grid is appropriate for key-value pair layouts, not multi-row columnar tables |

---

## 4. Component Overview

**Frontend:**

| File Path | New/Modified | Purpose | Key Responsibilities |
|-----------|--------------|---------|---------------------|
| `Financial.Web/src/api/types.ts` | Modified | API type definitions | Add `PortfolioAssetSummaryItemDto` interface with all nine fields from the F01 response |
| `Financial.Web/src/api/financialApiClient.ts` | Modified | API client | Add `getPortfolioAssetsSummary(brokerName, portfolioName)` to `FinancialApiClient` interface; implement it in the factory function using the path `/summary/portfolio/{brokerName}/{portfolioName}/assets` with `encodeURIComponent` on both params |
| `Financial.Web/src/hooks/usePortfolioAssetSummary.ts` | New | Hook encapsulating F01 fetch and parallel price enrichment | Manages `useReducer` with `items`, `rowPrices`, `isLoading`, `error`, and `retryCount`; triggers on Portfolio node selection; on `FETCH_SUCCESS` initialises `rowPrices` with one `{ isLoading: true, currentPrice: null, fetchFailed: false }` per item and fires one `getCurrentPrice` call per item in parallel; each price call dispatches `ROW_PRICE_SUCCESS` or `ROW_PRICE_ERROR` with the row index; exposes `items`, `rowPrices`, `isLoading`, `error`, `retry` |
| `Financial.Web/src/components/PortfolioSummaryTab.tsx` | New | Portfolio-specific Summary tab component | Renders totals section (via `useAggregatedSummary`) and per-asset table (via `usePortfolioAssetSummary`); handles loading and error states for each section independently; computes `currentValue = currentPrice × currentQuantity` and `profitPercent = (currentValue − totalInvested) / totalInvested × 100` per row; renders `"—"` when `totalInvested` is 0 or current price is unavailable; applies green/red CSS class to `% Profit` |
| `Financial.Web/src/components/PortfolioSummaryTab.css` | New | Scoped styles for `PortfolioSummaryTab` | Table, header, and cell layout; `portfolio-summary__profit--green` / `--red` colour modifier classes; `.portfolio-summary__loading-cell` style for the `...` per-cell indicator |
| `Financial.Web/src/components/DetailPanel.tsx` | Modified | Navigation-aware tab content dispatcher | Change summary routing: `isPortfolio` → render `PortfolioSummaryTab`; `isBroker` (i.e. `!isAsset && !isPortfolio`) → render `AggregatedSummaryTab`; `isAsset` → render `AssetSummaryTab`; import `PortfolioSummaryTab` |

---

## 5. API Contracts

### New API client method: `getPortfolioAssetsSummary`

Consumes the F01 endpoint. No backend changes are made in this feature.

- **Method:** GET
- **Path:** `/summary/portfolio/{brokerName}/{portfolioName}/assets`
- **Caller:** `usePortfolioAssetSummary` hook, triggered on Portfolio node selection

**Client method signature:**

```typescript
getPortfolioAssetsSummary: (brokerName: string, portfolioName: string) => Promise<PortfolioAssetSummaryItemDto[]>
```

**New type — `PortfolioAssetSummaryItemDto` (in `api/types.ts`):**

| Field | Type | Description |
|-------|------|-------------|
| `assetName` | `string` | Asset name, sorted alphabetically |
| `ticker` | `string` | Ticker symbol |
| `exchange` | `string` | Exchange code (e.g., `BVMF`, `LSE`) |
| `firstInvestmentDate` | `string \| null` | ISO 8601 date string; `null` when asset has no Buy transactions |
| `currentQuantity` | `number` | Net quantity held after all Buy and Sell transactions |
| `totalBought` | `number` | Sum of all Buy transaction totals |
| `totalSold` | `number` | Sum of all Sell transaction totals |
| `totalInvested` | `number` | `totalBought − totalSold` |
| `portfolioWeight` | `number` | Asset's share of total portfolio invested capital × 100 |

**Response example:**
```json
[
  {
    "assetName": "ALZR11",
    "ticker": "ALZR11",
    "exchange": "BVMF",
    "firstInvestmentDate": "2021-03-01T00:00:00",
    "currentQuantity": 25.0,
    "totalBought": 2500.00,
    "totalSold": 0.00,
    "totalInvested": 2500.00,
    "portfolioWeight": 71.4285714285714
  },
  {
    "assetName": "MXRF11",
    "ticker": "MXRF11",
    "exchange": "BVMF",
    "firstInvestmentDate": "2021-05-15T00:00:00",
    "currentQuantity": 0.0,
    "totalBought": 1200.00,
    "totalSold": 200.00,
    "totalInvested": 1000.00,
    "portfolioWeight": 28.5714285714286
  }
]
```

**Price fetch per row (existing endpoint, no changes):**
- **Method:** GET
- **Path:** `/prices/current?exchange={exchange}&ticker={ticker}`
- Used by `usePortfolioAssetSummary` in parallel after items are loaded; one call per asset row using the `ticker` and `exchange` values from the F01 response without transformation.

---

## 6. Data Model

Not applicable. No persistence or schema changes. All state is in-memory within the React component lifecycle.

---

## 7. Testing Strategy

### Test File Structure

| Test File | Test Type | Target | Coverage Goal |
|-----------|-----------|--------|---------------|
| `Financial.Web/src/hooks/usePortfolioAssetSummary.test.ts` | Unit | `usePortfolioAssetSummary` | All state transitions, parallel price dispatch, retry, node-change reset |
| `Financial.Web/src/components/__tests__/PortfolioSummaryTab.test.tsx` | Unit | `PortfolioSummaryTab` | All render states, value formatting, profit colour coding, regression for other node types |

### usePortfolioAssetSummary.test.ts

Follows the `createWrapper()` / `SelectedNodeProvider` / `vi.mock('../api/financialApiClient')` pattern from `useAggregatedSummary.test.ts`. Mock both `getPortfolioAssetsSummaryMock` and `getCurrentPriceMock`.

| Test Function | Description | Assertions |
|---------------|-------------|------------|
| `calls_getPortfolioAssetsSummary_on_portfolio_node_selection` | Portfolio node selected | `getPortfolioAssetsSummaryMock` called with correct `brokerName` and `portfolioName` |
| `does_not_fetch_when_broker_node_selected` | Broker node selected | `getPortfolioAssetsSummaryMock` not called |
| `does_not_fetch_when_asset_node_selected` | Asset node selected | `getPortfolioAssetsSummaryMock` not called |
| `sets_isLoading_true_while_fetch_in_progress` | Never-resolving promise | `isLoading` is `true` after Portfolio node selection |
| `populates_items_on_successful_fetch` | Resolves with 2 items | `items` length is 2; `items[0].assetName` matches expected value |
| `sets_error_on_fetch_failure` | Rejects with error message | `error` equals the error message; `items` is `null` |
| `resets_state_on_node_change` | Select Portfolio, then select Broker | After second selection, `items` is `null` and `isLoading` reflects current fetch state |
| `retry_re_triggers_fetch` | First call rejects, second resolves | After `retry()`, `getPortfolioAssetsSummaryMock` called twice; `items` populated after second call |
| `fires_getCurrentPrice_for_each_item_after_fetch` | Fetch resolves with 2 items | `getCurrentPriceMock` called twice with correct `exchange` and `ticker` pairs from the items |
| `sets_row_price_loading_true_after_items_arrive` | Price fetch never resolves | `rowPrices[0].isLoading` is `true` after items arrive |
| `populates_row_price_on_price_success` | Price resolves for first item | `rowPrices[0].currentPrice` equals returned price; `rowPrices[0].isLoading` is `false`; `rowPrices[0].fetchFailed` is `false` |
| `sets_row_fetch_failed_on_price_error` | Price rejects for first item | `rowPrices[0].fetchFailed` is `true`; `rowPrices[0].currentPrice` is `null`; `rowPrices[0].isLoading` is `false` |
| `failed_price_for_one_row_does_not_affect_other_rows` | First price fails, second price resolves | `rowPrices[0].fetchFailed` is `true`; `rowPrices[1].currentPrice` is populated and `rowPrices[1].fetchFailed` is `false` |

### PortfolioSummaryTab.test.tsx

Follows the `vi.mock` / `setMock` / `Object.assign` pattern from `AggregatedSummaryTab.test.tsx`. Mocks both `useAggregatedSummary` and `usePortfolioAssetSummary`.

| Test Function | Description | Assertions |
|---------------|-------------|------------|
| `renders_loading_state_in_totals_section_while_aggregated_summary_loads` | `useAggregatedSummary` returns `isLoading: true` | Loading indicator rendered in totals section |
| `renders_error_state_in_totals_section_on_aggregated_summary_failure` | `useAggregatedSummary` returns `error` | ErrorState with Retry button rendered in totals area |
| `renders_loading_state_in_table_section_while_items_load` | `usePortfolioAssetSummary` returns `isLoading: true` | Loading indicator rendered in table section |
| `renders_error_state_in_table_section_on_items_fetch_failure` | `usePortfolioAssetSummary` returns `error` | ErrorState with Retry button rendered in table section; totals section still visible above |
| `renders_table_with_correct_column_headers` | Both hooks return data | `<th>` elements with text: Asset Name, First Investment, Quantity, Total Invested, % Portfolio, Current Value, % Profit |
| `renders_asset_row_with_correctly_formatted_values` | Known item (first date, N8 quantity, N2 invested, one-decimal weight) | `assetName`, `firstInvestmentDate` as `DD/MM/YYYY`, `currentQuantity` N8, `totalInvested` N2, `portfolioWeight` as `23.4%` |
| `renders_per_cell_loading_indicator_while_price_loads` | `rowPrices[0].isLoading: true` | Current Value cell shows `...`; `% Profit` cell shows `...` |
| `renders_current_value_when_price_resolves` | `rowPrices[0].currentPrice: 10.50`, item `currentQuantity: 25` | Current Value cell contains formatted `262.50` |
| `renders_correct_profit_percent` | `currentValue: 262.50`, `totalInvested: 250.00` | `% Profit` cell shows formatted `5.00%` |
| `renders_dash_in_current_value_and_profit_on_price_failure` | `rowPrices[0].fetchFailed: true` | Current Value and `% Profit` cells both show `—` |
| `renders_dash_in_profit_when_total_invested_is_zero` | `totalInvested: 0`, price available | `% Profit` cell shows `—`; Current Value cell shows computed value |
| `applies_green_class_to_positive_profit` | `currentValue > totalInvested` | `% Profit` element has `portfolio-summary__profit--green` class |
| `applies_red_class_to_negative_profit` | `currentValue < totalInvested` | `% Profit` element has `portfolio-summary__profit--red` class |
| `renders_empty_string_for_null_first_investment_date` | `firstInvestmentDate: null` | First Investment cell content is empty |
| `totals_section_is_unaffected_when_table_section_errors` | Table error, aggregated summary resolves with data | Three totals (Total Bought, Total Sold, Total Credits) rendered; ErrorState present in table section |

### Acceptance Test Mapping

| PRD Acceptance Criterion (Section 9 — F02) | Covered By |
|---------------------------------------------|------------|
| Selecting a Portfolio node renders `PortfolioSummaryTab` | `renders_table_with_correct_column_headers` + `DetailPanel.test.tsx` (regression) |
| Selecting a Broker node still renders `AggregatedSummaryTab` (regression) | `DetailPanel.test.tsx` existing tests |
| Selecting an Asset node still renders `AssetSummaryTab` (regression) | `DetailPanel.test.tsx` existing tests |
| Three totals (green/red/blue) at top | `totals_section_is_unaffected_when_table_section_errors`; colour coverage in existing `AggregatedSummaryTab` tests |
| Per-asset table with 7 correct columns | `renders_table_with_correct_column_headers` |
| Current Value and % Profit populate automatically | `renders_current_value_when_price_resolves` + `fires_getCurrentPrice_for_each_item_after_fetch` |
| Failed price fetch shows `"—"`, other rows unaffected | `renders_dash_in_current_value_and_profit_on_price_failure` + `failed_price_for_one_row_does_not_affect_other_rows` |
| `% Profit` green when positive, red when negative | `applies_green_class_to_positive_profit` + `applies_red_class_to_negative_profit` |
| F01 failure shows ErrorState with Retry; totals unaffected | `renders_error_state_in_table_section_on_items_fetch_failure` + `totals_section_is_unaffected_when_table_section_errors` |
| `CurrentValue = CurrentPrice × CurrentQuantity` | `renders_current_value_when_price_resolves` |
| `% Profit = (CurrentValue − TotalInvested) / TotalInvested × 100` | `renders_correct_profit_percent` |
| `"—"` in `% Profit` when `TotalInvested` is 0 | `renders_dash_in_profit_when_total_invested_is_zero` |

### Cross-Feature Integration Tests

| PRD Section 9 — Cross-Feature Criterion | Covered By |
|------------------------------------------|------------|
| `ticker` and `exchange` from F01 used without modification in `GET /prices/current` calls | `fires_getCurrentPrice_for_each_item_after_fetch` — asserts exact `exchange` and `ticker` values passed to `getCurrentPrice` |
| `totalInvested` and `currentQuantity` from F01 used without transformation in `CurrentValue` and `% Profit` computation | `renders_current_value_when_price_resolves` + `renders_correct_profit_percent` — use known F01 field values and verify exact output |
