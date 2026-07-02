# Implementation Plan: F02 — Portfolio Summary Tab — Web Frontend

**Prerequisites:**
- F01 endpoint (`GET /api/v1/financial/summary/portfolio/{brokerName}/{portfolioName}/assets`) already implemented and deployed, returning `totalCredits` and `cashFlows` per item
- No new npm packages required; all existing frontend dependencies (`vitest`, `@testing-library/react`, React 18) are already present

---

### Phase 1: API Client Extension and XIRR Utility

**1. PortfolioAssetSummaryItemDto and AssetCashFlowDto Types** — Add the `AssetCashFlowDto` interface (`date`, `amount`) and the `PortfolioAssetSummaryItemDto` interface to `Financial.Web/src/api/types.ts`. See spec Section 5 for all eleven field names and TypeScript types.

**2. FinancialApiClient Method** — Add `getPortfolioAssetsSummary` to the `FinancialApiClient` interface and its implementation in `financialApiClient.ts`. See spec Section 5 for the URL pattern, parameter encoding, and return type.

**3. XIRR Utility** — Create `Financial.Web/src/utils/xirr.ts` implementing Newton-Raphson with a maximum of 100 iterations and convergence tolerance 1e-7. The function accepts a sorted `{ date: Date; amount: number }[]` series and returns the annualised rate as a `number` or `null` when the series has fewer than 2 entries or the algorithm does not converge. See spec Section 4 for the full contract.

---

### Phase 2: Hook

**4. usePortfolioAssetSummary Hook** — Create `Financial.Web/src/hooks/usePortfolioAssetSummary.ts`. The hook manages a `useReducer` with `items`, `rowPrices`, `isLoading`, `error`, and `retryCount`; on Portfolio node selection it fetches static data from the F01 endpoint (items include `totalCredits` and `cashFlows`); on success it initialises per-row loading state and fires one `getCurrentPrice` call per item in parallel, each dispatching independently as it resolves or fails. See spec Sections 3 and 4 for state shape, action types, and reducer rules.

---

### Phase 3: Component and Routing

**5. PortfolioSummaryTab Component** — Create `Financial.Web/src/components/PortfolioSummaryTab.tsx` and `PortfolioSummaryTab.css`. The component renders the aggregated totals section (reusing `useAggregatedSummary`) and the 10-column per-asset table (using `usePortfolioAssetSummary`), each with independent loading and error states. Per-cell `...` loading indicators appear while prices fetch; after each price resolves the component computes `currentValue`, `% Profit`, `% Profit w/ Credits`, and `XIRR` (via the `xirr` utility) in-row. `"—"` fallbacks apply for unavailable prices, zero `totalInvested`, and XIRR non-convergence. `% Profit`, `% Profit w/ Credits`, and XIRR are colour-coded green/red. See spec Sections 4 and 5.

**6. DetailPanel Modification** — Update `Financial.Web/src/components/DetailPanel.tsx` to render `PortfolioSummaryTab` when the selected node is a Portfolio and `AggregatedSummaryTab` only when it is a Broker. Import `PortfolioSummaryTab` and update the summary tab routing condition. See spec Section 4.

---

### Phase 4: Tests

**7. XIRR Utility Tests** — Create `Financial.Web/src/utils/xirr.test.ts`. Cover the correct annualised rate for known inputs (verified against an Excel XIRR reference), `null` return for fewer than 2 entries, and `null` return for non-convergent series. See spec Section 7 for the full test function list.

**8. usePortfolioAssetSummary Tests** — Create `Financial.Web/src/hooks/usePortfolioAssetSummary.test.ts` following the `createWrapper` / `vi.mock` pattern from `useAggregatedSummary.test.ts`. Cover all state transitions, parallel price dispatch, retry, node-change reset, and row isolation on partial price failure. Verify that `items` includes `totalCredits` and `cashFlows`. See spec Section 7 for the full test function list.

**9. PortfolioSummaryTab Tests** — Create `Financial.Web/src/components/__tests__/PortfolioSummaryTab.test.tsx` following the `vi.mock` / `setMock` pattern from `AggregatedSummaryTab.test.tsx`. Mock `useAggregatedSummary`, `usePortfolioAssetSummary`, and the `xirr` utility. Cover all render states, all 10 column headers, value formatting, Total Credits immediate rendering, per-cell loading indicators, profit and XIRR colour coding, XIRR non-convergence and insufficient-data dash, and regression checks for unaffected node types. See spec Section 7 for the full test function list.
