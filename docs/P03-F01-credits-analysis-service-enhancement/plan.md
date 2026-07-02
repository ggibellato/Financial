# Implementation Plan: P03-F01 — Credits Analysis — Service Enhancement

**Prerequisites:**
- All existing dependencies (xUnit, FluentAssertions, WebApplicationFactory) already present in the test projects
- `PortfolioAssetSummaryQueryService`, `PortfolioAssetSummaryItemDTO`, and the endpoint are already implemented and passing all P02 tests
- `Credit.Date` and `Credit.Value` on the domain entity already provide all data needed for the new computations

---

### Phase 1: Application Layer — DTO Extension

**1. PortfolioAssetSummaryItemDTO Update** — Add the seven new properties to the existing sealed class following the `{ get; init; }` pattern. Non-nullable fields with zero defaults (`LastMonthCredits`, `CurrentMonthCredits`) must not be declared nullable; nullable fields use the appropriate `?` types. See spec Sections 4 and 5 for property names and types.

---

### Phase 2: Application Layer — Service Update

**2. PortfolioAssetSummaryQueryService Update** — Capture `DateTime.Today` once at the start of `GetPortfolioAssetsSummary` and pass it through to `ComputeAssetData`. Extend the private `AssetComputedData` record with the seven new fields. Add a new private static method `ComputeCreditsAnalysis` that computes all seven fields for a single asset: determines the last credit month by excluding future-dated credits, sums credits in that month, detects payment frequency from all distinct credit months using the average-gap algorithm, derives estimated annual values, and sums current-calendar-month credits. Update `ToDTO` to propagate all seven new fields. See spec Sections 3, 4, and 5 for field formulas, frequency ranges, and null rules.

---

### Phase 3: Tests

**3. PortfolioAssetSummaryQueryServiceTests — New Unit Tests** — Add the twenty new unit test cases to the existing test class. Cover `LastMonthCredits` (most-recent-month sum, zero when no credits), `LastCreditMonth` (YYYY-MM format, null when no credits, future-date exclusion from both month determination and sum), `LastMonthCreditsPercent` (computed value, null when invested absent or last month null), the four frequency-detection scenarios (monthly, quarterly, four-month, undetectable by count and by gap range), `EstimatedAnnualCredits` (computed, null when no frequency), `EstimatedAnnualPercent` (computed, null when credits or invested absent), and `CurrentMonthCredits` (current-month sum and zero when none). See spec Section 7 for the full test function list and assertions.

**4. SummaryEndpointsTests — New Integration Test** — Add `GetPortfolioAssetsSummary_Returns200WithCreditsAnalysisFields` to assert that the HTTP response includes all seven new fields with valid values for the real test data file. See spec Section 7.
