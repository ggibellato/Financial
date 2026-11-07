# Implementation Plan: Settlement Screen

**Prerequisites:**
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7. No new packages.
- The live file for parity is the 2026-11-07 copy at `E:\dev\projetos\financial\data\data-cashflow.json` (V5, SHA-256 starts `1b608c06`). Every PR runs `scripts/parity/Invoke-Parity.ps1 -Step All -LiveCopy "E:\dev\projetos\financial\data\data-cashflow.json"` against `p60-deployed-f11` and expects `Parity OK: 0 differences` with the SHA-256 unchanged. Parity reads that file and never writes it; local runs use `data/dev/data-cashflow.json`.
- Implement each PR on its own branch off an up-to-date `main`, starting only after the previous PR is merged. Six PRs: 1a, 1b, 2, 3a, 3b, 4 (stage 1 and stage 3 don't fit 8 non-test code files as one PR). Count the files before pushing: at most 8 non-test code files and about 15 in total (generated `openapi.ts` excluded); 1b and 2 may exceed the total only by the typed-fixture sweep the spec names (§3 "PR size"). Propose a split instead of pushing anything larger.
- Strict no-comments policy in all code, XML docs included.
- `controle-mae`, the Controle Mãe page and its hook stay unchanged until F13.
- Deploy only after stage 4.

### Stage 1 — Settlement API

**PR 1a — Entry routes**

**1. Entry edit and rounding** - Let a Settlement entry change its date, description, note, paid by, amount and converted value in one validated step, never its currency, and store a converted value that rounds to 0.00 as empty instead of refusing it. See spec §3 "Edit" and "Converted value rounding to 0.00".

**2. Settlement service** - Add the service that adds, edits and deletes the entries of any active account with participants, refusing future dates, working out a new entry's converted value from the date's rate, and returning a warning naming the other currency when that value is left empty. See spec §3 "Service", "Paid by on the wire", "Currency on add", "Converted value on add", "Future date" and "Save warning".

**3. Entry routes** - Register the service and expose the add, edit and delete routes on a new Settlement controller with the request and response shapes and errors of spec §5.

**4. PR 1a verification** - Regenerate the snapshot and web types, run the suites and the parity run, `/simplify` and the architecture-reviewer, then push and open the PR.

**PR 1b — Settle route, actions and account detail**

**5. Settlement actions** - Give active accounts with participants the Add entry and Settle actions, and add the rule that disables Settle with "Nothing to settle." when every currency's current balance rounds to 0.00. See spec §3 "Actions".

**6. Settle route** - Add the Settle route that saves an entry with the values as entered and refuses an account with nothing to settle. See spec §3 "Settle route" and §5.

**7. Account detail** - Add the current balance per currency, the other participant and the disabled actions with their reasons to the account detail, empty for accounts without participants. See spec §3 "Disabled reason to the web" and "Account detail".

**8. PR 1b verification** - Regenerate the snapshot and web types and update the typed account-detail test fixtures. Run the suites and the parity run, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 2 — Rows

**9. Row currency and entry rows** - Give every transaction row the currency its amount is in, and add one row per Settlement entry to the account's rows and to the month rows, signed by who paid, in the entry's own currency, with its entry attached for editing. See spec §3 "Row currency" and "Settlement rows".

**10. All transactions** - Show each row's amount with its own currency and total the footer per row currency, with Settlement rows labelled as entries. See spec §3 "All transactions" and "Labels".

**11. Entry row delete** - Delete an entry row through its Settlement route from wherever it shows.

**12. Stage 2 verification** - Regenerate the snapshot and web types and add the currency to the typed row fixtures. Run the suites and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 3 — Screen

**PR 3a — Header, grid, picker and links**

**13. Settlement header** - Show a Settlement account's current balance per currency with who owes whom, or "Settled", independent of the viewed month, and its currencies beside the template name. See spec §3 "Header".

**14. Settlement grid** - Give the account grid a Settlement column set with Paid by, the amount in its currency, the converted value or "—", and the note. See spec §3 "Grid columns".

**15. Picker and Accounts page** - List active Settlement accounts in the account-screen picker and make the Accounts page's Settlement rows open their screen. See spec §3 "Picker and links".

**16. PR 3a verification** - Invert screen checklist row S3 and add S4–S7. Run the suites and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, look at the screen in a browser on the dev data, then push and open the PR.

**PR 3b — Entry forms**

**17. Entry form** - Add the inline Add entry and Edit entry form with its fields, defaults, validation, saving, server-error and warning states, keeping the form open after Add and on an Edit that returned a warning. See spec §3 "Entry form", "Entry validation" and "After a save".

**18. Screen wiring** - Add the Add entry action, open the form from it and from an entry row's Edit, include it in the unsaved-changes prompt, and show its toasts.

**19. PR 3b verification** - Run the suites and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, look at the forms in a browser on the dev data, then push and open the PR.

### Stage 4 — Settle

**20. Settle form** - Add the Settle mode of the entry form with its pre-fill from the current balances, re-fill on a currency change and the implied rate. See spec §3 "Settle form" and "Implied rate".

**21. Confirmation** - Confirm a settlement before saving, saying that both totals become zero or what remains and who owes it, then save, close and refresh the header. See spec §3 "Confirmation".

**22. Disabled action** - Show an action the API returns as disabled with its reason in the action bar, so Settle reads "Nothing to settle." on a settled account. See spec §3 "Disabled reason to the web".

**23. Stage 4 verification and ticks** - Add screen checklist row S8. Run the suites and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, and settle on a copy in a browser. Tick the F12 acceptance criteria in the PRD in their own commit, each naming its test, with the cross-feature boxes spec §7 lists, then push and open the PR.
