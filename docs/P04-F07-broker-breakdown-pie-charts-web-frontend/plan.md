# Implementation Plan: Broker Breakdown Pie Charts — Web Frontend

**Prerequisites:**
- F02 (Broker Portfolio & Asset Breakdown Service — Application Layer) already merged to `main`; `GET /summary/broker/{brokerName}/breakdown` is already implemented and returns the fully filtered, sorted breakdown
- `recharts` is already a project dependency (used by `CreditsTab`)

### Stage 1: Type Contract and API Client

**1. Breakdown DTO types** - Add the frontend types mirroring the backend's portfolio/asset breakdown DTOs, matching the shape already returned by the breakdown endpoint.

**2. API client method** - Add a client method for fetching a broker's breakdown, following the existing broker/portfolio summary method pattern.

### Stage 2: Data Hook

**3. useBrokerBreakdown hook** - Add a new hook that fetches the breakdown only when a Broker node is selected, exposing loading, error, retry, and the fetched data, mirroring the existing aggregated-summary hook's reducer shape but without the Portfolio-scope fetch path.

### Stage 3: Component and Integration

**4. BrokerBreakdownCharts component** - Add the new component that renders its own independent loading and error (with retry) states, an empty-state message when there are no eligible portfolios, and otherwise the overview "Portfolio Breakdown" pie chart followed by one pie chart per eligible portfolio. Reference the spec's Technical Decisions for the palette, tooltip, and legend approach.

**5. Percentage computation and tooltip content** - Compute each slice's percentage of its chart's total client-side and render it together with the slice name and formatted value in a custom tooltip, per the spec's Technical Decisions.

**6. Integration into AggregatedSummaryTab** - Render the new component below the existing totals grid, only when the selected node is a Broker, leaving Portfolio and Asset selection behavior unchanged.

### Stage 4: Tests

**7. useBrokerBreakdown test coverage** - Add unit tests covering fetch gating by node type, loading/success/error/retry states, and reset behavior on node change. Reference the spec's Testing Strategy for the full list of test functions.

**8. BrokerBreakdownCharts test coverage** - Add unit tests covering the overview and per-portfolio pies, percentage computation, the empty state, the error/retry state, and the independent loading state, mocking `recharts` per the project's existing pattern.

**9. AggregatedSummaryTab integration test** - Extend the existing test file to confirm the new component renders only for Broker node selection and not for Portfolio selection.
