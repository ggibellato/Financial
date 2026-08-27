# Implementation Plan: F02. Sortable Columns — WPF

**Prerequisites:**
- `Financial.App` dev environment (Windows, .NET, WPF designer tooling already configured per repo `README.md`)
- No new NuGet packages — implemented with existing WPF APIs (`DataGrid.Sorting`, `ListCollectionView.CustomSort`)

### Stage 1: Shared Sort Infrastructure

**1. Sort Cycle and Comparer** - Build `SortCycle`, the pure state-transition logic for the unsorted → ascending → descending → unsorted cycle (resetting to ascending whenever a different column is clicked), and `NullLastComparer`, the pure, type-aware value comparer that keeps null values last in both directions.

**2. Sortable Columns Behavior** - Build the `SortableColumnsBehavior` attached behavior: an `IsEnabled` attached property that subscribes to a `DataGrid`'s `Sorting` event, resolves the clicked column's `SortMemberPath` against each row via reflection, advances the sort cycle, applies a `ListCollectionView.CustomSort` built on the new comparer (or clears it when returning to unsorted), and keeps each column's `SortDirection` (and therefore its header arrow) in sync.

**3. Global Opt-In** - Wire `SortableColumnsBehavior.IsEnabled="True"` into `App.xaml`'s existing global `DataGrid` style, so every grid in the app gains the 3-state, null-last sort by default without further per-view changes.

**4. Unit Tests** - Cover every cycle transition in `SortCycle` and every null/typed-value comparison direction in `NullLastComparer`.

### Stage 2: Missing SortMemberPath Fixes

**5. Portfolio Summary Grid** - Add `SortMemberPath` to the 9 `DataGridTemplateColumn`s across the Active and Historic templates (Current Value, Current Price, %, w/ Credits, XIRR, Realized Gain/Loss and their historic equivalents), pointing each at the existing raw numeric property already computed alongside its formatted display string.

**6. Cards and Price History Grids** - Add `SortMemberPath` to the Cards grid's Outstanding/Status/Next Invoice Due Date/Active template columns and the Price History grid's Source template column, pointing each at its existing underlying bound value.

### Stage 3: Scope Exclusions

**7. Reserva Movements Grid** - Explicitly opt the Movements `DataGrid` out of the new sort behavior, matching its exclusion on Web — its rows are linked to inline split-group totals via `RowDetailsTemplate`, and reordering would separate a split's members from their total.

**8. Annual Summary Tabs** - Explicitly opt all three Annual Summary tab `DataGrid`s out of the new sort behavior. Unlike Web, their fixed/spacer/emphasized rows live in the same flat bound collection as the sortable data rows with no cheap way to keep them pinned, so sorting the whole collection would scatter them — document this as an intentional, WPF-specific parity gap from Web's behavior on 2 of its 3 Annual Summary tabs.

### Stage 4: Verification and Polish

**9. Cross-Grid Manual Pass** - Run the WPF app and click through every in-scope grid's headers to confirm the 3-state cycle, header arrow, null-last ordering (Portfolio Summary rows still loading their price), and pinned/excluded grids all match the PRD's Experience description.

**10. Full Verification** - Run the full `dotnet test` suite (including the new `SortCycle`/`NullLastComparer` tests) and a `dotnet build --configuration Release` to confirm no regressions across every touched view.
