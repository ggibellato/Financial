# Implementation Plan: Linked Deposit, Withdrawal and Pots Screen

**Prerequisites:**
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7. No new packages.
- The user downloads the live file to `data/data-cashflow.json` before each stage's parity run (`scripts/parity/Invoke-Parity.ps1 -Step All`, baseline `p60-deployed-f14`). Parity reads that file and never writes it. Local runs use `data/dev/data-cashflow.json`.
- Implement each stage on its own branch off an up-to-date `main`, starting only after the previous stage's PR is merged. Server stages 1–7: at most 8 non-test code files and about 15 in total (generated `openapi.ts` excluded); propose a split instead of pushing anything larger. Web stages 8–11 may exceed it (user, 2026-10-31); state the count in the PR.
- Strict no-comments policy in all code, XML docs included.

### Stage 1: Linked withdrawal through the Reserva route

**1. Operation link** - Give expenses and pot movements an optional operation id, written to the file only when set. See spec §3 "Link" and §6.

**2. Withdrawal rule** - Add the rule that builds a withdrawal's movement and bank expenses with one operation id, and make the Reserva withdrawal use it. See spec §3 "Withdrawal rule" and "Reserva routes".

**3. Guards** - Refuse editing or deleting a linked expense or movement through the old routes with the 409 messages. See spec §3 "Guards on old routes".

**4. Stage 1 verification** - Run the suites, parity, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 2: Withdrawal operations API

**5. Pots service and routes** - Add the pots service and controller with the withdrawal create route and the operation read, edit and delete routes. See spec §5.

**6. Reading an operation** - Rebuild an operation's form values from its records. See spec §3 "Reading an operation".

**7. Stage 2 verification** - Regenerate the snapshot and web types, run the suites, parity, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 3a: Manual deposit

**8. Deposit rule** - Add the rule that builds a deposit into the active pots or one pot with 0–5 funding lines. See spec §3 "Deposit rule".

**9. Deposit route** - Add the deposit route and deposit edit through `PUT accounts/{potsId}/deposits/{operationId}`.

**10. Stage 3a verification** - Regenerate the snapshot and web types, run the suites, parity, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 3b: Reserva routes through the operations

**10a. Reserva income split** - Run `POST reserve/income-split` as a manual deposit with no bank into the active pots of the first Pots account, through `IPotsService`.

**10b. Reserva withdrawal** - `POST reserve/withdrawals` delegates to `IPotsService`, so it also refuses a future date.

**10c. Shared checks** - Move the shared bank and overdraft checks out of `PotsService` into `Application/Validation`.

**10d. Stage 3b verification** - Regenerate the snapshot and web types if the contract changes, run the suites, parity, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 4: Deposit from an income

**11. Income deposit** - Write, rebuild and remove an income's linked deposit with its funding lines, and keep older split incomes on today's path. See spec §3 "Deposit from an income" and "Older split incomes".

**12. Stage 4 verification** - Regenerate the snapshot and web types, run the suites, parity, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 5: Manage pots (server)

**13. Replace pots** - Save all of an account's pots together with the 100% and no-delete rules. See spec §3 "Manage pots".

**14. Stage 5 verification** - Regenerate the snapshot and web types, run the suites, parity, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 6: Pots read, old-delete fix and parity

**15. Pots read** - Return each pot's balance at the end of a month and the account total. See spec §3 "Pots read".

**16. Old manual-split delete** - Narrow the legacy delete to the manual split's own lines. See spec §3 "Old manual-split delete".

**17. Parity** - Emit the pots figures and map them. See spec §3 "Parity".

**18. Stage 6 verification** - Regenerate the snapshot and web types, run the suites, the parity self-test and parity, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 7: Actions and rows

**19. Actions** - Add Deposit, Withdraw, Manage pots and Withdraw from pots to the capability-driven actions. See spec §3 "Account actions".

**20. Rows** - List a Pots account's movements and give linked rows their operation link. See spec §3 "Rows".

**21. Stage 7 verification** - Regenerate the snapshot and web types, run the suites, parity, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 8: Pots screen (read)

**22. Tabs and Pots tab** - Show the Pots and Transactions tabs on a Pots account, with the pots grid and its states. See spec §3 "Web: Pots screen".

**23. Movement rows and Accounts links** - Show the movement rows and make the Accounts page Pots rows open the Pots tab. Legacy rows keep today's edit and delete (PRD): wire them to `reserve/movements/{id}` from the row's `movement`, add the `PotTransaction` entries to `KIND_LABELS` and `RECORD_NOUNS`, and remove the `PotTransaction` guard stage 7 put in `hasRowActions`.

**24. Stage 8 verification** - Run the web suites and build, parity, `/simplify`, the architecture-reviewer and the ui-reviewer with `docs/ui/review-checklist.md`, then push and open the PR stating its size.

### Stage 9: Manage pots dialog and Admin retirement

**25. Manage pots dialog** - Edit all pots together with a running total and the server's errors.

**26. Retire Admin → Reserve buckets** - Remove the page, its menu entry, its components and the reserve-bucket write routes, and redirect its route. See spec §3 "Admin retirement".

**27. Stage 9 verification** - Regenerate the snapshot and web types, run the suites and build, parity, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR stating its size.

### Stage 10: Deposit and Withdraw forms

**28. Forms** - Add the Deposit and Withdraw forms with funding lines, the split preview and the overdraft dialog. See spec §3 "Web: forms".

**29. Both sides and edit** - Add Withdraw from pots on banks and open an operation's form from the `operation` query value.

**30. Stage 10 verification** - Run the web suites and build, parity, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR stating its size.

### Stage 10b: Split line count and structured overdraft (server)

**30a. Split line count** - Add `splitLineCount` to `ReserveMovementDTO`, set by `ReserveService` from `IsLineOfSameManualSplit`, on the movement list and the account rows. Additions-only contract change: regenerate the snapshot and web types, and update the web test fixtures the required property changes. See spec §3 "Split line count".

**30a2. Structured overdraft 409** - `OverdraftConfirmationRequiredException` takes the pot name and balance and builds today's message; the middleware writes `code`, `potName` and `balance` as `ProblemDetails` extensions, pinned by a middleware test. No snapshot change for these fields. The web keeps parsing the text in this stage. See spec §3 "Structured overdraft 409".

**30b. Stage 10b verification** - Run the suites and build, parity, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 11: Income funding lines, bank links and ticks

**31. Funded from** - Add the funding lines section to the shared income form. See spec §3 "Web: income funding".

**32. Bank links and fixes** - Link linked bank rows to their operation, align the withdrawal category list with the server, and use the server's `splitLineCount` on the Pots screen's delete dialog and toast and on the Reserva page's delete warning. Remove the web copy of the rule (`isManualSplitLine`, `linesDeletedWith`); a form open on another deleted line still closes.

**32a. Overdraft dialog** - Extend `ApiError` to carry the 409's `code` and `balance`, with a small hand-written web type because they are not in the snapshot, then switch `usePotWithdrawalForm` from the message regex to those fields. A 409 without the code keeps today's handling. See spec §3 "Structured overdraft 409".

**33. PRD ticks** - Tick the F10 boxes in their own commit, each naming its test; tick a cross-feature box only when every feature it names has shipped.

**34. Stage 11 verification** - Run the suites and build, parity, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR stating its size.
