# Implementation Plan: F05. WPF Mensais View

**Prerequisites:**
- F01 merged (Cash Flow tab host with a reserved `mensaisTab` placeholder; DI composition root already registers all CashFlow services including `IMensaisService`)
- A local copy of `data-cashflow.json` for manual smoke testing (never the live file)

### Stage 1: Page Shell and Bill Tables

**1. Add MensaisViewModel with bill loading and Brasil/UK split** - Create `ViewModels/CashFlow/MensaisViewModel.cs` with the request-guarded `RefreshAsync` pattern (per F02–F04), fetching `GetBills()` and exposing `BrasilBills`/`UkBills` collections plus display-only `DisplayYear`/`DisplayMonth`.

**2. Build BillTableView** - Create `Views/CashFlow/BillTableView.xaml`(.cs) as a reusable `DataGrid` with a `ShowBrasilFields` dependency property toggling the NIT/Min. Wage columns.

**3. Build MensaisView shell** - Create `Views/CashFlow/MensaisView.xaml`(.cs) hosting the display-only `MonthYearPicker`, the toolbar, and two `BillTableView` instances (Brasil, UK).

**4. Wire MensaisView into the shell** - Register `MensaisViewModel`/`MensaisView` in `App.xaml.cs`, add `x:Name="mensaisTab"` in `MainWindow.xaml`, and set its content from `MainWindow.xaml.cs`.

### Stage 2: Add Bill Form

**5. Add Add-Bill state and commands to MensaisViewModel** - Form fields (Description, Due Day, Value, Area, Note), `AddBillFormValidation`, and the submit command calling `CreateBillAsync`.

**6. Build AddBillFormView** - Inline form UserControl (same recipe as F02–F04's forms) with a plain `TextBox` for Due Day and `DecimalInputHelper` on Value, opened from the "Add Bill" toolbar button.

### Stage 3: Edit and Delete Bill

**7. Add edit/delete state and commands to MensaisViewModel** - Edit form fields (Value, Status) pre-filled from the selected row, `EditBillFormValidation`, an update command calling `UpdateBillAsync`, and a delete command with the standard confirmation wording calling `DeleteBillAsync`.

**8. Build EditBillFormView and wire BillTableView row actions** - Inline form UserControl opened from a bill row's edit icon; wire `BillTableView`'s edit/delete icons to the ViewModel's commands.

### Stage 4: Reset All to Unset

**9. Add Reset All to Unset command to MensaisViewModel** - A confirmed bulk action calling `ResetAllToUnsetAsync` and refreshing both bill tables from its response.

**10. Wire the Reset All to Unset toolbar button** - Add the button to `MensaisView.xaml`'s toolbar bound to the new command.

### Stage 5: Verification

**11. Add unit tests** - Add `MensaisViewModelTests.cs`, `AddBillFormValidationTests.cs`, `EditBillFormValidationTests.cs`, and a `StubMensaisService` in `TestStubs.cs`, covering the Brasil/UK split, Add/Edit/Delete, and Reset All to Unset.

**12. Full solution build and test pass** - Run `dotnet build` across the solution and `dotnet test` for `Financial.Presentation.Tests`, confirming zero regressions.

**13. Manual smoke test** - Launch `Financial.App` against a temporary copy of `data-cashflow.json` and exercise adding a Brasil bill and a UK bill, editing a bill, deleting a bill, and running Reset All to Unset.
