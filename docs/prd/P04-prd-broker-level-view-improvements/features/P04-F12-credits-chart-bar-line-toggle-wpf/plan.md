# Implementation Plan: Credits Chart Bar/Line Toggle — WPF

**Prerequisites:**
- F10 (Transactions Monthly Investment Chart — WPF) already merged to `main`; its `ChartTypeMode` enum, `RectangleBarSeries`/`LineSeries` branching, and per-node chart-mode persistence are the reference pattern this feature mirrors for the Credits chart
- F11 (Credits Chart Bar/Line Toggle — Web Frontend) already merged to `main`; its Stacked+Line/Grouped+Line semantics and toggle labelling are the cross-platform contract this feature implements on WPF
- `OxyPlot.Wpf` is already a project dependency; `LineSeries` is already used by `TransactionsChartBuilder`, `RectangleBarSeries` is already used by `CreditsChartBuilder`

### Stage 1: Chart Type State

**1. Chart type enum and option view model** - Add the Bar/Line chart-type enum and its option view model for toggle-button binding, distinct from the existing Transactions chart's equivalent type. Reference the spec's Technical Decisions for the naming rationale.

**2. Per-node persistence** - Extend the existing per-node persisted view-state record with the new chart type field, alongside the already-persisted period filter and Stacked/Grouped selections, so all three persist together without overwriting each other. Reference the spec's Technical Decisions for the persistence approach.

### Stage 2: Chart Rendering

**3. Line series construction** - Extend the chart builder to construct line series from the same monthly per-type totals the existing bar rendering already uses: a single line for the combined total when grouped, or one line per credit type when stacked. Reference the spec's Technical Decisions for the exact Grouped/Stacked line semantics.

**4. Value-label rule generalization** - Extend the existing value-label annotation logic so it applies consistently across both the Stacked/Grouped and Bar/Line dimensions: single-series combinations get one total label per month, multi-series combinations get one label per credit type per month. Reference the spec's Technical Decisions for the exact rule.

### Stage 3: ViewModel and UI Wiring

**5. ViewModel wiring** - Wire the new chart type state, its toggle command, and its selection persistence into the asset details view model, extending the existing chart-rebuild call sites to pass the new mode through. Reference the spec's Component Overview for the exact methods being extended.

**6. Toolbar toggle row** - Add the new Bar/Line toggle row to both Credits chart templates (asset-level and aggregate-level), and relabel the existing Stacked/Grouped toggle row, following the exact button styling and binding pattern the existing toggle already uses. Reference the spec's Component Overview for the exact label text and template scope.

### Stage 4: Tests

**7. Chart builder test coverage** - Add unit tests for the chart builder covering Bar/Line series construction and the Grouped-single-series versus Stacked-per-type-series behaviour. Reference the spec's Testing Strategy for the full list of test functions.

**8. ViewModel test coverage** - Add unit tests for the view model's chart type default, persistence, and independence from the existing Stacked/Grouped and period-filter persistence, including that toggling rebuilds the chart from already-loaded data with no new fetch.
