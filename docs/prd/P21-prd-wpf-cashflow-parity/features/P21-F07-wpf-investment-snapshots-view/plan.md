# Implementation Plan: F07. WPF Investment Snapshots View

**Prerequisites:**
- F01 merged (Cash Flow tab host with a reserved `investmentSnapshotsTab` placeholder; DI composition root already registers all CashFlow services including `IInvestmentSnapshotService`)
- A local copy of `data-cashflow.json` for manual smoke testing (never the live file)

### Stage 1: Page Shell and Snapshot Grid

**1. Add InvestmentSnapshotsViewModel with month-filtered loading** - Create `ViewModels/CashFlow/InvestmentSnapshotsViewModel.cs` with `Year`/`Month` properties (setters trigger `RefreshAsync`, request-guard pattern per F02–F06), fetching `GetSnapshotsForMonthAsync` and exposing a `Snapshots` collection plus a computed `NetTotal`.

**2. Add SnapshotRow with liability label** - Create `ViewModels/CashFlow/SnapshotRow.cs` wrapping each snapshot with a computed `DisplayLabel` that appends " (liability)" for liability accounts.

**3. Build InvestmentSnapshotsView shell** - Create `Views/CashFlow/InvestmentSnapshotsView.xaml`(.cs) with the functional `MonthYearPicker`, the snapshot `DataGrid` (edit icon, account label, value), and the "Total (net of liabilities)" row.

**4. Wire InvestmentSnapshotsView into the shell** - Register `InvestmentSnapshotsViewModel`/`InvestmentSnapshotsView` in `App.xaml.cs`, add `x:Name="investmentSnapshotsTab"` in `MainWindow.xaml`, and set its content from `MainWindow.xaml.cs`.

### Stage 2: Edit Snapshot Value

**5. Add Edit Value state and commands to InvestmentSnapshotsViewModel** - Form field (Value) pre-filled from the selected row, `EditSnapshotValueFormValidation`, and an update command calling `UpdateSnapshotValueAsync`.

**6. Build EditSnapshotValueFormView and wire grid row action** - Inline form UserControl (same recipe as F02–F06's forms) with the shared `DecimalInputHelper` masking, opened from a snapshot row's edit icon.

### Stage 3: Verification

**7. Add unit tests** - Add `InvestmentSnapshotsViewModelTests.cs`, `EditSnapshotValueFormValidationTests.cs`, and a `StubInvestmentSnapshotService` in `TestStubs.cs`, covering month-filtered loading, the liability label, net total computation, and Edit Value (valid/invalid/failed).

**8. Full solution build and test pass** - Run `dotnet build` across the solution and `dotnet test` for `Financial.Presentation.Tests`, confirming zero regressions.

**9. Manual smoke test** - Launch `Financial.App` against a temporary copy of `data-cashflow.json` and exercise changing the Month/Year picker, confirming the liability label, and editing a snapshot's value.
