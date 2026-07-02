# Implementation Plan: F03 — Portfolio Summary Tab — WPF

**Prerequisites:**
- F01 (`IPortfolioAssetSummaryQueryService` and `GetPortfolioAssetsSummary`) already implemented and registered in DI (`ApplicationServiceCollectionExtensions`); DTO `PortfolioAssetSummaryItemDTO` includes `TotalCredits` and `CashFlows`
- `IAssetPriceService` already available in `AssetDetailsViewModel`
- xUnit and FluentAssertions already in use in `Financial.Presentation.Tests`

---

### Phase 1: Row ViewModel

**1. PortfolioAssetSummaryRowViewModel** — Create `Financial.App/ViewModels/PortfolioAssetSummaryRowViewModel.cs` extending `ViewModelBase`. The class holds static values from `PortfolioAssetSummaryItemDTO` — including `TotalCredits` and `CashFlows` — and tracks async price state via `IsLoadingPrice`, `PriceFetchFailed`, and nullable `CurrentValue`, `ProfitPercent`, `ProfitWithCreditsPercent`, and `Xirr`. Expose computed display strings for all fields and colour-flag bool properties (`ProfitIsPositive`, `ProfitIsNegative`, `ProfitWithCreditsIsPositive`, `ProfitWithCreditsIsNegative`, `XirrIsPositive`, `XirrIsNegative`) used in XAML DataTriggers. Include `ApplyPrice(decimal)` — which computes CurrentValue, ProfitPercent, ProfitWithCreditsPercent, and Xirr (via Newton-Raphson on `CashFlows` + terminal entry) — and `MarkPriceFailed()`, both raising `PropertyChanged` for all affected properties. See spec Section 4 for the complete property list, Newton-Raphson algorithm, and formatting rules.

---

### Phase 2: ViewModel and Interface

**2. IAssetDetailsViewModel Extension** — Add `IsPortfolioView` (bool, read-only) and `LoadPortfolioSummary(string brokerName, string portfolioName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits, IReadOnlyList<PortfolioAssetSummaryItemDTO> assetItems)` to the `IAssetDetailsViewModel` interface. See spec Section 4 for the complete signature.

**3. AssetDetailsViewModel — Portfolio Summary** — Add `IsPortfolioView` and `PortfolioAssetSummaryRows` (`ObservableCollection<PortfolioAssetSummaryRowViewModel>`) to `AssetDetailsViewModel`. Implement `LoadPortfolioSummary`: set `IsPortfolioView = true`, populate `PortfolioAssetSummaryRows` from the item list (same pattern as `Credits.Clear()` / `Add()`), and start background price fetch per row. Add `FetchRowPricesAsync` using a `CancellationTokenSource` that is cancelled and replaced on each new `LoadPortfolioSummary` call. Each row calls `IAssetPriceService.GetCurrentPrice` via `Task.Run`, then calls `ApplyPrice` or `MarkPriceFailed` on the row VM. Update `Clear()` to also cancel pending fetches, clear `PortfolioAssetSummaryRows`, and reset `IsPortfolioView`. Update `LoadAssetDetails` and `LoadAggregateCredits` to set `IsPortfolioView = false`. See spec Sections 3 and 4 for the cancellation and data-flow approach.

**4. MainNavigationViewModelBase — Portfolio Asset Summary** — Add `IPortfolioAssetSummaryQueryService` as a constructor parameter to `MainNavigationViewModelBase<T>`. In the private `LoadPortfolioCredits` method (which handles Portfolio node selection), call `GetPortfolioAssetsSummary(brokerName, portfolioName)` and then call `AssetDetails.LoadPortfolioSummary(...)` passing the summary, credits, and item list. See spec Section 4. Update `MainNavigationViewModel` to accept and forward the new parameter; DI resolution is automatic as the service is already registered.

---

### Phase 3: XAML

**5. NavigationView.xaml — Summary Tab Refactor** — Inside the Summary `TabItem`, move the existing content grid into a named `DataTemplate` resource (`AssetSummaryTemplate`). Create a new `PortfolioSummaryTemplate` DataTemplate containing: (a) a panel with three `TextBlock` labels for Total Bought (green), Total Sold (red), and Total Credits (blue) bound to `AssetDetails` properties; and (b) a read-only `DataGrid` bound to `AssetDetails.PortfolioAssetSummaryRows` with `IsReadOnly="True"`, `CanUserAddRows="False"`, and no `InputBindings`. Wrap the tab body in a `ContentControl` whose `ContentTemplate` defaults to `AssetSummaryTemplate` and switches to `PortfolioSummaryTemplate` via a `DataTrigger` on `AssetDetails.IsPortfolioView = True`. See spec Sections 3 and 4 for the DataTemplate switching pattern.

**6. DataGrid Columns** — Define all ten DataGrid columns in the `PortfolioSummaryTemplate` in the fixed order specified by the PRD: `Asset Name` (binds `AssetName`), `First Investment` (binds `DisplayFirstInvestmentDate`), `Quantity` (binds `DisplayCurrentQuantity`, right-aligned), `Total Invested` (binds `DisplayTotalInvested`, right-aligned), `% Portfolio` (binds `DisplayPortfolioWeight`, right-aligned), `Total Credits` (binds `DisplayTotalCredits`, right-aligned), `Current Value` (`DataGridTemplateColumn` with `DataTrigger` on `IsLoadingPrice` to show `"..."`; otherwise `DisplayCurrentValue`), `% Profit` (`DataGridTemplateColumn` with `DataTrigger` on `IsLoadingPrice` for `"..."`; otherwise `DisplayProfitPercent` with `DataTrigger`s on `ProfitIsPositive`/`ProfitIsNegative` to set `Foreground`), `% Profit w/ Credits` (`DataGridTemplateColumn` with the same loading/colour pattern using `DisplayProfitWithCreditsPercent`, `ProfitWithCreditsIsPositive`, `ProfitWithCreditsIsNegative`), and `XIRR` (`DataGridTemplateColumn` with loading/colour pattern using `DisplayXirr`, `XirrIsPositive`, `XirrIsNegative`). See spec Section 4 for exact display formats and colour semantics.

---

### Phase 4: Tests

**7. PortfolioAssetSummaryRowViewModel Tests** — Create `Tests/Financial.Presentation.Tests/ViewModels/PortfolioAssetSummaryRowViewModelTests.cs`. Cover display formatting for all fields including `DisplayTotalCredits`, `DisplayProfitWithCreditsPercent`, and `DisplayXirr`; `ApplyPrice`/`MarkPriceFailed` state transitions; colour flag values for profit, profit-with-credits, and XIRR; zero `TotalInvested` edge case; XIRR convergence and non-convergence (empty CashFlows); and `PropertyChanged` events raised on state-changing methods. See spec Section 7 for the complete test function list.

**8. AssetDetailsViewModel Portfolio Summary Tests** — Create `Tests/Financial.Presentation.Tests/ViewModels/AssetDetailsViewModelPortfolioSummaryTests.cs`. Use a stub `IAssetPriceService` that never completes to avoid async side effects. Cover `LoadPortfolioSummary` populating rows, `IsPortfolioView` toggling, `Clear()` reset, and regression checks that `LoadAssetDetails` resets `IsPortfolioView`. See spec Section 7 for the complete test function list.

**9. MainNavigationViewModelBaseTests Extension** — Add the four new test cases to `MainNavigationViewModelBaseTests.cs`: portfolio node calls `LoadPortfolioSummary` with correct items, correct broker/portfolio names forwarded to the service, and broker node does not trigger `LoadPortfolioSummary`. Extend `SpyAssetDetailsViewModel` to implement the new interface method, and add `StubPortfolioAssetSummaryQueryService`. See spec Section 7 for the complete test function list.
