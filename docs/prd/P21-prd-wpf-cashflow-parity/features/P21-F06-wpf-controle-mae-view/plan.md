# Implementation Plan: F06. WPF Controle Mãe View

**Prerequisites:**
- F01 merged (Cash Flow tab host with a reserved `controleMaeTab` placeholder; DI composition root already registers all CashFlow services including `IControleMaeService`)
- A local copy of `data-cashflow.json` for manual smoke testing (never the live file)

### Stage 1: Page Shell, Ledger Grid, and Totals Row

**1. Add ControleMaeViewModel with entries and totals loading** - Create `ViewModels/CashFlow/ControleMaeViewModel.cs` with `FromDate` triggering `RefreshEntriesAsync` (request-guard pattern per F02–F05), a separate `RefreshTotalsAsync` fetching the all-time `GetTotals()` independently of `FromDate`, and an `Entries` collection.

**2. Build ControleMaeView shell** - Create `Views/CashFlow/ControleMaeView.xaml`(.cs) with the "From" `DatePicker`, ledger `DataGrid` (Date/Description/Note/BRL/GBP with "—" for null), and the all-time totals row.

**3. Wire ControleMaeView into the shell** - Register `ControleMaeViewModel`/`ControleMaeView` in `App.xaml.cs`, add `x:Name="controleMaeTab"` in `MainWindow.xaml`, and set its content from `MainWindow.xaml.cs`.

### Stage 2: Create Entry Form

**4. Add Create Entry state and commands to ControleMaeViewModel** - Form fields (Date, Description, Note, Currency, Value), `CreateEntryFormValidation`, and the submit command calling `CreateEntryAsync`.

**5. Build CreateEntryFormView** - Inline form UserControl (same recipe as F02–F05's forms) with a Currency `ComboBox` and a plain `TextBox` for Value (accepts a leading minus sign), opened from the "New Entry" toolbar button.

### Stage 3: Edit and Delete Entry

**6. Add edit/delete state and commands to ControleMaeViewModel** - Edit form fields (BRL Value, GBP Value) pre-filled from the selected row, `EditEntryFormValidation` (blank maps to null), an update command calling `UpdateEntryValuesAsync`, and a delete command with the standard confirmation wording calling `DeleteEntryAsync`.

**7. Build EditEntryFormView and wire ledger grid row actions** - Inline form UserControl opened from a row's edit icon; wire the ledger grid's edit/delete icons to the new commands.

### Stage 4: Verification

**8. Add unit tests** - Add `ControleMaeViewModelTests.cs`, `CreateEntryFormValidationTests.cs`, `EditEntryFormValidationTests.cs`, and a `StubControleMaeService` in `TestStubs.cs`, covering entry loading, the totals/FromDate independence, Create/Edit/Delete.

**9. Full solution build and test pass** - Run `dotnet build` across the solution and `dotnet test` for `Financial.Presentation.Tests`, confirming zero regressions.

**10. Manual smoke test** - Launch `Financial.App` against a temporary copy of `data-cashflow.json` and exercise changing the From date, creating a BRL entry and a GBP entry, editing an entry's values, and deleting an entry.
