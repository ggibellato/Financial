# Implementation Plan: P03-F03 — Credits Analysis Columns and Footer — WPF

**Prerequisites:**
- P02-F03 implemented: `PortfolioAssetSummaryRowViewModel`, `AssetDetailsViewModel.LoadPortfolioSummary`, and `PortfolioSummaryTemplate` DataGrid with 10 columns (Asset Name through XIRR) are all in place
- P03-F01 implemented: `PortfolioAssetSummaryItemDTO` confirmed to include `LastMonthCredits`, `LastCreditMonth`, `LastMonthCreditsPercent`, `EstimatedAnnualCredits`, `EstimatedAnnualPercent`, `CreditFrequencyPerYear`, and `CurrentMonthCredits`
- xUnit and FluentAssertions available in `Tests/Financial.Presentation.Tests`

---

### Phase 1: Row ViewModel

**1. PortfolioAssetSummaryRowViewModel — Credit Display Properties** — Extend `Financial.App/ViewModels/PortfolioAssetSummaryRowViewModel.cs` to read the six F01 credit-analysis fields from `PortfolioAssetSummaryItemDTO` in the constructor and expose five computed display strings. All new display strings are static (computed at construction, not price-dependent) and follow the existing "—" null-guard pattern. See spec Section 4 for the complete property list, null conditions, locale, and format strings.

---

### Phase 2: ViewModel Footer

**2. AssetDetailsViewModel — Footer Properties and Row Subscriptions** — Add six footer properties and row subscription logic to `Financial.App/ViewModels/AssetDetailsViewModel.cs`. Add `FooterTotalInvested`, `FooterTotalCredits`, `FooterCurrentMonthCredits`, `FooterCurrentMonthLabel`, and `FooterEstimatedAnnualCreditsDisplay` as `SetProperty`-backed fields populated in `LoadPortfolioSummary` and reset in `Clear()`. Add computed `FooterCurrentValueDisplay` that reads from `PortfolioAssetSummaryRows` directly and is triggered by per-row `PropertyChanged` subscriptions. Introduce `_rowSubscriptions` list and `SubscribeToRowPriceChanges` / `UnsubscribeFromRowPriceChanges` helpers; call unsubscription from the existing `CancelAndResetRowPriceFetch` method. See spec Section 4 for the complete member list, computed logic, and subscription lifecycle.

---

### Phase 3: XAML

**3. NavigationView.xaml — New DataGrid Columns** — Inside the `PortfolioSummaryTemplate` DataGrid in `Financial.App/Components/NavigationView.xaml`, add five `DataGridTextColumn` definitions after the existing XIRR column: Last Month Credits, Last Credit Month, Last Month %, Est. Annual Credits, Est. Annual %. All new columns bind to the corresponding `Display*` properties on the row VM and apply the existing `ElementStyle` with `TextAlignment="Right"` and `FontFamily="Consolas"`. Apply `CellStyle` and `HeaderStyle` overrides on the "Last Month Credits" column to add a 3 px `#007ACC` left border as the visual group separator. See spec Section 4 for column headers, bindings, and border specification.

**4. NavigationView.xaml — Footer Panel** — In the `PortfolioSummaryTemplate`'s parent `Grid`, add a third `RowDefinition Height="Auto"` and insert a `Border` with `SystemColors.ControlBrush` background at `Grid.Row="2"`. Inside, place a `WrapPanel` with five label+value pairs bound to the new `AssetDetails.Footer*` properties: Total Invested, Total Credits, Current Value (bound to `FooterCurrentValueDisplay`), Credits [Mon yyyy] (label from `FooterCurrentMonthLabel`, value from `FooterCurrentMonthCredits`), and Est. Annual Credits (bound to `FooterEstimatedAnnualCreditsDisplay`). The footer is outside the DataGrid scroll area and is not a DataGrid row. See spec Section 4 for the panel structure and property bindings.

---

### Phase 4: Tests

**5. PortfolioAssetSummaryRowViewModelTests — Credit Display Properties** — Extend the existing `BuildRow` factory in `Tests/Financial.Presentation.Tests/ViewModels/PortfolioAssetSummaryRowViewModelTests.cs` to accept the six new F01 credit-analysis fields with defaults matching the "no credits" state. Add 10 test cases covering all five new `Display*` properties and their null/"—" guard conditions. See spec Section 7 for the complete test function list.

**6. AssetDetailsViewModelPortfolioSummaryTests — Footer Properties** — Extend the existing test class in `Tests/Financial.Presentation.Tests/ViewModels/AssetDetailsViewModelPortfolioSummaryTests.cs` with 9 new test cases. Extend the local DTO builder to include the six F01 credit fields. Cover footer property population from row data, `FooterCurrentValueDisplay` two-state behavior ("Calculating…" and resolved N2 sum), and `Clear()` resetting all footer backing fields. For the resolved-price test, call `ApplyPrice` directly on row VMs after `LoadPortfolioSummary` returns, bypassing the async fetch. See spec Section 7 for the complete test function list.
