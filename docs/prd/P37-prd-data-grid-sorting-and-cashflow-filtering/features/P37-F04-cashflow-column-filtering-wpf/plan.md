# Implementation Plan: F04. CashFlow Column Filtering — WPF

**Prerequisites:**
- `Financial.App` dev environment (already configured per repo `README.md`)
- No new NuGet packages — implemented with existing WPF APIs (`Popup`, `ItemsControl`, `ObservableCollection`)

### Stage 1: Shared Filter Infrastructure

**1. Column Filter ViewModel** - Build `ColumnFilterViewModel<T>` and its non-generic `ColumnFilterViewModelBase`: available values computed from the full unfiltered row set, a checked/unchecked state per value, "(All)" toggle semantics, and a `Matches(row)` predicate supporting both single- and multi-value columns (e.g. a transfer's two banks).

**2. Filterable Column Header Control** - Build `FilterableColumnHeader`: label, a filter icon toggle that visibly indicates an active filter, and a popup checklist (with a search box past 10 values) bound to a `ColumnFilterViewModelBase` instance. Register it as the implicit header template for that base type in `App.xaml`, so assigning a `DataGridColumn`'s `Header` to a filter ViewModel is the only wiring each view needs.

**3. Unit Tests** - Cover `ColumnFilterViewModel<T>`'s available-value computation, toggle/clear transitions, multi-value matching, and state preservation across a data refresh.

### Stage 2: Grid Integration

**4. Bank and Card Grids** - Wire a `ColumnFilterViewModel` into `BanksGridView` (Bank), `CardsGridView` (Card), and `IncomeSectionView` (Bank): each hosting ViewModel owns the filter instance, re-filters its bound collection on change, and the view's code-behind assigns the column's `Header`.

**5. Expense Grids** - Wire Category and Card filters into `ExpenseSectionView` and `CreditCardExpensesView`.

**6. Bank Operations Refactor** - Replace `BankSectionView`'s `ComboBox`-based single-select Bank filter with the header-based checklist: retire `SelectedBankFilter`/`BankFilterOptions`/`ApplyBankFilter`/`BuildBankFilterOptions` from `BankOperationsWorkflowViewModel`, and wire a `ColumnFilterViewModel<BankOperationRow>` with a two-bank accessor for transfer rows.

**7. Category Totals Grids** - Wire Category filters into `MonthlySummaryView`'s embedded Category Totals grid and both of `AnnualSummaryView`'s in-scope tabs (Category Totals, Historic Summary Average), treating spacer/emphasized rows as always-visible pass-throughs.

**8. Integration Tests** - Extend `BankOperationsWorkflowViewModelTests` for the new filter shape; add filter-specific tests to each other touched ViewModel's test file.

### Stage 3: Verification and Polish

**9. Cross-Grid Manual Pass** - Run the WPF app and exercise every in-scope grid's filter popup — single-column, multi-column (Expense Section's Category + Card together), the >10-value search box, the empty-result message, and coexistence with F02's sort on the same header cell.

**10. Accessibility Check** - Confirm the filter toggle button and its popup checklist are keyboard-operable (Tab to reach, Enter/Space to open, arrow keys to move through checkboxes, Escape to dismiss) per the project's WCAG 2.2 AA baseline.

**11. Full Verification** - Run the complete `dotnet test` suite and `dotnet build --configuration Release` to confirm no regressions across every touched view and ViewModel.
