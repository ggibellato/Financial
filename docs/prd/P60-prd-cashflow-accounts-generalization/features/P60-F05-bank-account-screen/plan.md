# Implementation Plan: Bank Account Screen

**Prerequisites:**
- .NET 10 SDK and Node, as for F04. No new packages: Fluent UI v9 already ships `Toaster`, `Dropdown`, `Skeleton` and `Dialog`.
- A fresh copy of the live `data-cashflow.json`, downloaded from Google Drive, for the parity run at the end of each PR. The parity baseline is the `p60-baseline` tag.
- Each PR is implemented on its own branch off an up-to-date `main`. Each starts only after the previous one is merged.
- Strict no-comments policy in all code, XML docs included.

### Stage 1: PR 1, account endpoints

**1. Action rule** - Add the `AccountAction` enum and the `AccountActions` rule in Domain `Rules/`, mapping capabilities to actions in the fixed order of spec §3. An inactive account allows none.

**2. Shared reader and mappers** - Add the shared `AccountTransactionReader` and switch `AccountGroupSummaryService` to it, with no change in behaviour. Make the four `ToDto` mappers `internal static`.

**3. Query service and controller** - Add the three DTOs, `IAccountQueryService` and `AccountQueryService`, with the list, detail and row rules of spec §3 and §6, and register the service. Add `AccountsController` with the three routes. Regenerate the OpenAPI snapshot and the web types, and check that the diff adds only those routes and schemas.

**4. Acceptance tests** - Add the tagged acceptance tests for the API halves of ACs 03, 04 and 05 and the cross-feature transfer criterion, on the seeded data in spec §7.

**5. Parity** - Add the after-side account balance figures and their mapping, and extend the parity README and self-test.

**6. PR 1 verification and hand-off** - Run the full .NET suite and the parity self-test. Then run parity on the fresh live copy: expect 0 differences, every account key resolved through the mapping, and an unchanged SHA-256 for the copy. Run `/simplify` and the architecture-reviewer, then push the branch and open the PR, without merging.

### Stage 2: PR 2, read-only screens

**7. Client and navigation** - Add the type aliases and the three client calls. Add the Accounts menu entry above Monthly and the two lazy routes.

**8. Accounts page** - Build the Accounts page on the F04 summary for the current month, with its states and its links to the account screen.

**9. Account screen** - Build the screen hook (month in the URL, parallel load, not-found, silent refresh), the header with the picker and month input, and the transactions grid. Give the page loading, empty, error and not-found states and the narrow-screen layout in spec §3.

**10. PR 2 verification and hand-off** - Run the web lint, tests and build and the full .NET suite. Add the bank-screen rows to `screen-checklist.md` and check them, along with Monthly's rows, in a parity run with `-KeepRunning`. Run `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR, without merging.

### Stage 3: PR 3, Add expense and Add income

**11. Form settings** - Give the expense and income forms and hooks the optional locked bank, keep-open-after-save and dirty flag. Monthly passes nothing and must behave exactly as before.

**12. Feedback building blocks** - Mount one `Toaster` in `App.tsx` and add the success-toast hook. Add the discard-prompt hook and dialog.

**13. Actions on the screen** - Add the action bar and the inline form area: one form at a time, the discard guard on account, month and action changes, focus moving into the form and back, a silent refresh and a toast after save.

**14. PR 3 verification and hand-off** - Run the web lint, tests and build, a parity run, `/simplify`, the architecture-reviewer and the ui-reviewer. Then push and open the PR, without merging.

### Stage 4: PR 4, Reconcile, transfers, Edit and Delete

**15. Adjustment and transfer forms** - Give the adjustment form the locked bank and the header balance. Give the transfer form its account mode: locked side, options from F03, destination amount when currencies differ, and the `accounts/transfers` calls. Monthly's use of both forms is unchanged.

**16. Row actions** - Add Edit and Delete to editable grid rows. Edit opens the matching form with the row's source record; Delete confirms with `ConfirmDeleteDialog` and calls the record's endpoint. Add Transfer to, Transfer from and Reconcile to the action bar.

**17. PR 4 verification and hand-off** - Run the web lint, tests and build, a parity run, `/simplify`, the architecture-reviewer and the ui-reviewer. Then push and open the PR, without merging.

### Stage 5: PR 5, Pay recurring bill

**18. Shared bill fields** - Extract the bill-expense fields from Mensais's prompt dialog into one component, which the dialog now uses with no visible change.

**19. Pay recurring bill** - Add the bill picker form and its hook: unpaid UK bills by due day, pre-filled fields, this bank locked, and the already-paid handling in spec §3. Add the action to the action bar.

**20. PR 5 verification, PRD ticks and hand-off** - Run the full .NET suite and the web lint, tests and build. Run parity with `-KeepRunning` and recheck the bank-screen rows and Monthly's rows. Run `/simplify`, the architecture-reviewer and the ui-reviewer. Tick the F05 criteria and the two cross-feature criteria in spec §7 in their own commit, then push and open the PR, without merging.
