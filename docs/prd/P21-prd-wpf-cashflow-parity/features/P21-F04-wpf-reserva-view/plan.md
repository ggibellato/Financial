# Implementation Plan: F04. WPF Reserva View

**Prerequisites:**
- F01 merged (Cash Flow tab host with a reserved `reservaTab` placeholder; DI composition root already registers all CashFlow services including `IReserveService`)
- A local copy of `data-cashflow.json` for manual smoke testing (never the live file)

### Stage 1: Page Shell, Balances Grid, and Movements Grid

**1. Add ReservaViewModel with balances and movements loading** - Create `ViewModels/CashFlow/ReservaViewModel.cs` with the request-guarded `RefreshAsync` pattern (per F02/F03), fetching `GetBucketBalances`/`GetMovementHistory` and exposing `Balances`, `TotalBalance`, and a `Movements` collection.

**2. Add ReserveMovementRow with split-group computation** - Create `ViewModels/CashFlow/ReserveMovementRow.cs` and the grouping logic that sets `GroupTotal`/`IsPartOfGroup` per movement, matching the web's same-Date+Description grouping.

**3. Build ReservaView shell** - Create `Views/CashFlow/ReservaView.xaml`(.cs) with the toolbar, Balances `DataGrid` (4 buckets + Total row), and Movements `DataGrid` with `RowDetailsTemplate` showing the "Total split for {description}" line whenever `GroupTotal` is set.

**4. Wire ReservaView into the shell** - Register `ReservaViewModel`/`ReservaView` in `App.xaml.cs`, add `x:Name="reservaTab"` in `MainWindow.xaml`, and set its content from `MainWindow.xaml.cs`.

### Stage 2: Income Split Form

**5. Add income split state and commands to ReservaViewModel** - Form fields (Date, Amount, Description), `IncomeSplitFormValidation`, the submit command calling `PostIncomeSplitAsync`, and the post-save result panel state.

**6. Build IncomeSplitFormView** - Inline form UserControl (same recipe as F02's `ExpenseFormView`) with `DecimalInputHelper` on Amount, opened from the "New Income Split" toolbar button, showing the result panel after a successful post.

### Stage 3: Withdrawal Form with Overdraft Confirmation

**7. Add withdrawal state and commands to ReservaViewModel** - Form fields (Bucket, Amount, Date, Description), `WithdrawalFormValidation`, and the submit command calling `PostWithdrawalAsync`, catching `OverdraftConfirmationRequiredException` and invoking the injected confirm delegate to resubmit with the override flag.

**8. Build WithdrawalFormView** - Inline form UserControl with a Bucket ComboBox (defaulting to Investimento) and `DecimalInputHelper` on Amount, opened from the "New Withdrawal" toolbar button.

### Stage 4: Edit and Delete Movement

**9. Add edit/delete state and commands to ReservaViewModel** - Edit form fields pre-filled from the selected row, `EditReserveMovementFormValidation`, an update command calling `UpdateMovementAsync`, and a delete command that picks the split-aware or standard confirmation wording before calling `DeleteMovementAsync`.

**10. Build EditReserveMovementFormView and wire grid row actions** - Inline form UserControl opened from a movement row's edit icon; wire the Movements grid's edit/delete icons to the new commands, ensuring only one of the 3 forms is open at a time.

### Stage 5: Verification

**11. Add unit tests** - Add `ReservaViewModelTests.cs`, `IncomeSplitFormValidationTests.cs`, `WithdrawalFormValidationTests.cs`, `EditReserveMovementFormValidationTests.cs`, and a `StubReserveService` in `TestStubs.cs`, covering balances/grouping, income split, withdrawal (including overdraft confirm/decline), edit, and split-aware delete.

**12. Full solution build and test pass** - Run `dotnet build` across the solution and `dotnet test` for `Financial.Presentation.Tests`, confirming zero regressions.

**13. Manual smoke test** - Launch `Financial.App` against a temporary copy of `data-cashflow.json` and exercise posting an income split, posting an overdrawing withdrawal (decline then confirm), editing a movement, and deleting both a split-group and a standalone movement.
