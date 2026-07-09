# Implementation Plan: Credits Chart Bar/Line Toggle — Web Frontend

**Prerequisites:**
- F09 (Transactions Monthly Investment Chart — Web Frontend) already merged to `main`; its Bar/Line toggle button styling and `LineChart`/`Line` usage are the reference pattern this feature mirrors for the Credits chart
- `recharts` is already a project dependency; `LineChart`/`Line` are already used by the Transactions chart, `BarChart`/`Bar` are already used by the Credits chart

### Stage 1: Dynamic Per-Type Aggregation

**1. Credit-type data shape refactor** - Replace the hardcoded two-field month bucket with a dynamic per-type structure, computing which credit types are present directly from the data rather than assuming a fixed set, and computing each month's combined total alongside the per-type breakdown. Reference the spec's Technical Decisions for why this refactor is required now rather than deferred.

### Stage 2: Chart Type State

**2. Chart display mode state and persistence** - Add the Bar/Line chart type selection to the credits hook, defaulting to Bar, persisted per node selection alongside the existing period filter and Stacked/Grouped selections without overwriting either. Reference the spec's Technical Decisions for the persistence approach and the type-naming decision.

### Stage 3: Component Rendering

**3. Dynamic colour palette** - Add the computed colour palette function used to assign a distinct colour per credit type, replacing the two hardcoded colour constants, shared identically between the bar and line rendering paths. Reference the spec's Technical Decisions for the palette approach.

**4. Line chart rendering** - Extend the chart panel to render a line chart when line mode is selected: a single line for the combined total when grouped, or one line per credit type when stacked. Reference the spec's Technical Decisions for the exact Grouped/Stacked line semantics.

**5. Toolbar toggle rows** - Add the new Bar/Line toggle row to the chart toolbar, and relabel the existing Stacked/Grouped toggle row, following the exact button styling and click-handling pattern the existing toggle already uses. Reference the spec's Component Overview for the exact label text.

### Stage 4: Tests

**6. Hook test coverage** - Extend the credits hook's test file with coverage for the dynamic per-type aggregation, total computation, and chart type default/persistence behaviour including its independence from the existing Stacked/Grouped persistence. Reference the spec's Testing Strategy for the full list of test functions.

**7. Component test coverage** - Extend the credits tab's test file with coverage for the Bar/Line toggle's default and click behaviour, the relabelled toggle rows, and the Grouped+Line versus Stacked+Line rendering differences, mocking `recharts` per the project's existing pattern.
