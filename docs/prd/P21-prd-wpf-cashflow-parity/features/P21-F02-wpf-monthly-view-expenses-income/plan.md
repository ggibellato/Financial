# Implementation Plan: F02. WPF Monthly View — Expenses & Income

**Prerequisites:**
- F01 merged (Cash Flow tab with empty Monthly nested tab, CashFlow DI wiring, `Views/CashFlow/`/`ViewModels/CashFlow/` folder convention reserved)
- A local copy of `data-cashflow.json` for manual smoke testing (never the live file)

### Stage 1: Reusable Month+Year Picker

**1. Build MonthYearPicker** - Create `Components/MonthYearPicker.xaml`(.xaml.cs) with month-name and year `ComboBox`es exposed as two-way bindable dependency properties, so `MonthlyView` (and later F05/F07) can bind it directly to a ViewModel's Year/Month.

### Stage 2: Monthly Page Shell and Data Loading

**2. Create MonthlyViewModel's core state and fetch orchestration** - Add `ViewModels/CashFlow/MonthlyViewModel.cs` with Year/Month properties, loading/error state, and a fetch routine that loads expenses, incomes, category totals, tithe summary, and banks for the selected period, refetching whenever the period changes.

**3. Build the Monthly view shell and wire it into MainWindow** - Add `Views/CashFlow/MonthlyView.xaml`(.xaml.cs) hosting the `MonthYearPicker` and a nested Summary/Expense/Incoming `TabControl` (sub-tab content added in later stages), register it in DI, and assign it to the Monthly `TabItem` in `MainWindow.xaml.cs`.

### Stage 3: Summary Sub-Tab

**4. Build MonthlySummaryView** - Add `Views/CashFlow/MonthlySummaryView.xaml`(.xaml.cs) showing the Category Totals grid and tithe summary text, bound to `MonthlyViewModel`.

### Stage 4: Expense CRUD

**5. Add expense state and commands to MonthlyViewModel** - Extend `MonthlyViewModel` with expense create/edit form fields, payment-mode toggling, round-up field visibility (bank-dependent), settled-expense read-only handling, and Add/Update/Delete commands calling `IExpenseService`.

**6. Add ExpenseFormValidation** - Add `ViewModels/CashFlow/ExpenseFormValidation.cs` with the required-field, payment-mode, and round-up-range validation rules.

**7. Build ExpenseSectionView and ExpenseFormView** - Add the Expense grid (with edit/delete actions) and the inline create/edit form UserControl, wired to `MonthlyViewModel`'s expense state and commands, using `DecimalInputHelper` for the Value/Round-Up fields.

### Stage 5: Income CRUD

**8. Add income state and commands to MonthlyViewModel** - Extend `MonthlyViewModel` with income create/edit form fields, gross-value field visibility (source-dependent), and Add/Update/Delete commands calling `IIncomeService`.

**9. Add IncomeFormValidation** - Add `ViewModels/CashFlow/IncomeFormValidation.cs` with the required-field validation rules.

**10. Build IncomeSectionView and IncomeFormView** - Add the Income grid and the inline create/edit form UserControl, wired to `MonthlyViewModel`'s income state and commands.

### Stage 6: Verification

**11. Add unit tests** - Add `MonthlyViewModelTests.cs`, `ExpenseFormValidationTests.cs`, and `IncomeFormValidationTests.cs` covering fetch orchestration, CRUD flows, field-visibility toggling, and all validation branches.

**12. Full solution build and test pass** - Run `dotnet build` across the solution and `dotnet test` for `Financial.Presentation.Tests`, confirming zero regressions.

**13. Manual smoke test** - Launch `Financial.App` against a temporary copy of `data-cashflow.json` and exercise sub-tab switching, month/year changes, and expense/income create/edit/delete in both payment modes.
