# Implementation Plan: Transactions Monthly Investment Chart — WPF

**Prerequisites:**
- F03 (Broker & Portfolio Transactions Aggregation Service — Application Layer) already merged to `main`; `ITransactionQueryService` is already registered in DI and implemented
- F04 (Chart Period Filter — YTD Extension) already merged to `main`; `PeriodFilterHelper` is already implemented and in use by the WPF Credits tab
- F09 (Transactions Monthly Investment Chart — Web Frontend) already merged to `main`; its zero-fill algorithm and neutral-colour decision are the reference this feature mirrors on the WPF side
- `OxyPlot.Wpf` is already a project dependency; `RectangleBarSeries` is already used by `CreditsChartBuilder`, `LineSeries` is not yet used anywhere and will be introduced by this feature

### Stage 1: Aggregation and Chart Building

**1. Monthly net-invested aggregator** - Add the pure, independently testable calculation that groups transactions by calendar month (Buy minus Sell) and zero-fills every month across the selected period's actual date range, working identically against either the Asset-scope or Broker/Portfolio-scope transaction shape via a minimal tuple projection at each call site. Reference the spec's Technical Decisions for the zero-fill algorithm and the shared-input-shape approach.

**2. Chart type enum and option view models** - Add the Bar/Line chart-type enum and the small view models backing its toggle buttons and the transactions period-filter buttons, mirroring the existing Credits tab's equivalent view models field-for-field.

**3. Transactions chart builder** - Add the OxyPlot `PlotModel` builder producing a single-series Bar or Line chart from the aggregated monthly data, using one neutral colour regardless of sign, plus the month-label density adjustment for narrow plot widths. Reference the spec's Component Overview for the exact mirroring of `CreditsChartBuilder`.

### Stage 2: ViewModel Wiring

**4. Async Broker/Portfolio transaction fetch** - Extend `AssetDetailsViewModel` with the injected transaction query service and the two new async load methods for Broker and Portfolio scope, following the same cancellable background-fetch pattern already used for the broker breakdown pie charts. Reference the spec's Technical Decisions for why this mirrors `LoadBrokerBreakdown` rather than a synchronous parameter-passing approach.

**5. Filter and chart-mode state with per-node persistence** - Add the transactions period-filter and Bar/Line chart-mode selection state to `AssetDetailsViewModel`, persisted per node selection in a dictionary independent from the existing Credits tab's persistence, reusing the existing node-identity key-building logic. Reference the spec's Technical Decisions for the persistence approach.

**6. Asset-scope chart integration** - Extend the existing asset-loading path to rebuild the transactions chart from the already-loaded transaction collection with no new fetch, and ensure the aggregate-view flag and all new state reset correctly when switching between Asset, Broker, Portfolio, and cleared selections. Reference the spec's Component Overview for exactly which existing methods are extended.

**7. Navigation dispatch and DI wiring** - Wire the two new async load calls into the existing Broker/Portfolio node-selection dispatch path, and thread the newly injected service through the app's dependency injection constructor chain. Reference the spec's Component Overview for the exact call sites.

### Stage 3: UI

**8. Transactions tab template split** - Replace the Transactions tab's single always-visible `DataGrid` with two selectable templates: one for Asset selection (existing `DataGrid` plus the new chart above it in a resizable split, mirroring the Credits tab's asset template) and one for Broker/Portfolio selection (chart only, no `DataGrid`, no "New" button). Reference the spec's Technical Decisions for the resizable-split choice and the Component Overview for the exact template structure to mirror.

**9. Chart interaction wiring** - Wire the period-filter buttons, the Bar/Line toggle, and the plot-width-driven label-density adjustment into the new templates, following the exact binding and code-behind pattern already used by the Credits tab's chart.

### Stage 4: Tests

**10. Aggregator test coverage** - Add unit tests for the new monthly aggregation covering zero-fill correctness across period filters, Buy-minus-Sell math, the All-Time earliest-transaction fallback, and empty-input handling. Reference the spec's Testing Strategy for the full list of test functions.

**11. ViewModel test coverage** - Add unit tests for the new `AssetDetailsViewModel` behaviour covering loading/error/success states for both new async load methods, Asset-scope reuse of already-loaded data with no new fetch, per-node filter/mode persistence independent of the Credits tab's own persistence, and state reset on `Clear()`.

**12. Navigation dispatch test coverage** - Extend the existing navigation view model tests to confirm Broker and Portfolio node selection dispatch the two new load calls with the correct arguments, and that Asset node selection does not.
