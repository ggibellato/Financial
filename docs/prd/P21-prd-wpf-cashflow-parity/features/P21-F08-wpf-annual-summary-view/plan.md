# Implementation Plan: F08. WPF Annual Summary View

**Prerequisites:**
- F01 merged (Cash Flow tab host with a reserved `annualSummaryTab` placeholder; DI composition root already registers all CashFlow services including `IAnnualSummaryService`)
- A local copy of `data-cashflow.json` for manual smoke testing (never the live file)

### Stage 1: Category Totals Sub-Tab

**1. Add AnnualSummaryViewModel with Category Totals row building** - Create `ViewModels/CashFlow/AnnualSummaryViewModel.cs` with a `Year` property (setter triggers `RefreshAsync`, request-guard pattern per F02–F07), fetching `GetCategoryTotalsAnnualForYear` and building the flat `CategoryTotalRows` sequence (income rows, category rows, spacers, emphasized Resultado/Total despesas).

**2. Add AnnualSummaryRow** - Create `ViewModels/CashFlow/AnnualSummaryRow.cs`.

**3. Build AnnualSummaryView shell with Category Totals tab** - Create `Views/CashFlow/AnnualSummaryView.xaml`(.cs) with the Year input, a nested `TabControl`, and the Category Totals `DataGrid` (12 month columns + Average + Annual Total, spacer/emphasis row styling).

**4. Wire AnnualSummaryView into the shell** - Register `AnnualSummaryViewModel`/`AnnualSummaryView` in `App.xaml.cs`, add `x:Name="annualSummaryTab"` in `MainWindow.xaml`, and set its content from `MainWindow.xaml.cs`.

### Stage 2: Investments Sub-Tab

**5. Add Investments row building to AnnualSummaryViewModel** - Fetch `GetInvestmentAnnualResultForYear` inside `RefreshAsync`; build `InvestmentRows` (per-account rows with liability suffix, Total row, Month Result row) and expose the 3 `NetPosition` summary figures.

**6. Add InvestmentAnnualRow** - Create `ViewModels/CashFlow/InvestmentAnnualRow.cs`.

**7. Add the Investments tab to AnnualSummaryView** - `DataGrid` with 12 month columns only (no Average/Annual Total), nullable-cell rendering, plus the 3 summary figures below the table.

### Stage 3: Historic Summary Average Sub-Tab

**8. Add Historic Summary Average row building to AnnualSummaryViewModel** - Fetch `GetHistoricSummaryAverageFromYear` inside `RefreshAsync`; expose `AvailableYears` and build `HistoricSummaryRows` (spacers after specific category names, emphasis on Resultado/Total despesas).

**9. Add HistoricSummaryRow** - Create `ViewModels/CashFlow/HistoricSummaryRow.cs`.

**10. Add the Historic Summary Average tab with dynamic columns** - `DataGrid` with `AutoGenerateColumns=false`; code-behind builds one `DataGridTextColumn` per year in `AvailableYears`, rebuilt whenever the year list changes.

### Stage 4: Verification

**11. Add unit tests** - Add `AnnualSummaryViewModelTests.cs` and a `StubAnnualSummaryService` in `TestStubs.cs`, covering row-building and spacer/emphasis logic for all 3 sub-tabs, the liability suffix, nullable investment cells, and Year-change refetch.

**12. Full solution build and test pass** - Run `dotnet build` across the solution and `dotnet test` for `Financial.Presentation.Tests`, confirming zero regressions.

**13. Manual smoke test** - Launch `Financial.App` against a temporary copy of `data-cashflow.json` and exercise changing the Year, switching between all 3 sub-tabs, and confirming spacer/emphasized rows and the dynamic per-year columns render correctly.
