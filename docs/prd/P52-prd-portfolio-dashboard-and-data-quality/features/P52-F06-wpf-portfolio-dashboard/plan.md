# P52-F06 — WPF — Portfolio Dashboard — Implementation Plan

## Prerequisites

- F01, F02, F03, F04 (backend) and F05 (React) are merged to `main`; this branch
  (`feature/p52-f06-wpf-portfolio-dashboard`) is current with `main`.
- Read `spec.md` in full before starting any phase, especially the "Decisions / Assumptions"
  subsection — every phase below references decisions by number.
- `docs/ui/wpf.md`, `docs/ui/review-checklist.md`, and F05's own spec (the UX reference this
  feature must match) are required reading before touching any XAML.
- Per this PRD's own scale (comparable to F05), implementation proceeds as **4 separate PRs**,
  mirroring F05's own Part 1–4 split, to respect the 8-non-test-file PR-size guideline while each
  part still ships a genuine, independently mergeable vertical slice (spec.md Decision — this
  plan's own phase boundaries were chosen to match F05's precedent exactly: nav/shell first, then
  one panel per subsequent PR in the PRD's own dependency-free order F02→F03→F04). Each phase below
  = one PR = one commit-per-phase per this project's `implement-feature` convention.

## Phase 1 — Nav entry, ViewModel/View shell, KPI tiles (F01)

1. **Nav entry.** Add the new `dashboard` `NavChild` to `NavTree.cs`'s `investments` category
   (Decision 1).
2. **KPI tiles ViewModel.** Add `DashboardKpiTilesViewModel` (calls
   `IPortfolioDashboardService.GetDashboardAsync()`, exposes the 8 native + 8 converted tiles,
   `IsPartial`/`UnvaluedHoldingCount` notice, `ViewMissingPriceHoldingsCommand`) per spec.md
   Decisions 3, 5–8.
3. **KPI tiles View.** Add `DashboardKpiTilesView.xaml` (`UniformGrid` of 8 tiles, reporting-
   currency secondary block reusing `PortfolioSummaryView`'s existing structure, `IsPartial`
   notice) per Decisions 5–7.
4. **Shell ViewModel.** Add `DashboardViewModel` composing `DashboardKpiTilesViewModel` alone for
   this phase (the other three panel properties are added in later phases); owns
   `ShowPageLevelError`/`RetryAllCommand` scaffolding sized for one panel now, extended in Phases
   2–4 (Decision 4).
5. **Shell View.** Add `DashboardView.xaml`/`.xaml.cs` composing `DashboardKpiTilesView` inside the
   layout `Grid`/`ScrollViewer` shell Decision 16 defines, with the other three panel slots
   present but empty (`Grid.Row` reserved) until their own phase lands.
6. **Wiring.** Register the new ViewModels/View in `App.xaml.cs`; add `dashboardView` to
   `MainWindow.xaml.cs`'s constructor parameters and `viewsByKey`; manually compose
   `DashboardViewModel` in `MainWindow.xaml.cs` using the already-captured
   `_navigationViewModel`/`_navigationViewModelHistoric` fields (Decision 13's composition
   pattern, even though this phase's `DashboardViewModel` does not yet call `SelectHolding` —
   wiring the constructor parameters now avoids a signature churn in Phase 3).
7. **Tests.** Unit tests for `DashboardKpiTilesViewModel` (loading/error/values/partial-notice/
   reporting-currency states) and for `DashboardViewModel`'s page-level error state as it exists
   with one panel. Manual verification: run the app, confirm the Dashboard entry appears first
   under Investments and the KPI tiles render real data.

## Phase 2 — Allocation breakdown (F02)

1. **Allocation ViewModel.** Add `AllocationBreakdownViewModel` (calls
   `IAllocationBreakdownService.GetAllocationBreakdown()`, `SelectedDimension` defaulting to
   `Class`, per-dimension `AllocationEntryRowViewModel` collection) per Decisions 10–11.
2. **Chart builder.** Add `AllocationPieChartBuilder.cs`, copying `BrokerBreakdownChartBuilder`'s
   palette/series/tracker setup (Decision 11).
3. **Allocation View.** Add `AllocationBreakdownView.xaml` (`TabControl` + OxyPlot `PlotView` +
   legend `DataGrid` with `Width="*"` label column and right-aligned numeric columns) per Decisions
   10–11.
4. **Wiring.** Add `AllocationBreakdownViewModel`/`AllocationBreakdownView` to `DashboardViewModel`/
   `DashboardView`'s composition (the two-column `Grid` from Decision 16 — Allocation's column now
   has real content; Warnings' column stays reserved until Phase 3); extend `ShowPageLevelError`
   to require this panel's own error state too.
5. **Tests.** Unit tests for `AllocationBreakdownViewModel` (dimension switching, empty-dimension
   state, read-only assertion) and `AllocationPieChartBuilder`. Manual verification: confirm the
   four dimension tabs render real allocation data with a legend table.

## Phase 3 — Warnings panel + click-through (F03)

1. **Warnings ViewModel.** Add `DataQualityWarningsViewModel` (calls
   `IDataQualityReportService.GenerateReport()`, builds severity-ordered
   `WarningCategoryViewModel`/`WarningFindingRowViewModel` collections, hide-if-zero, all-zero
   confirmation, `NavigationError`, `ExpandCategory`) per Decisions 12, 14–15.
2. **Tree click-through.** Add `MainNavigationViewModelBase.SelectHolding` (walks `RootNodes`,
   selects via `IsSelected`, expands ancestors, returns match status) per Decision 13.
3. **Warnings View.** Add `DataQualityWarningsView.xaml` (`ui:InfoBar` for empty/error states, one
   `Expander` per nonzero category, clickable finding rows) per Decisions 12, 14–15.
4. **Cross-panel wiring.** Add `DashboardViewModel.NavigateToHoldingCommand` (try Active tree's
   `SelectHolding`, fall through to Historic, raise `NavigateToTreeRequested` on success or set
   `Warnings.NavigationError` on a double-miss) and `MainWindow.xaml.cs`'s
   `NavigateToTreeRequested` handler (switches the shell pane via the already-constructed
   `MainShellViewModel.SelectItemCommand`) per Decision 13. Wire `DashboardKpiTilesViewModel
   .ViewMissingPriceHoldingsCommand` → `DashboardViewModel.ExpandMissingPriceRequested` →
   `Warnings.ExpandCategory(MissingPrice)` + the narrowly-scoped `BringIntoView()` code-behind, per
   Decision 8.
5. **Layout completion.** Fill the Warnings column of Decision 16's two-column `Grid`; extend
   `ShowPageLevelError` to require all four panels' error states (still three of four until Phase
   4).
6. **Tests.** Unit tests for `DataQualityWarningsViewModel` (severity order, hide-if-zero, all-
   zero, stale-valuation count-only, `ExpandCategory`, `NavigationError`), for
   `MainNavigationViewModelBase.SelectHolding` (match/no-match/idempotent-reselect, selecting
   through `IsSelected` per the testing guide's documented pitfall), and for
   `DashboardViewModel.NavigateToHoldingCommand`/`ExpandMissingPriceRequested` forwarding. Manual
   verification: click a warning finding, confirm the shell switches to the correct tree with the
   right node selected and expanded; click the KPI partial notice's "View affected holdings" link,
   confirm the Missing Price category expands and scrolls into view.

## Phase 4 — Upcoming income + final AC-tracing (F04)

1. **Upcoming income enum.** Add `UpcomingIncomeWindow` (Decision 9).
2. **Upcoming income ViewModel.** Add `UpcomingIncomeViewModel` (calls
   `IUpcomingIncomeService.GetUpcomingIncome()`, `SelectedWindow` defaulting to `Days90`, client-
   side window filtering with no re-fetch on window change, empty state) per Decision 9.
3. **Upcoming income View.** Add `UpcomingIncomeView.xaml` (chip-style window selector copying
   `PriceHistoryView.xaml`'s filter row, read-only `DataGrid`, empty-state text) per Decision 9.
4. **Layout completion.** Add `UpcomingIncomeView` as the full-width final section of
   `DashboardView` (Decision 16); `ShowPageLevelError` now requires all four panels' error states,
   completing Decision 4's page-level error state as fully specified.
5. **AC-tracing tests.** Add the WPF Integration (DI-resolution) suite
   (`DashboardCompositionTests.cs`) proving the four panel ViewModels resolve and load through the
   real Application DI graph against fixture data, and add/confirm the AC ids this feature
   introduces to PRD §9's F06 group and the Cross-Feature Integration group
   (`P52-F06-wpf-portfolio-dashboard-01..03`), checking off this feature's own three §9 boxes and
   the four Cross-Feature Integration boxes once every assertion in spec.md §7 passes.
6. **Full-feature manual verification.** Run the built app; walk every state spec.md §6 defines
   (loading, empty, partial, error, all-4-failed, success) for all four panels; confirm WPF/React
   parity against the running `Financial.Web` dev server for the same fixture data (per
   `docs/rules/ui.md`'s "Completion requirement": both platforms actually run and looked at, not
   just green tests); run `docs/ui/review-checklist.md` against every touched view/component.
