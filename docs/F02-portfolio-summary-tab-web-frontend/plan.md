# Implementation Plan: F02 — Portfolio Summary Tab — Web Frontend

**Prerequisites:**
- F01 endpoint (`GET /api/v1/financial/summary/portfolio/{brokerName}/{portfolioName}/assets`) already implemented and deployed
- No new npm packages required; all existing frontend dependencies (`vitest`, `@testing-library/react`, React 18) are already present

---

### Phase 1: API Client Extension

**1. PortfolioAssetSummaryItemDto Type** — Add the `PortfolioAssetSummaryItemDto` interface to `Financial.Web/src/api/types.ts`. See spec Section 5 for all nine field names and TypeScript types.

**2. FinancialApiClient Method** — Add `getPortfolioAssetsSummary` to the `FinancialApiClient` interface and its implementation in `financialApiClient.ts`. See spec Section 5 for the URL pattern, parameter encoding, and return type.

---

### Phase 2: Hook

**3. usePortfolioAssetSummary Hook** — Create `Financial.Web/src/hooks/usePortfolioAssetSummary.ts`. The hook manages a `useReducer` with `items`, `rowPrices`, `isLoading`, `error`, and `retryCount`; on Portfolio node selection it fetches static data from the F01 endpoint; on success it initialises per-row loading state and fires one `getCurrentPrice` call per item in parallel, each dispatching independently as it resolves or fails. See spec Sections 3 and 4 for state shape, action types, and reducer rules.

---

### Phase 3: Component and Routing

**4. PortfolioSummaryTab Component** — Create `Financial.Web/src/components/PortfolioSummaryTab.tsx` and `PortfolioSummaryTab.css`. The component renders the aggregated totals section (reusing `useAggregatedSummary`) and the per-asset table (using `usePortfolioAssetSummary`), each with independent loading and error states. Per-cell `...` loading indicators and `"—"` fallbacks apply per the PRD UX flow. See spec Sections 4 and 5.

**5. DetailPanel Modification** — Update `Financial.Web/src/components/DetailPanel.tsx` to render `PortfolioSummaryTab` when the selected node is a Portfolio and `AggregatedSummaryTab` only when it is a Broker. Import `PortfolioSummaryTab` and update the summary tab routing condition. See spec Section 4.

---

### Phase 4: Tests

**6. usePortfolioAssetSummary Tests** — Create `Financial.Web/src/hooks/usePortfolioAssetSummary.test.ts` following the `createWrapper` / `vi.mock` pattern from `useAggregatedSummary.test.ts`. Cover all state transitions, parallel price dispatch, retry, node-change reset, and row isolation on partial price failure. See spec Section 7 for the full test function list.

**7. PortfolioSummaryTab Tests** — Create `Financial.Web/src/components/__tests__/PortfolioSummaryTab.test.tsx` following the `vi.mock` / `setMock` pattern from `AggregatedSummaryTab.test.tsx`. Mock both `useAggregatedSummary` and `usePortfolioAssetSummary`. Cover all render states, value formatting, profit colour coding, and regression checks for unaffected node types. See spec Section 7 for the full test function list.
