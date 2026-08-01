# Implementation Plan: F03. WPF Monthly View — Banks, Cards, Transfers & Balance Adjustments

**Prerequisites:**
- F02 merged (Monthly view with Summary/Expense/Incoming sub-tabs; `MonthlySummaryView.xaml` has a reserved extension point for this feature; `MonthlyViewModel` already fetches expenses/incomes/banks/category totals/tithe)
- A local copy of `data-cashflow.json` for manual smoke testing (never the live file)

### Stage 1: Banks Grid — Balances and Round-Up Totals

**1. Add BankTotalRow and bank totals computation** - Add `ViewModels/CashFlow/BankTotalRow.cs`, extend `MonthlyViewModel.RefreshAsync` to also fetch `IBankService.GetBankBalancesByMonth` and compute each bank's round-up total from the already-loaded `Expenses`, exposing a `BankTotals` collection.

**2. Build BanksGridView (balances only)** - Add `Views/CashFlow/BanksGridView.xaml`(.cs) showing Bank/Balance/Round-Up columns and footer totals, appended into `MonthlySummaryView.xaml`'s reserved extension point.

### Stage 2: Bank History

**3. Add BankHistoryEntry and history merge** - Add `ViewModels/CashFlow/BankHistoryEntry.cs`; extend `RefreshAsync` to fetch `ITransferService.GetTransfersByMonth` once and `IBalanceAdjustmentService.GetAdjustmentsByBank` per bank, merging both into each `BankTotalRow.History`, sorted newest first.

**4. Add expand/collapse and delete-from-history to BanksGridView** - Wire `DataGrid.RowDetailsTemplate` to `BankTotalRow.IsExpanded`, render the history entries, and add delete actions (with confirmation) for transfers/adjustments that refresh the owning bank's totals and history afterward.

### Stage 3: Transfer Form

**5. Add transfer state and commands to MonthlyViewModel** - Create/edit form fields (Date, source/destination bank, amount, note), `TransferFormValidation`, and Add/Update commands calling `ITransferService`.

**6. Build TransferFormView** - Inline form UserControl (same recipe as F02's `ExpenseFormView`) wired to the new state/commands, opened from a bank row's "Move Money" button.

### Stage 4: Balance Adjustment Form

**7. Add adjustment state and commands to MonthlyViewModel** - Create/edit form fields (Date, target balance, note), current-balance reference, `BalanceAdjustmentFormValidation`, and Add/Update commands calling `IBalanceAdjustmentService`, surfacing the resulting delta.

**8. Build BalanceAdjustmentFormView** - Inline form UserControl with the current-balance reference text and post-save delta confirmation, opened from a bank row's "Correct Balance" button.

### Stage 5: Cards Grid

**9. Add card statement state and commands to MonthlyViewModel** - Fetch `ICardStatementService.GetStatementsForMonthAsync` in `RefreshAsync`, add a `MarkPaidSources` dictionary and Mark/Unmark Paid commands.

**10. Build CardsGridView** - `DataGrid` with Outstanding/Status columns, a bank picker + Mark Paid button (disabled until a bank is chosen) per unpaid row, Unmark Paid for paid rows, footer adjustment total, appended into `MonthlySummaryView.xaml`.

### Stage 6: Verification

**11. Add unit tests** - Add `MonthlyViewModelBanksCardsTests.cs`, `TransferFormValidationTests.cs`, `BalanceAdjustmentFormValidationTests.cs` covering bank totals/history computation, transfer/adjustment CRUD, and card mark/unmark-paid.

**12. Full solution build and test pass** - Run `dotnet build` across the solution and `dotnet test` for `Financial.Presentation.Tests`, confirming zero regressions.

**13. Manual smoke test** - Launch `Financial.App` against a temporary copy of `data-cashflow.json` and exercise expanding a bank's history, moving money, correcting a balance, and marking/unmarking a card statement paid.
