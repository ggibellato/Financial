# Implementation Plan: Transactions Monthly Investment Chart — Web Frontend

**Prerequisites:**
- F03 (Broker & Portfolio Transactions Aggregation Service — Application Layer) already merged to `main`; both transaction endpoints are already implemented
- F04 (Chart Period Filter — YTD Extension) already merged to `main`; the shared `utils/periodFilter.ts` module is already implemented and in use by the Credits chart
- `recharts` is already a project dependency; `BarChart` is already used by `CreditsTab`, `LineChart` is not yet used anywhere and will be introduced by this feature

### Stage 1: Type Contract and API Client

**1. Transaction summary DTO type and client methods** - Add the frontend type mirroring the backend's combined transaction DTO, and the two client methods for fetching a broker's or portfolio's combined transaction list, following the existing broker/portfolio credits method pattern.

### Stage 2: Hook Extension

**2. Broker/Portfolio fetch branching** - Extend the existing transactions hook to also fetch the combined transaction list for Broker and Portfolio node selection, alongside its existing Asset-scope fetch, following the node-type branching pattern already established by the credits hook.

**3. Chart filter and mode state** - Add period-filter and Bar/Line chart-mode state to the hook, persisted per node selection, mirroring the credits hook's existing filter/mode persistence mechanism. Reference the spec's Technical Decisions for the persistence approach.

**4. Zero-filling monthly aggregation** - Add the aggregation function that computes each month's net-invested value (Buy total minus Sell total) across the selected period's full date range, including months with no transactions, working identically against either the Asset-scope or Broker/Portfolio-scope transaction list without any data conversion step. Reference the spec's Technical Decisions for the zero-fill algorithm.

### Stage 3: Component

**5. TransactionsChart rendering** - Add the chart sub-component with period-filter buttons, a Bar/Line toggle, and the recharts bar/line rendering, mirroring the credits chart's existing toolbar and panel structure with a single neutral series colour. Reference the spec's Technical Decisions for the colour choice and toggle naming.

**6. TransactionsTab integration** - Replace the current Broker/Portfolio placeholder with the chart, and add the chart above the existing table for Asset selection, with no change to the table's existing behavior. Reference the spec's Component Overview for the exact rendering structure per node type.

**7. Styling** - Add the chart, filter button, and mode toggle styles, following the existing credits tab's visual pattern.

### Stage 4: Tests

**8. Hook test coverage** - Extend the existing transactions hook test file with coverage for the new fetch branching, filter/mode persistence, and the zero-filling aggregation's correctness. Reference the spec's Testing Strategy for the full list of test functions.

**9. Component test coverage** - Extend the existing transactions tab test file with coverage confirming the chart-only rendering for Broker/Portfolio, the chart-plus-table rendering for Asset, the period filter buttons, the Bar/Line toggle, and the error state, mocking `recharts` per the project's existing pattern.
