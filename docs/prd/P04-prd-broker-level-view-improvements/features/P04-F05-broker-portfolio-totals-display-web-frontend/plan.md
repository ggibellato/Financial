# Implementation Plan: Broker & Portfolio Totals Display — Web Frontend

**Prerequisites:**
- F01 (Aggregated Totals Enhancement — Application Layer) already merged to `main`; `totalInvested` is already returned by the broker and portfolio summary endpoints
- No new tools, libraries, or environment variables required

### Stage 1: Type Contract

**1. AggregatedSummaryDto totalInvested field** - Add the `totalInvested` field to the frontend's `AggregatedSummaryDto` type so it matches the shape already returned by the broker and portfolio summary endpoints. Reference the spec's Component Overview for the exact type.

### Stage 2: UI Rendering

**2. Render Total Invested in AggregatedSummaryTab** - Add a fourth field to the totals grid, positioned after Total Credits, with a colour-coding helper that mirrors the existing conditional green/red pattern used elsewhere in the app. Reference the spec's Technical Decisions for the colour and helper approach.

**3. Update grid layout** - Adjust the totals grid's CSS so the four fields form a balanced 2×2 layout, removing the full-width span currently applied to Total Credits. Reference the spec's Technical Decisions for the chosen layout.

### Stage 3: Tests

**4. Update AggregatedSummaryTab unit tests** - Extend the shared fixture with the new field and add test coverage for Total Invested's position, both colour branches, and its inclusion in the existing formatting and zero-value tests. Reference the spec's Testing Strategy for the full list of test functions.

**5. Update useAggregatedSummary hook test fixture** - Extend the mock `AggregatedSummaryDto` fixture with the new field so the existing pass-through test continues to compile and pass.

**6. Update PortfolioSummaryTab integration test** - Extend the local fixture with the new field and add a test confirming that selecting a Portfolio node renders all four totals, in the same order, via the reused `AggregatedSummaryTab` component. Reference the spec's Testing Strategy for the assertion details.
