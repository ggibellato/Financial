# Implementation Plan: Admin Accounts Page

**Prerequisites:**
- .NET 10 SDK and Node, as for F05. No new packages: Fluent UI v9 already ships `Dialog`, `Select`, `Switch` and `Toaster`.
- A fresh copy of the live `data-cashflow.json`, downloaded from Google Drive, for the parity run at the end of each PR. The parity baseline is the `p60-deployed-f05` tag, which is the default of `Invoke-Parity.ps1`.
- Each PR is implemented on its own branch off an up-to-date `main`. Each starts only after the previous one is merged.
- Strict no-comments policy in all code, XML docs included.

### Stage 1: PR 1, account endpoints

**1. Account rules** - Give `Account` the trimmed 60-character name rule, `SetActive`, `ChangeTemplate` and the allowed-currency check in `Create`, and give `AccountTemplates` the allowed currencies and the template list, as spec §3 and §6 describe. Replace the reflection setter in `TransferTests` with `SetActive(false)`.

**2. Shared reference reader and uniqueness** - Add the internal `AccountReferenceReader`, and the optional comparer on `EnsureNameIsUnique`. Make account names unique ignoring case on `/banks` too, keeping its own message.

**3. Admin service and endpoints** - Add the three DTOs, extend `AccountDTO`, and fill the new fields in `AccountQueryService`. Add `IAccountAdminService` and `AccountAdminService` with the rules and messages of spec §5, register it, and add the four routes to `AccountsController`. Regenerate the OpenAPI snapshot and the web types, and check that the diff only adds.

**4. Acceptance tests** - Add the tagged API acceptance tests for ACs 01–05 and the F05 half of cross-feature line 839, on seeded data with one used and one unused account.

**5. PR 1 verification and hand-off** - Run the full .NET suite and parity on the fresh live copy, expecting 0 differences and an unchanged SHA-256. Run `/simplify` and the architecture-reviewer, then push the branch and open the PR, without merging.

### Stage 2: PR 2, inactive accounts in the API

**6. Inactive rule** - Add `ActiveAccountRule` in Domain `Rules/`. Call it from `Expense` (create, account-moving edit, settle), `Income` (create, account-moving edit), `BalanceAdjustment` (create) and `Transfer`, which drops its private copy.

**7. Banks list** - Add `IsActive` to `BankDTO` and switch `BankService` to the shared reference reader, with no other change to `/banks`. Regenerate the snapshot and the web types.

**8. Acceptance tests** - Add the tagged API tests for AC 06, covering every route listed in spec §5, plus the deletes and same-account edits that must still work.

**9. PR 2 verification and hand-off** - Run the full .NET suite and parity on a fresh live copy. Run `/simplify` and the architecture-reviewer, then push and open the PR, without merging.

### Stage 3: PR 3, Admin Accounts page

**10. Client and navigation** - Add the type aliases and the four client calls. Add the Accounts entry under Admin → CashFlow and its lazy route.

**11. Page and hook** - Build `useAdminAccounts` on `useCrudResource` and `AdminAccountsPage` on `AdminEntityPage`, with the grid, the delete flow that offers Deactivate for an account with records, and the toasts.

**12. Account dialog** - Build `AccountFormDialog` on `EntityFormDialog`: template first, fields by capability, per-field validation on submit, the duplicate-name message under Name, read-only template and currency with records, and the discard prompt on close.

**13. PR 3 verification and hand-off** - Run the web lint, tests and build and the full .NET suite. Run parity on a fresh live copy with `-KeepRunning`, and check Monthly's and the bank screen's checklist rows. Run `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR, without merging.

### Stage 4: PR 4, inactive banks hidden in pickers

**14. Selection helper** - Add `selectableBanks`, which keeps the active banks plus the currently selected one, in the given order.

**15. Forms** - Use it in `ExpenseForm`, `IncomeForm`, `BalanceAdjustmentForm`, `TransferForm` (both sides), `BillExpenseFields`, `WithdrawalForm` and `CardsGrid`'s statement payment bank. Monthly, Mensais and Reserva must look and behave exactly as before while every bank is active.

**16. PR 4 verification and hand-off** - Run the web lint, tests and build. Run parity on a fresh live copy with `-KeepRunning`, and check Monthly's and the bank screen's rows. Run `/simplify`, the architecture-reviewer and the ui-reviewer. Tick the F06 PRD criteria in their own commit, then push and open the PR, without merging.
