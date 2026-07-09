# Implementation Plan: Broker Breakdown Pie Charts — WPF

**Prerequisites:**
- F02 (Broker Portfolio & Asset Breakdown Service — Application Layer) already merged to `main`; `IBrokerBreakdownQueryService` is already implemented and registered in DI
- F06 (Broker & Portfolio Totals Display — WPF) already merged to `main`; `BrokerSummaryTemplate` and `IsBrokerView` already exist
- `OxyPlot.Wpf` is already a project dependency; `PieSeries`/`PieSlice` are available but not yet used anywhere in the codebase

### Stage 1: Chart Builder and Pie Data Types

**1. BrokerBreakdownChartBuilder** - Add a new static class that builds an OxyPlot `PlotModel` containing a single pie series from a list of named values, using a shared categorical colour palette and a tracker format string showing name, value, and percentage. Reference the spec's Technical Decisions for the palette and tracker format.

**2. PortfolioBreakdownPieItem** - Add a small immutable record pairing a portfolio's name with its built pie `PlotModel`, used to back the per-portfolio pies collection.

### Stage 2: ViewModel Async Loading

**3. AssetDetailsViewModel breakdown state and async load** - Add the new constructor dependency, the overall and per-portfolio pie properties, loading/error state, and a `LoadBrokerBreakdown` method that fetches and builds the chart data on a background task, following the existing async-fetch pattern already used for per-row price fetching. Reference the spec's Technical Decisions for why this service is owned directly by the view model rather than passed through the navigation view model base.

**4. Reset behavior** - Ensure breakdown state is correctly cleared and any in-flight fetch is cancelled whenever the view model clears or switches away from Broker view, mirroring the existing reset points for other Broker-scoped state.

**5. IAssetDetailsViewModel contract update** - Add the new load method to the interface.

### Stage 3: Dispatch and DI Wiring

**6. Broker node dispatch** - Call the new load method alongside the existing broker summary load when a Broker node is selected, with no change to how the existing totals and credits data are fetched.

**7. Constructor wiring** - Add the new service dependency to the concrete navigation view model's constructor and pass it through to the asset details view model, relying on existing dependency injection registration.

### Stage 4: WPF Template

**8. BrokerSummaryTemplate breakdown UI** - Extend the template with a scrollable container, a loading indicator, an inline error message, an empty-state message, the overall breakdown pie, and an items control rendering one pie per eligible portfolio. Reference the spec's Component Overview for the exact layout and binding structure.

### Stage 5: Tests

**9. ViewModel test coverage** - Extend the existing Broker summary test file with coverage for the new load method's loading state, successful population of both pie properties, error handling, and reset behavior on clear and node switching. Reference the spec's Testing Strategy for the full list of test functions.

**10. Routing test coverage** - Extend the routing test double and its tests to confirm the new load method is called with the correct broker name on Broker node selection.
