# Implementation Plan: F04 — Summary Tab — Broker/Portfolio Aggregated View

**Prerequisites:**
- F02 fully implemented — `SelectedNodeContext` must expose `nodeType`, `brokerName`, and `portfolioName`
- F03 fully implemented — `LoadingState`, `ErrorState`, `AssetSummaryTab.css` colour classes, and the `useReducer` hook pattern are all in place as reference

---

### Phase 1: Backend — Application Layer

**1. AggregatedSummaryDTO** - Create the DTO class in `Financial.Application/DTOs/` with `TotalBought`, `TotalSold`, and `TotalCredits` decimal properties. This is the single response contract shared by both the broker and portfolio endpoints.

**2. ISummaryQueryService** - Define the service interface in `Financial.Application/Interfaces/` declaring the broker summary and portfolio summary methods, following the same structure as `ICreditQueryService`.

**3. SummaryQueryService and DI registration** - Implement the service in `Financial.Application/Services/` using `IRepository` to fetch active assets for the given scope, then LINQ-aggregate Buy transaction totals, Sell transaction totals, and credit value totals into the DTO. Register the service as a singleton in `ApplicationServiceCollectionExtensions`, following the existing registration pattern for `ICreditQueryService`.

---

### Phase 2: Backend — API Layer

**4. SummaryController** - Create `Financial.Api/Controllers/SummaryController.cs` with the `[Route("summary")]` prefix. Expose one GET action for `broker/{brokerName}` and one for `portfolio/{brokerName}/{portfolioName}`, each delegating to `ISummaryQueryService` and returning `Ok(dto)`. Follow the structure of `CreditsController` for attribute routing and `ProducesResponseType` declarations.

---

### Phase 3: Frontend

**5. API types and client methods** - Add the `AggregatedSummaryDto` TypeScript interface to `Financial.Web/src/api/types.ts`, then add `getSummaryByBroker` and `getSummaryByPortfolio` to the `FinancialApiClient` interface and `createFinancialApiClient` factory in `financialApiClient.ts`, using the existing `request<T>()` helper.

**6. useAggregatedSummary hook** - Create `Financial.Web/src/hooks/useAggregatedSummary.ts` using `useReducer` for state (summary, isLoading, error, retryCount) and `useEffect` to fire the correct API method based on `selectedNode.nodeType`. The hook should reset state on node change and expose a `retry` callback, mirroring the structure of `useAssetSummary`.

**7. AggregatedSummaryTab component** - Create `Financial.Web/src/components/AggregatedSummaryTab.tsx` and co-located `AggregatedSummaryTab.css`. The component consumes `useAggregatedSummary` and renders `LoadingState` or `ErrorState` for non-data states, and three labelled N2 value rows with the established green/red/blue colour classes for data states.

**8. DetailPanel integration** - Update `Financial.Web/src/components/DetailPanel.tsx` to import `AggregatedSummaryTab` and render it in the `summary` tab branch for `Broker` and `Portfolio` node types (replacing the current placeholder). Also update the `transactions` tab branch to display the static message "Transactions are only available for individual assets" when the selected node is not an asset.
