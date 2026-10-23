# Technical Specification: Bank Account Screen

Complexity: **complex**. It touches Domain, Application, the API, the parity scripts and several new screens in the web app. There is no data-file change.

## 1. Technical Overview

**What.**
- Three read endpoints:
  - `GET accounts`: every account, ordered by name.
  - `GET accounts/{id}/{year}/{month}`: the account, its month-end balance and the actions it allows.
  - `GET accounts/{id}/transactions/{year}/{month}`: the grid rows, each carrying its source record.
- A Domain rule, `AccountActions`, maps capabilities to allowed actions.
- One shared reader returns every record that affects a balance.
- A new **Accounts** page (`/cashflow/accounts`) lists the default group for the current month.
- A new **account screen** (`/cashflow/accounts/:accountId?month=YYYY-MM`) has:
  - the picker, the header and the month selector;
  - the Transactions grid;
  - the allowed actions, each opening one inline form;
  - row Edit and Delete.
- New building blocks: one app-root Fluent `Toaster` with a success-toast hook, and a discard-changes prompt. Paying a recurring bill reuses one shared field set, which Mensais's dialog also uses.

**Why.**
- Bank tasks are spread over Monthly's tabs, and no screen shows everything that touched one bank.
- The account screen is the shell F08 (card), F10 (pots) and F12 (settlement) build on. The rule for what an account can do stays on the server, so F07–F12 add templates with no front-end rule changes.

**Delivery.** Five PRs. Each leaves `main` deployable and ends with a parity run showing 0 differences. The PRD review agreed four stages; interview Q8 split the actions stage in two, by file count.

| PR | Content | Non-test code files |
|---|---|---|
| **1. Account endpoints** | Action rule; shared record reader; three DTOs; query service, interface and DI; `AccountsController`; four `ToDto` mappers made `internal static`; summary service switched to the shared reader; parity account figures | ~15 (4 are one-word edits) + 3 parity scripts |
| **2. Read-only screens** | Nav entry and routes; API client and type aliases; Accounts page; account screen with picker, header, month selector and grid; loading, empty, error and not-found states; screen-checklist rows | ~13 |
| **3. Add expense and Add income** | Locked-bank and keep-open settings on the expense and income forms; app-root `Toaster` and toast hook; discard prompt; action bar; inline form area | ~11 |
| **4. Reconcile, transfers, Edit and Delete** | Locked-bank setting on the adjustment and transfer forms; F03 transfer calls; row Edit and Delete with `ConfirmDeleteDialog` | ~10 |
| **5. Pay recurring bill** | Shared bill-expense fields (Mensais dialog and bank screen); bill picker form; wiring; PRD ticks | ~6 |

**Scope.**

*Included:*
- PRD F05 Capabilities, Experience and Error Handling, and every Section 9 F05 criterion.
- **Consumes from F02:** `Account` (`Has`, `IsActive`, `Currency`, `TemplateName`, `RoundsUp`), the capabilities and `AccountBalanceRule`.
- **Consumes from F03:** `accounts/transfers` create, update and delete, and the `transfer-destinations` / `transfer-sources` lists.
- **Consumes from F04:** the default group summary, for the Accounts page.
- **Provides to F08, F10 and F12:** the account screen shell: Accounts page, picker, header, month selector, transactions grid, allowed-actions rendering and inline action area.
- **Provides to F06:** `GET accounts`, which F06's Admin Accounts grid can reuse (interview Q1).

*Not included:*
- The tab bar (F08) and "Other accounts" on the Accounts page (F09). Both were decided in the PRD review.
- Card charges entered from the bank screen (F08).
- Moving adjustments off `/banks/{id}/adjustments` (F13).
- Changes to Monthly, Mensais or any other existing page, beyond the shared bill fields in Mensais's dialog, which keeps the same look and behaviour.

## 2. Architecture Impact

**Where does this belong?** CashFlow.
- **Domain:** which actions an account allows is a rule about capabilities and the active flag, with no I/O.
- **Application:** composes accounts, balances and rows from the repository and maps them to DTOs.
- **API:** routes only.
- **Web:** displays them and runs the existing write endpoints.

**Layers and direction.**

```
Financial.Web   AccountsPage ──► getDefaultGroupSummary
                AccountScreenPage ──► useAccountScreen ──► getAccounts / getAccount / getAccountTransactions
                AccountActionBar ─► inline: ExpenseForm | IncomeForm | BalanceAdjustmentForm | TransferForm | PayRecurringBillForm
                App ──► Toaster      useSuccessToast   useDiscardPrompt + DiscardChangesDialog
      │ HTTP
Financial.Api   AccountsController (new, [Route("accounts")])
      │
Financial.CashFlow.Application
      AccountQueryService (new) ──► AccountTransactionReader (new, shared with AccountGroupSummaryService)
                                ──► AccountActions.AllowedFor, AccountBalanceRule.Balance
                                ──► ExpenseService/IncomeService/AccountTransferService/BalanceAdjustmentService.ToDto (internal static)
Financial.CashFlow.Domain
      AccountAction (new enum)   AccountActions (new, Rules/)
```

**What keeps Domain away from Infrastructure?** `ICashFlowRepository` is declared in Application. `AccountActions` takes an `Account` and reads nothing else.

**The template rule.** No new code reads `Account.Template`. The picker groups by `TemplateName`, and the actions come from `Has(Capability)`, so `AccountTemplateReadRuleTests`' allow-list is unchanged.

**SOLID.**
- **Open/closed:** a new template made of existing capabilities gets its actions with no code change. A new action is one enum value, one table entry and its form.
- **Single responsibility:**
  - `AccountActions` owns permissions.
  - `AccountTransactionReader` owns "which records affect balances".
  - `AccountQueryService` owns composition and the span/log contract.
- **Interface segregation:** a new `IAccountQueryService`. No existing service interface grows.
- **Dependency inversion:** the service depends on `ICashFlowRepository`, `ITelemetryTracer` and `ILogger<T>` only.

## 3. Technical Decisions

| Decision | Choice | Why |
|---|---|---|
| Account list | `GET accounts` returns every account ordered by name (`StringComparer.OrdinalIgnoreCase`, stable), active and inactive, with `IsActive` | Interview Q1. The picker filters to active accounts; F06's grid shows all. |
| Account detail route | `GET accounts/{id:guid}/{year:int}/{month:int}`. A month outside 1–12 throws `ArgumentOutOfRangeException` → 400. An unknown id → `FirstOrThrow` → 404 | PRD: the account with a balance for a month. The month is in the path, as on every other month route (`AccountGroupsController`, `BanksController`, `ExpensesController`); the caller always knows the month, so it is never optional |
| Balance | `AccountBalanceRule.Balance(account, endOfMonth, records)` for every month | PRD review decision 4: equal to Monthly and F04's summary |
| Allowed actions | A Domain enum `AccountAction` with `AddExpense`, `AddIncome`, `TransferTo`, `TransferFrom`, `PayRecurringBill`, `Reconcile`. `AccountActions.AllowedFor(account)` returns, in that order, each action whose capability the account has: `HasCategories`, `ReceivesIncome`, `CanSendTransfers`, `CanReceiveTransfers`, `CanPayBills`, `CanReconcile`. The wire value is the enum name as a string list | PRD: actions derived from capabilities. Each capability is the one its write path already checks (`Expense.cs:288`, `Income.cs:87`, `TransferPairRules`, `MensaisService.cs:251`, `BalanceAdjustment`). |
| Inactive account | `AllowedFor` returns an empty list. Rows have `CanEdit = false`. The screen shows an "Inactive" badge and is read-only | Interview Q4 |
| Shared record reader | `AccountTransactionReader.Read(ICashFlowRepository)` returns incomes, expenses, transfers and adjustments as `IAccountTransaction`. `AccountGroupSummaryService` and `AccountQueryService` use it. `BankService` keeps its private copy until F13 removes it | PRD review finding 1: this is the third use. Not touching `BankService` saves a file it would lose anyway. |
| Grid rows | Per type, records that reference the account and whose `Date` falls in the month: expenses by `PaymentSourceAccount`, incomes by `Account`, transfers by source or destination, adjustments by `Account` | `Date` is the date the balance uses, so the rows match the balance. Matching by reference, not by non-zero contribution, keeps a zero-delta adjustment visible and editable. |
| Row kinds | `Expense`, `CardCharge` (an expense with a credit card, settled onto this bank), `Income`, `TransferIn`, `TransferOut`, `Adjustment`; the wire value is a string | The web maps kinds to labels: Expense, Card charge, Income, Transfer in, Transfer out, Adjustment |
| Row amounts | Expense and card charge: `Amount = −Value` and `RoundUp = −RoundUpAmount` (null when none). Income: `+NetValue`. Transfer out: `−Amount`. Transfer in: `+AmountReceived()`. Adjustment: `+Delta`. Always `Amount + (RoundUp ?? 0) == ContributionTo(account)` | Interview Q3 |
| Row text | Description: the expense or income description, or the transfer or adjustment note (may be empty). Category: the expense's category name. Counterpart: the other account (transfers), the card (card charges), the income source (incomes) | PRD columns |
| Row source record | Each row carries exactly one of `Expense` (`ExpenseDTO`), `Income` (`IncomeDTO`), `Transfer` (`AccountTransferDTO`) or `Adjustment` (`BalanceAdjustmentDTO`), built by the owning service's `ToDto`, which becomes `internal static` | Interview Q2 and Q9 |
| Editable rows | `CanEdit` is false for `CardCharge` and for every row of an inactive account | Interview Q5: paid card charges stay managed through card statements |
| Row order | Date descending. Rows with the same date keep the reader's order: incomes, expenses, transfers, adjustments. Grid columns stay sortable with `useSortableRows` | Monthly lists newest first |
| Parity | After side only: `Add-ParityAccountFigures` reads `GET accounts` once, then `GET accounts/{id}/{year}/{month}` per account per month, recording `{month}/account/<name>/balance`. `figure-map.json` maps it to `{month}/banks/*/balance`. `Get-ParityReport` gains `-IncludeAccounts`, which `Invoke-Parity.ps1` passes for the after side | Interview Q6: AC 03 across every month |
| Accounts page | The F04 summary for the current month: name (a link), template, currency and balance, with an "Inactive" badge where relevant (from `GET accounts`, since the summary members carry no active flag), and per-currency totals as Monthly's Banks footer shows them. No month selector | PRD: from F04; F09 adds "Other accounts" |
| Month in the URL | `?month=YYYY-MM`. A missing or invalid value means the current month, and the URL is replaced. A change replaces the history entry. The picker keeps it. | PRD: the picker keeps the selected month |
| Picker | Fluent `Dropdown` (a short, known list; it is controlled, so focus stays on it across an account switch) with one `OptionGroup` per template name, listing active accounts by name. An inactive account being viewed shows as the value but not in the list. Selecting navigates to that account with the same month | PRD |
| Not found | A route id that isn't a GUID, or a 404 from `getAccount`: "This account does not exist." with a link to the Accounts page. Nothing else loads | PRD Error Handling. The client checks the GUID shape itself, so a bad link never reaches the SPA fallback. |
| Loading | A Fluent `Skeleton` for the header and the grid on first load. Later refreshes after a save are silent | PRD states |
| Empty grid | "No transactions in October 2026" (month name and year) | PRD |
| Locked bank | `ExpenseForm`, `IncomeForm`, `BalanceAdjustmentForm` and `TransferForm` and their hooks take an optional `lockedBank` (`{ id, name }`). When it's set, the bank shows as read-only text and its value is fixed. Monthly passes nothing. | PRD review decision 3 |
| Expense form on the account screen | Bank mode only. The payment-mode switch is hidden while `lockedBank` is set. Round-up shows only when the account rounds up, as today. | Card charges come in F08 |
| Keep open after Save | An optional `keepOpenAfterSave` on the expense and income hooks. After a successful create, the form stays open with the same date and category (expense) or date and income source (income), and clears description, value, round-up and the tithe flag (expense) or values and description (income). Monthly passes nothing and keeps closing. Edits always close. | PRD review decision 2 |
| One form at a time | The page holds one active form (an action or a row edit). Opening another while the current one is dirty asks to discard. A form is dirty when its values differ from the values it opened with. | PRD |
| Discard prompt | `useDiscardPrompt` returns `confirmDiscard(action)`. A clean form runs the action at once; a dirty form opens `DiscardChangesDialog` (Fluent `Dialog`, title "Discard unsaved changes?", body "Your changes to this form will be lost.", buttons "Keep editing" and "Discard"). It guards changing account, month or action, and opening a row edit. Sidebar navigation and closing the browser tab are not intercepted. | PRD review decision 6 |
| Toasts | One Fluent `Toaster` in `App.tsx`. `useSuccessToast()` returns `showSuccess(message)` (intent success, auto-dismiss). Messages: "Expense added", "Income added", "Expense updated", "Income updated", "Transfer saved", "Transfer updated", "Balance corrected", "Expense deleted", "Income deleted", "Transfer deleted", "Adjustment deleted", "Bill paid". A failure is never only a toast; it shows as a `MessageBar`. | PRD; the component-selection guide |
| Focus | Opening a form focuses its first field. Closing it (Cancel, or a save that closes) returns focus to the button that opened it, either the action button or the row's Edit button | PRD keyboard |
| Transfer to and from | `TransferForm` in account mode: Transfer to locks the source to this account and lists `transfer-destinations`. Transfer from locks the destination and lists `transfer-sources`. Saves call `accounts/transfers`. A "Destination amount" field shows only when the two currencies differ | F03 endpoints and rules. Monthly's `/transfers` use is unchanged. |
| Reconcile | `BalanceAdjustmentForm` with the bank locked. Its current balance is the header balance. It keeps its "Balance corrected" panel. Calls `/banks/{id}/adjustments`. | PRD review decision 8 |
| Edit and Delete | Edit opens the matching form with the row's source record. Delete opens `ConfirmDeleteDialog` ("Delete this expense? This cannot be undone.", and the same for income, transfer and adjustment). Delete calls `DELETE expenses/{id}`, `incomes/{id}`, `accounts/transfers/{id}` or `banks/{accountId}/adjustments/{id}`. Each save or delete refreshes the header and the grid silently. | PRD |
| Actions the web knows | The action bar renders each allowed action it has a form for, in the API's order, and ignores unknown values. Stage 3 adds Add expense and Add income, stage 4 adds Transfer to, Transfer from and Reconcile, stage 5 adds Pay recurring bill | AC 05 is met in full at stage 5. Each stage stays deployable. |
| Pay recurring bill | `BillExpenseFields` (Date, Description, Value, Bank, Category) is extracted from `UkExpensePromptDialog`, which now uses it. `PayRecurringBillForm` shows a bill picker and then the fields, with the bank locked. The picker lists `Area == UK` bills whose status isn't `Paid`, ordered by due day, as "Due 5 · Council tax · £120.00". Picking a bill pre-fills Description and Value; Date defaults to today; Category is required with no default. Save calls `POST mensais/{id}/paid` with this bank. | Interview Q7 |
| Already-paid bill | On a 400, the form re-reads the bills. If the chosen bill is now `Paid`: "This bill is already paid.", and the list refreshes. Otherwise the server message shows as for any rejected save. | The endpoint returns 400, not 409, and its message isn't a stable contract |
| Network failure | "Could not save. Check the connection and try again." in a `MessageBar` above the form, keeping the values | PRD |
| Narrow screens | The header, picker and month selector stack below 600 px. The action bar wraps. The grid scrolls inside its container. Nothing scrolls horizontally at 320 px. | PRD; `docs/ui/review-checklist.md` |
| Comments | Strict no-comments policy in all code, XML docs included. New controllers carry no XML docs, as `AccountTransfersController` doesn't. | CLAUDE.md |

## 4. Component Overview

Paths are relative to the repo root.

**PR 1: Account endpoints**

| File | Change |
|---|---|
| `Financial.CashFlow.Domain/Enums/AccountAction.cs` | **New.** `AddExpense, AddIncome, TransferTo, TransferFrom, PayRecurringBill, Reconcile` |
| `Financial.CashFlow.Domain/Rules/AccountActions.cs` | **New.** `public static class AccountActions` with `IReadOnlyList<AccountAction> AllowedFor(Account account)` and the capability table in §3 |
| `Financial.CashFlow.Application/Services/AccountTransactionReader.cs` | **New.** `internal static class` with `IReadOnlyList<IAccountTransaction> Read(ICashFlowRepository repository)` |
| `Financial.CashFlow.Application/Services/AccountGroupSummaryService.cs` | Its private `ReadAccountTransactions` is replaced by the shared reader |
| `Financial.CashFlow.Application/DTOs/AccountDTO.cs` | **New.** `Id`, `Name`, `TemplateName`, `Currency`, `IsActive` |
| `Financial.CashFlow.Application/DTOs/AccountDetailDTO.cs` | **New.** `Id`, `Name`, `TemplateName`, `Currency`, `IsActive`, `Year`, `Month`, `Balance`, `IReadOnlyList<string> AllowedActions` |
| `Financial.CashFlow.Application/DTOs/AccountTransactionDTO.cs` | **New.** `Id`, `Kind`, `Date`, `string? Description`, `string? CategoryName`, `string? Counterpart`, `Amount`, `decimal? RoundUp`, `CanEdit`, `ExpenseDTO? Expense`, `IncomeDTO? Income`, `AccountTransferDTO? Transfer`, `BalanceAdjustmentDTO? Adjustment` |
| `Financial.CashFlow.Application/Interfaces/IAccountQueryService.cs` | **New.** `GetAccounts()`, `GetAccount(Guid id, int year, int month)`, `GetTransactions(Guid id, int year, int month)` |
| `Financial.CashFlow.Application/Services/AccountQueryService.cs` | **New.** Read-only, with the span/log/try pattern of `AccountGroupSummaryService` (`StartServiceSpan("CashFlow", nameof(AccountQueryService), <operation>, "Account")`) |
| `Financial.CashFlow.Application/Services/ExpenseService.cs`, `IncomeService.cs`, `AccountTransferService.cs`, `BalanceAdjustmentService.cs` | `private static ToDto` → `internal static ToDto` (one word each) |
| `Financial.CashFlow.Application/DependencyInjection/CashFlowApplicationServiceCollectionExtensions.cs` | `AddSingleton<IAccountQueryService, AccountQueryService>()` |
| `Financial.Api/Controllers/AccountsController.cs` | **New.** `[ApiController] [Route("accounts")]`: `GET` (list), `GET {id:guid}/{year:int}/{month:int}`, `GET {id:guid}/transactions/{year:int}/{month:int}`. Each declares 200, plus 400 and 404 where they apply. |
| `scripts/parity/Parity.psm1` | `Add-ParityAccountFigures`, the `-IncludeAccounts` switch on `Get-ParityReport` and its call from `Add-ParityMonthFigures` |
| `scripts/parity/Invoke-Parity.ps1` | Passes `-IncludeAccounts` for the after side |
| `scripts/parity/figure-map.json` | `{month}/account/*/balance` → `{month}/banks/*/balance`, `negate: false`, reason "F05 account balance" |
| `scripts/parity/README.md`, `scripts/parity/Parity.test.ps1` | The `account` family row; `account` in `$figureFamilies`; a self-test case (docs and tests, not counted) |
| `Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json`, `Financial.Web/src/api/generated/openapi.ts` | Regenerated (generated files, not counted) |

**PR 2: Read-only screens**

| File | Change |
|---|---|
| `Financial.Web/src/navigation/navTree.ts` | "Accounts" `/cashflow/accounts` above Monthly in the CashFlow category |
| `Financial.Web/src/navigation/routes.tsx`, `Financial.Web/src/navigation/lazyPages.tsx` | `cashflow/accounts` and `cashflow/accounts/:accountId`, lazy-loaded |
| `Financial.Web/src/api/types.ts` | Aliases `AccountDto`, `AccountDetailDto`, `AccountTransactionDto` |
| `Financial.Web/src/api/financialApiClient.ts` | `getAccounts()`, `getAccount(id, year, month)`, `getAccountTransactions(id, year, month)` |
| `Financial.Web/src/pages/AccountsPage.tsx` | **New.** Default group for the current month via `getDefaultGroupSummary`. Loading, error with "Try again", and empty ("No accounts in the default group for this month") states. Rows link to the account screen. |
| `Financial.Web/src/pages/AccountScreenPage.tsx`, `AccountScreenPage.css` | **New.** Header, grid and states; not-found message; narrow-screen layout |
| `Financial.Web/src/hooks/useAccountScreen.ts` | **New.** Reads the route id and the `month` search param. Loads the account list, detail and transactions in one `Promise.all`. Exposes loading, error, retry, not-found, `refreshSilently` and the month setter. |
| `Financial.Web/src/components/AccountHeader.tsx` | **New.** Name, template, currency, balance, "Inactive" badge, picker and month input |
| `Financial.Web/src/components/AccountTransactionsGrid.tsx` | **New.** Fluent `Table` in a scroll container. Columns Date, Description, Type, Category, Counterpart, Amount, Round-up; numbers right-aligned and signed; sortable; empty message; skeleton rows while loading |
| `scripts/parity/screen-checklist.md` | Bank-screen rows (docs, not counted). See §7. |

**PR 3: Add expense and Add income**

| File | Change |
|---|---|
| `Financial.Web/src/components/ExpenseForm.tsx`, `Financial.Web/src/hooks/useExpenseForm.ts` | `lockedBank` and `keepOpenAfterSave`; payment-mode switch hidden while locked; dirty flag |
| `Financial.Web/src/components/IncomeForm.tsx`, `Financial.Web/src/hooks/useIncomeForm.ts` | `lockedBank` and `keepOpenAfterSave`; dirty flag |
| `Financial.Web/src/App.tsx` | One `<Toaster>` with a fixed toaster id |
| `Financial.Web/src/hooks/useSuccessToast.ts` | **New.** `showSuccess(message)` |
| `Financial.Web/src/hooks/useDiscardPrompt.ts`, `Financial.Web/src/components/DiscardChangesDialog.tsx` | **New.** As in §3 |
| `Financial.Web/src/components/AccountActionBar.tsx` | **New.** One button per known allowed action. It reports which action opened, so focus can go back to its button. |
| `Financial.Web/src/pages/AccountScreenPage.tsx` | The inline form area between the header/actions row and the grid; one active form; the discard guard on account, month and action changes; silent refresh and toast after save |

**PR 4: Reconcile, transfers, Edit and Delete**

| File | Change |
|---|---|
| `Financial.Web/src/components/BalanceAdjustmentForm.tsx`, `Financial.Web/src/hooks/useBalanceAdjustmentForm.ts` | `lockedBank`; current balance passed in; dirty flag |
| `Financial.Web/src/components/TransferForm.tsx`, `Financial.Web/src/hooks/useTransferForm.ts` | Account mode (§3): locked side, options from the API, destination amount when currencies differ, `accounts/transfers` calls; dirty flag |
| `Financial.Web/src/api/financialApiClient.ts`, `Financial.Web/src/api/types.ts` | `createAccountTransfer`, `updateAccountTransfer`, `deleteAccountTransfer`, `getTransferDestinations`, `getTransferSources`; aliases `AccountTransferDto`, `AccountTransferCreateDto`, `AccountTransferUpdateDto`, `TransferAccountOptionDto` |
| `Financial.Web/src/components/AccountTransactionsGrid.tsx` | Trailing Actions cell with Edit and Delete icon buttons (aria-labels "Edit <kind>", "Delete <kind>") on rows where `canEdit` is true |
| `Financial.Web/src/components/AccountActionBar.tsx` | Transfer to, Transfer from, Reconcile |
| `Financial.Web/src/pages/AccountScreenPage.tsx` | Row edit routed by kind; delete through `ConfirmDeleteDialog`; toasts |

**PR 5: Pay recurring bill**

| File | Change |
|---|---|
| `Financial.Web/src/components/BillExpenseFields.tsx` | **New.** The fields and validation taken from `UkExpensePromptDialog`, with an optional `lockedBank` |
| `Financial.Web/src/components/UkExpensePromptDialog.tsx` | Renders `BillExpenseFields`. Same look and behaviour. |
| `Financial.Web/src/components/PayRecurringBillForm.tsx` | **New.** Bill picker and `BillExpenseFields` with the bank locked; the already-paid handling in §3 |
| `Financial.Web/src/hooks/usePayRecurringBill.ts` | **New.** Loads `getMensaisBills`, filters and orders the bills, saves through `markMensaisBillPaid` |
| `Financial.Web/src/components/AccountActionBar.tsx`, `Financial.Web/src/pages/AccountScreenPage.tsx` | Pay recurring bill |
| `docs/prd/.../prd-cashflow-accounts-generalization.md` | F05 boxes and the cross-feature boxes in §7, in their own commit |

## 5. API Contracts

Base path `/api/v1/financial`. `ArgumentException` → 400 and `KeyNotFoundException` → 404, through the existing middleware. All three routes are read-only.

**`GET accounts`**: 200 with `AccountDTO[]`, ordered by name.

```json
[
  { "id": "8f3b1c1a-2e3a-4b1a-9a7f-100000000001", "name": "Barclays", "templateName": "Bank", "currency": "GBP", "isActive": true },
  { "id": "8f3b1c1a-2e3a-4b1a-9a7f-100000000003", "name": "Old Savings", "templateName": "Bank", "currency": "GBP", "isActive": false }
]
```

**`GET accounts/{id}/2026/10`**: 200 with `AccountDetailDTO`. 400 when the month is not 1–12. 404 when the id is unknown.

```json
{
  "id": "8f3b1c1a-2e3a-4b1a-9a7f-100000000001",
  "name": "Barclays",
  "templateName": "Bank",
  "currency": "GBP",
  "isActive": true,
  "year": 2026,
  "month": 10,
  "balance": 1234.56,
  "allowedActions": ["AddExpense", "AddIncome", "TransferTo", "TransferFrom", "PayRecurringBill", "Reconcile"]
}
```

**`GET accounts/{id}/transactions/{year}/{month}`**: 200 with `AccountTransactionDTO[]`, date descending. 400 and 404 as above. Each row has exactly one source slot filled.

```json
[
  {
    "id": "c1…", "kind": "Expense", "date": "2026-10-14", "description": "Tesco",
    "categoryName": "Groceries", "counterpart": null, "amount": -42.10, "roundUp": -0.90, "canEdit": true,
    "expense": { "id": "c1…", "date": "2026-10-14", "description": "Tesco", "value": 42.10, "roundUpAmount": 0.90, "…": "ExpenseDTO" },
    "income": null, "transfer": null, "adjustment": null
  },
  {
    "id": "d2…", "kind": "TransferOut", "date": "2026-10-10", "description": "Savings",
    "categoryName": null, "counterpart": "Chase", "amount": -200.00, "roundUp": null, "canEdit": true,
    "expense": null, "income": null,
    "transfer": { "id": "d2…", "sourceAccountName": "Barclays", "destinationAccountName": "Chase", "amount": 200.00, "destinationAmount": 200.00, "…": "AccountTransferDTO" },
    "adjustment": null
  }
]
```

No existing route changes shape. The web starts calling F03's `accounts/transfers`, `transfer-destinations` and `transfer-sources` (stage 4) and the existing `mensais/{id}/paid` (stage 5).

## 6. Data Model and Business Rules

**Data file.** Unchanged; still version 2. Nothing new is stored.

**Allowed actions.** Empty for an inactive account. Otherwise every action whose capability the account has, in the fixed order in §3. A Bank has all six.

**Rows for a month** (`start` = day 1, `end` = last day), for account `A`:
1. Expenses with `PaymentSourceAccount == A` and `start <= Date <= end`. Kind `CardCharge` when the expense has a credit card, otherwise `Expense`.
2. Incomes with `Account == A` and `Date` in the month.
3. Transfers with `A` as source (kind `TransferOut`) or destination (kind `TransferIn`) and `Date` in the month.
4. Balance adjustments with `Account == A` and `Date` in the month.

These are sorted by date descending. For every row, `Amount + (RoundUp ?? 0) == record.ContributionTo(A)`. The rows' month total therefore equals the balance at the end of this month minus the balance at the end of the previous month, for every month from the balance start date on.

**Account screen state (web).**
- **Load:** list, detail and transactions in parallel. The first load shows skeletons.
- **Refresh after a write:** silent; the header and the grid update together.
- **Month change:** guarded by the discard prompt, then the URL updates and the data reloads.
- **Account change:** guarded, then the app navigates to the new id with the same month.

**Error handling.**
- The three endpoints can't fail a write. Invalid input returns 400, an unknown id 404, and anything else marks the span failed and returns 500.
- On the screen:
  - a load failure shows "Unable to load this account" with "Try again";
  - a 404, or a malformed id, shows the not-found message;
  - a rejected save shows the field messages inline and leaves the grid unchanged;
  - a network failure shows the `MessageBar` and keeps the values;
  - delete and pay-bill failures behave the same.

## 7. Testing Strategy

Following the `testing-guide-Financial` skill:
- **Unit:** Domain rules, the service, hooks and components.
- **Integration:** the API host through `ApiEndpointTests`, and the pages with `financialApiClient` mocked at module level.
- **AC tracing:** one tagged test per PRD F05 bullet, `[Trait("AC", "P60-F05-bank-account-screen-NN")]`, numbered 01–14 in PRD order. Web tests carry the tag in the test name.
- **Other rules:**
  - every class carries `[Trait("Category", …)]`;
  - names follow `Method_Condition_Result`;
  - shared doubles live only in `Tests/Financial.TestUtilities`;
  - failure paths assert the failed span and the logged exception type;
  - every component test covers the negative path.

| Test file | PR | Type | Target | Coverage goal |
|---|---|---|---|---|
| `Tests/Financial.CashFlow.Domain.Tests/Rules/AccountActionsTests.cs` | 1 | Unit | Action rule | Bank gets all six in order; each action appears only with its capability (minimal test account per capability); inactive gets none |
| `Tests/Financial.CashFlow.Application.Tests/Services/AccountQueryServiceTests.cs` | 1 | Unit | Service | List order (case-insensitive, stable); detail balance; unknown id throws `KeyNotFoundException`; invalid month throws; row kinds, signs, round-up, counterpart and category; `CanEdit` false for card charges and inactive accounts; exactly one slot per row; zero-delta adjustment listed; failed span on exception |
| `Tests/Financial.CashFlow.Application.Tests/Services/AccountGroupSummaryServiceTests.cs` | 1 | Unit | Summary | Unchanged and still green after the reader swap |
| `Tests/Financial.Api.Tests/Acceptance/AccountEndpointsAcceptanceTests.cs` | 1 | Integration (AC) | Host | ACs 03, 04 and 05 (API half), and the cross-feature transfer criterion |
| `Tests/Financial.Api.Tests/ConstructorGuardTests.cs`, `OpenApiContractTests.cs` | 1 | Unit / Integration | Controller, snapshot | Picked up automatically; the snapshot diff adds only the three routes and three schemas |
| `Tests/Financial.Architecture.Tests/AccountTemplateReadRuleTests.cs` | 1 | Integration | Allow-list | Unchanged and green |
| `scripts/parity/Parity.test.ps1` | 1 | Self-test | Parity | Account keys built only with `-IncludeAccounts`; the mapping resolves to the bank balance key |
| `Financial.Web/src/api/__tests__/financialApiClient.test.ts` | 2, 4 | Unit | Client | URLs, methods and bodies of the new calls; `ApiError` on non-2xx |
| `Financial.Web/src/hooks/__tests__/useAccountScreen.test.ts` | 2 | Unit | Hook | Month from the URL, an invalid one replaced; one parallel load; 404 → not found; malformed id → not found with no call; retry; silent refresh |
| `Financial.Web/src/pages/__tests__/AccountsPage.test.tsx` | 2 | Integration (frontend) | Page | AC 01; loading, error with retry, empty; inactive badge; link targets |
| `Financial.Web/src/pages/__tests__/AccountScreenPage.test.tsx` | 2–5 | Integration (frontend) | Page | ACs 02, 03, 05–11 and 13 as their stages land (table below) |
| `Financial.Web/src/components/__tests__/AccountHeader.test.tsx`, `AccountTransactionsGrid.test.tsx` | 2, 4 | Unit | Components | Header text and badge; picker groups and active-only list; grid columns, signs, right alignment, empty message, sorting; row actions only where `canEdit` |
| `Financial.Web/src/hooks/__tests__/useExpenseForm.test.ts`, `useIncomeForm.test.ts` | 3 | Unit | Hooks | Locked bank fixed in the request; keep-open keeps date and category and clears the rest; Monthly default still closes; dirty flag |
| `Financial.Web/src/hooks/__tests__/useDiscardPrompt.test.ts`, `useSuccessToast.test.ts`; `components/__tests__/DiscardChangesDialog.test.tsx`, `AccountActionBar.test.tsx` | 3–5 | Unit | Building blocks | Clean form runs at once; dirty opens the dialog; Keep editing cancels; Discard runs; toast dispatched; only known allowed actions rendered, in the API's order |
| `Financial.Web/src/hooks/__tests__/useTransferForm.test.ts`, `useBalanceAdjustmentForm.test.ts` | 4 | Unit | Hooks | Account mode endpoints and options; destination amount only for different currencies; locked side; Monthly mode unchanged |
| `Financial.Web/src/components/__tests__/BillExpenseFields.test.tsx`, `PayRecurringBillForm.test.tsx`; `hooks/__tests__/usePayRecurringBill.test.ts` | 5 | Unit | Pay bill | Only unpaid UK bills, by due day; pre-fill; category required; locked bank in the request; already-paid handling |
| `Financial.Web/src/pages/__tests__/MensaisPage.test.tsx`, `MonthlyPage.test.tsx` and the existing form tests | 3–5 | Integration (frontend) | Old pages | AC 13: unchanged and green |

The acceptance tests seed their own data with `ReplaceCashFlowDataBeforeStart` and a `TestClock` fixed at 2026-10-15:
- **banks:** Barclays (GBP, round-up on), Chase (GBP) and an inactive GBP bank;
- **records:** expenses with and without round-up; an expense on a card settled onto Barclays; an income with a source; a transfer Barclays → Chase; a balance adjustment on Barclays.

**Key functions:**

| Test function | AC | PR | Assertions |
|---|---|---|---|
| `AccountsPage_defaultGroup_listsMembersWithBalancesAndTotals (AC P60-F05-bank-account-screen-01)` | 01 | 2 | Members in name order with balances; per-currency totals; a row opens `/cashflow/accounts/{id}` |
| `AccountScreenPage_pickerChange_keepsMonth (AC …-02)` | 02 | 2 | Choosing Chase in October navigates to Chase with `month=2026-10` |
| `GetAccount_AnyMonth_BalanceEqualsGroupSummary` | 03 | 1 | Balance equals the summary member's balance for the current month and an earlier one |
| `AccountScreenPage_header_showsNameTemplateCurrencyAndBalance (AC …-03)` | 03 | 2 | "Barclays · Bank", the balance and the currency from the detail |
| `GetTransactions_SeededMonth_ListsEveryRecordSigned` | 04 | 1 | Every seeded record appears with the right kind and sign; round-up separate; settled card charge as `CardCharge`, not editable |
| `AccountTransactionsGrid_rows_signedAndRightAligned (AC …-04)` | 04 | 2 | Amount cells right-aligned with sign |
| `GetAccount_Bank_ReturnsSixAllowedActions` and `GetAccount_InactiveBank_ReturnsNoActions` | 05 | 1 | Exact list and order; empty for inactive |
| `AccountScreenPage_actions_renderExactlyAllowedActions (AC …-05)` | 05 | 5 | Six buttons for a bank; a reduced list renders only those; an inactive account renders none |
| `AccountScreenPage_secondActionWhileDirty_asksToDiscard (AC …-06)` | 06 | 3 | One form at a time, placed above the grid; dirty switch opens the dialog; Keep editing keeps values |
| `AccountScreenPage_addExpense_savesRefreshesAndStaysOpen (AC …-07)` | 07 | 3 | `POST expenses` with the locked bank; detail and transactions refetched; form open with the same date and category; toast "Expense added" |
| `AccountScreenPage_payRecurringBill_paysWithThisBank (AC …-08)` | 08 | 5 | Lists only unpaid UK bills; `POST mensais/{id}/paid` with this bank as `paymentSourceBankId` |
| `AccountScreenPage_rowEditAndDelete_workWithConfirmation (AC …-09)` | 09 | 4 | Edit opens the matching form pre-filled; Delete asks, then calls the right endpoint |
| `AccountScreenPage_states_behaveAsSpecified (AC …-10)` | 10 | 5 | Skeleton, empty message, inline validation, `MessageBar` keeping values, saving disables Save, success toast, discard prompt |
| `AccountScreenPage_keyboard_focusMovesIntoFormAndBack (AC …-11)` | 11 | 4 | Focus on the first field when opened; back on the opening button after Cancel |
| `GetTransactions_TransferBetweenAccounts_AppearsOnBothAndMovesBothBalances` | Cross-feature (F03 → F05) | 1 | `POST accounts/transfers`, then the row shows on both accounts with opposite signs and both balances move |

**Proved by the Definition of Done rather than a test:**
- **AC 03 on real data:** the stage 1 parity run maps `account/*/balance` to every bank balance in all months, with 0 differences.
- **AC 12 (320 px) and the visual half of AC 11:** the ui-reviewer on stages 2–5 and a manual check at 320 px in the screen-checklist rows.
- **AC 13:** every existing Monthly and Mensais test stays green, and the parity screen check on Monthly is unchanged.
- **AC 14:** parity 0 differences on every stage; the screen-checklist bank rows ticked in stages 2 and 5; `/simplify` and the architecture-reviewer on every stage; the ui-reviewer on stages 2–5.

**Screen-checklist rows** added in stage 2, for M1–M3:
- **B1:** the Barclays header balance equals Monthly's Barclays balance for the month.
- **B2:** the Barclays grid has every expense, income, transfer and adjustment Monthly shows for Barclays that month, with the same amounts, and round-ups in their own column.
- **B3:** a month with no records shows "No transactions in <Month Year>".
- **B4:** at 320 px wide there's no horizontal page scroll.

**Cross-feature criteria.** Stage 5 ticks:
- "The shared balance calculation from F02 gives the balances shown by F04's summary and F05's header" (F02, F04 and F05 have all shipped);
- "Records written by F03's transfer appear in F05's Transactions grid for both accounts and move both balances" (F03 and F05).

The three that name F06, F07, F08, F10 or F12 stay unticked.
