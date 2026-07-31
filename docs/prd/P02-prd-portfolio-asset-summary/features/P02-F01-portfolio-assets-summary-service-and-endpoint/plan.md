# Implementation Plan: F01 — Portfolio Assets Summary Service and Endpoint (Update)

**Prerequisites:**
- All existing dependencies (`FluentAssertions`, `xUnit`, `WebApplicationFactory`) already present in the test projects
- `NavigationMapper.CalculateTotals` already returns `TotalCredits` as the third tuple element — the existing `PortfolioAssetSummaryQueryService` discards it with `_`
- The existing `PortfolioAssetSummaryQueryService`, `PortfolioAssetSummaryItemDTO`, and endpoint are already implemented and passing; this update extends them without breaking existing behaviour

---

### Phase 1: Application Layer — New DTO and Extended Item DTO

**1. AssetCashFlowDTO** — Create the new `AssetCashFlowDTO` sealed class in `Financial.Application/DTOs/`. It holds two properties: the event date and the cash-flow amount (negative for outflows, positive for inflows). See spec Section 4 for the property names and types.

**2. PortfolioAssetSummaryItemDTO Update** — Add `TotalCredits` (decimal) and `CashFlows` (`IReadOnlyList<AssetCashFlowDTO>`) to the existing sealed class, following the same `{ get; init; }` pattern used by the existing properties. See spec Section 4.

---

### Phase 2: Application Layer — Service Update

**3. PortfolioAssetSummaryQueryService Update** — Extend the private `AssetComputedData` record with `TotalCredits` and `CashFlows`. In `ComputeAssetData`, stop discarding the third element of the `NavigationMapper.CalculateTotals` tuple (capture it as `totalCredits`). Build the cash-flow list by iterating `asset.Transactions` for Buy and Sell entries and `asset.Credits` for income entries; sort the combined list ascending by date. Propagate both new fields through `ToDTO`. See spec Sections 3 and 4 for the sign convention and sort rules.

---

### Phase 3: Tests

**4. PortfolioAssetSummaryQueryServiceTests — New Test Cases** — Add the nine new unit tests to the existing test class. Cover `TotalCredits` summation (with credits, without credits), cash-flow sign convention for Buy/Sell/Credit, sort order, empty list when no events, and that the Buy amount uses `TotalPrice` (unit price × quantity + fees). See spec Section 7 for the full test function list and assertions.

**5. SummaryEndpointsTests — New Integration Test** — Add the `GetPortfolioAssetsSummary_Returns200WithNewFields` test to verify that the HTTP response now includes `totalCredits` and `cashFlows` fields for real test data. See spec Section 7.
