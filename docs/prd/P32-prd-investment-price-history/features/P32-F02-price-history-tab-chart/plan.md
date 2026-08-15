# Implementation Plan: F02. Price History Tab & Chart

**Prerequisites:**
- F01 (Price History Recording) is fully merged into `main` (PRs #417-#421) — `Asset.PriceHistory`, `IPriceService`, and the `PUT`/`DELETE /prices` endpoints all exist and are in production shape.

**PR sizing note:** each phase below is scoped to land as its own small, independently reviewable PR (roughly 4-6 files each). The user reviews and approves each PR before it merges and before the next phase starts.

### Stage 1: WPF ViewModel State & Actions

**1. Price History View State** - Introduce the per-asset-context cached filter state and the chart-building logic for a single-series price-over-time line chart, following the same shape as the existing Credits view state and chart builder but without the type/stacking dimension Credits needs and Price History doesn't.

**2. Price Actions** - Add the orchestration layer for adding, updating, and deleting a price entry through the API, following the existing Credit actions pattern.

**3. Asset Details ViewModel Wiring** - Extend the asset details ViewModel with the properties, commands, and state needed to drive the new tab, following the same wiring already in place for Credits.

### Stage 2: WPF Dialog & Tab UI

**4. Price Dialog** - Add a modal dialog for adding, editing, and confirming deletion of a price entry, letting the user pick any date, following the existing Credit dialog pattern.

**5. Price History Tab** - Add the new tab to the asset details view, positioned after the existing Credits tab, showing the list and chart built in Stage 1 and wired to the dialog from this stage.

### Stage 3: Web

**6. Price History Data Hook** - Add the hook that loads an asset's price history, applies the period filter, derives chart data, and manages the inline add/edit form state, following the existing Credits hook pattern.

**7. Price History Tab Component** - Add the new tab component (list, chart, inline form) and wire it into the asset details tab container alongside the existing Credits and Transactions tabs.
