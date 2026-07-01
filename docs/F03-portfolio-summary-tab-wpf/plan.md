# Implementation Plan: F03 — Portfolio Summary Tab — WPF

**Prerequisites:**
- F01 (`IPortfolioAssetSummaryQueryService` and `GetPortfolioAssetsSummary`) already implemented and registered in DI (`ApplicationServiceCollectionExtensions`)
- `IAssetPriceService` already available in `AssetDetailsViewModel`
- xUnit and FluentAssertions already in use in `Financial.Presentation.Tests`

---

### Phase 1: Row ViewModel

**1. PortfolioAssetSummaryRowViewModel** — Create `Financial.App/ViewModels/PortfolioAssetSummaryRowViewModel.cs` extending `ViewModelBase`. The class holds static values from `PortfolioAssetSummaryItemDTO` and tracks async price state via `IsLoadingPrice`, `PriceFetchFailed`, and nullable `CurrentValue`/`ProfitPercent`. Expose computed display strings and the `ProfitIsPositive`/`ProfitIsNegative` bool properties used for colour data-triggers in XAML. Include `ApplyPrice(decimal)` and `MarkPriceFailed()` methods that update state and raise `PropertyChanged`. See spec Section 4 for the complete property list and Section 7 for formatting rules.

---

### Phase 2: ViewModel and Interface

**2. IAssetDetailsViewModel Extension** — Add `IsPortfolioView` (bool, read-only) and `LoadPortfolioSummary(string brokerName, string portfolioName, AggregatedSummaryDTO summary, IReadOnlyList<CreditDTO> credits, IReadOnlyList<PortfolioAssetSummaryItemDTO> assetItems)` to the `IAssetDetailsViewModel` interface. See spec Section 4 for the complete signature.

**3. AssetDetailsViewModel — Portfolio Summary** — Add `IsPortfolioView` and `PortfolioAssetSummaryRows` (`ObservableCollection<PortfolioAssetSummaryRowViewModel>`) to `AssetDetailsViewModel`. Implement `LoadPortfolioSummary`: set `IsPortfolioView = true`, populate `PortfolioAssetSummaryRows` from the item list (same pattern as `Credits.Clear()` / `Add()`), and start background price fetch per row. Add `FetchRowPricesAsync` using a `CancellationTokenSource` that is cancelled and replaced on each new `LoadPortfolioSummary` call. Each row calls `IAssetPriceService.GetCurrentPrice` via `Task.Run`, then calls `ApplyPrice` or `MarkPriceFailed` on the row VM. Update `Clear()` to also cancel pending fetches, clear `PortfolioAssetSummaryRows`, and reset `IsPortfolioView`. Update `LoadAssetDetails` and `LoadAggregateCredits` to set `IsPortfolioView = false`. See spec Sections 3 and 4 for the cancellation and data-flow approach.

**4. MainNavigationViewModelBase — Portfolio Asset Summary** — Add `IPortfolioAssetSummaryQueryService` as a constructor parameter to `MainNavigationViewModelBase<T>`. In the private `LoadPortfolioCredits` method (which handles Portfolio node selection), call `GetPortfolioAssetsSummary(brokerName, portfolioName)` and then call `AssetDetails.LoadPortfolioSummary(...)` passing the summary, credits, and item list. See spec Section 4. Update `MainNavigationViewModel` to accept and forward the new parameter; DI resolution is automatic as the service is already registered.

---

### Phase 3: XAML

**5. NavigationView.xaml — Summary Tab Refactor** — Inside the Summary `TabItem`, move the existing content grid into a named `DataTemplate` resource (`AssetSummaryTemplate`). Create a new `PortfolioSummaryTemplate` DataTemplate containing: (a) a panel with three `TextBlock` labels for Total Bought (green), Total Sold (red), and Total Credits (blue) bound to `AssetDetails` properties; and (b) a read-only `DataGrid` bound to `AssetDetails.PortfolioAssetSummaryRows` with `IsReadOnly="True"`, `CanUserAddRows="False"`, and no `InputBindings`. Wrap the tab body in a `ContentControl` whose `ContentTemplate` defaults to `AssetSummaryTemplate` and switches to `PortfolioSummaryTemplate` via a `DataTrigger` on `AssetDetails.IsPortfolioView = True`. See spec Sections 3 and 4 for the DataTemplate switching pattern and DataGrid column specification.

**6. DataGrid Columns** — Define DataGrid columns in the `PortfolioSummaryTemplate`: `Asset Name` (`DataGridTextColumn`, binds `AssetName`), `First Investment` (`DataGridTextColumn`, binds `DisplayFirstInvestmentDate`), `Quantity` (`DataGridTextColumn`, binds `DisplayCurrentQuantity`, right-aligned), `Total Invested` (`DataGridTextColumn`, binds `DisplayTotalInvested`, right-aligned), `% Portfolio` (`DataGridTextColumn`, binds `DisplayPortfolioWeight`, right-aligned), `Current Value` (`DataGridTemplateColumn` with `DataTrigger` on `IsLoadingPrice` to show `"..."`, otherwise `DisplayCurrentValue`), and `% Profit` (`DataGridTemplateColumn` with `DataTrigger` on `IsLoadingPrice` for `"..."`, otherwise `DisplayProfitPercent` with `DataTrigger` on `ProfitIsPositive`/`ProfitIsNegative` to set `Foreground`). See spec Section 7 (acceptance criteria) for exact display formats.

---

### Phase 4: Tests

**7. PortfolioAssetSummaryRowViewModel Tests** — Create `Tests/Financial.Presentation.Tests/ViewModels/PortfolioAssetSummaryRowViewModelTests.cs`. Cover display formatting for all fields, `ApplyPrice`/`MarkPriceFailed` state transitions, `ProfitIsPositive`/`ProfitIsNegative` values, zero `TotalInvested` edge case, and `PropertyChanged` events raised on state-changing methods. See spec Section 7 for the complete test function list.

**8. AssetDetailsViewModel Portfolio Summary Tests** — Create `Tests/Financial.Presentation.Tests/ViewModels/AssetDetailsViewModelPortfolioSummaryTests.cs`. Use a stub `IAssetPriceService` that never completes to avoid async side effects. Cover `LoadPortfolioSummary` populating rows, `IsPortfolioView` toggling, `Clear()` reset, and regression checks that `LoadAssetDetails` resets `IsPortfolioView`. See spec Section 7 for the complete test function list.

**9. MainNavigationViewModelBaseTests Extension** — Add the four new test cases to `MainNavigationViewModelBaseTests.cs`: portfolio node calls `LoadPortfolioSummary` with correct items, correct broker/portfolio names forwarded to the service, and broker node does not trigger `LoadPortfolioSummary`. Extend `SpyAssetDetailsViewModel` to implement the new interface method, and add `StubPortfolioAssetSummaryQueryService`. See spec Section 7 for the complete test function list.
