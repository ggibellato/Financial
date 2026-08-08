# Implementation Plan: WPF Dynamic Reserve Bucket UI

**Prerequisites:**
- F03 (income-split computation, dynamic `IncomeSplitResultDTO` shape) merged
- F04 (bucket balances for all buckets) merged
- F05 (`IReserveBucketService`/`ReserveBucketDTO`) merged
- F06 (web equivalent) merged — establishes the default-bucket-selection and degradation decisions this feature mirrors

### Stage 1: ViewModel

**1. Dynamic bucket state in `ReservaViewModel`** - Remove the static `Buckets` string array, inject `IReserveBucketService`, add an `ObservableCollection<ReserveBucketDTO> Buckets` populated inside `RefreshAsync` (buckets-only failures degrade to an empty list without setting the page error), add a `DefaultBucketName()` helper used by `ShowWithdrawalForm`/initial load, and add a `SplitPercentageWarning` computed property.

### Stage 2: Views

**2. Dynamic bucket pickers** - Update `WithdrawalFormView.xaml` and `EditReserveMovementFormView.xaml`'s `ComboBox` to bind `ItemsSource` to `Buckets` (`DisplayMemberPath="Name"`, `SelectedValuePath="Name"`) instead of the removed static array.

**3. Dynamic split-result display** - Replace `IncomeSplitFormView.xaml`'s 4 hardcoded `TextBlock`s with an `ItemsControl` over `LastSplitResult.Buckets`.

**4. Warning banner** - Add a red `TextBlock` bound to `SplitPercentageWarning` in `ReservaView.xaml`, near the toolbar.

### Stage 3: Wiring

**5. DI registration** - Update `App.xaml.cs`'s `ReservaViewModel` registration to resolve and pass `IReserveBucketService`.

### Stage 4: Tests

**6. Stub and ViewModel tests** - Add `StubReserveBucketService` to `TestStubs.cs`; update `ReservaViewModelTests.CreateViewModel` to accept it; add tests for bucket loading, default-bucket selection, the split-percentage warning (balanced/unbalanced/empty), and the fetch-failure degradation path.
