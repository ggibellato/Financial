# Implementation Plan: Settlement Template and Mãe Migration

**Prerequisites:**
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7. No new packages.
- The live file for parity is the 2026-11-05 download at `E:\dev\projetos\financial\data\data-cashflow.json` (V4, SHA-256 starts `d83852d4`). Every PR runs `scripts/parity/Invoke-Parity.ps1 -Step All -LiveCopy "E:\dev\projetos\financial\data\data-cashflow.json"` against `p60-deployed-f16` and expects `Parity OK: 0 differences` with the SHA-256 unchanged. From 2b the run's own Migrate step turns its copy into V5 (spec §3 "Parity across the version change"); nothing is migrated by hand and the live file is never written.
- Local browser checks from 2b on use a migrated temp copy of `data/dev/data-cashflow.json`; `data/dev` and `data/data-cashflow.json` are not rewritten.
- Implement each PR on its own branch off an up-to-date `main`, starting only after the previous PR is merged. Count the files before pushing: at most 8 non-test code files and about 15 in total (generated `openapi.ts` excluded), except 2a (one rename repeated) and 2b (11, named in spec §3 "PR size"). Propose a split instead of pushing anything larger.
- Strict no-comments policy in all code, XML docs included.
- Deploy only after 3b, with `migrate` on the Drive download (spec §7 "Deploy").

### Stage 1a — Settlement template and account fields

**1. Settlement template** - Add the three capabilities and the Settlement template to the template table, after Pots and outside the default group. See spec §3 "Template" and "Currency message".

**2. Account currencies and other participant** - Give an account its list of currencies and the other participant's name, with their rules and resets on a template change. See spec §3 "Account currencies" and "Other participant".

**3. Account file fields** - Persist both fields only on the accounts that have them, so a V4 file without Settlement accounts saves byte-identical, and refuse a file whose fields don't match the template. See spec §3 "File fields".

**4. Stage 1a verification** - Run the suites, the architecture test, parity, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 1b — Admin → Accounts for Settlement

**5. Account DTOs and Admin rules** - Carry the currencies and the other participant on the account DTOs, and validate and apply them on create and edit, locking the currencies once the account has records. See spec §3 "Admin fields" and "Locked currencies", and §5 1b.

**6. Account dialog and Admin page** - Add the Currencies checkboxes and the Other participant field to the account dialog with every state, and carry both fields through the create, edit and deactivate requests and the admin list. See spec §3 "Dialog" and §7 "UI states".

**7. Stage 1b verification** - Regenerate the snapshot and web types, run the suites, parity, `/simplify`, the architecture-reviewer and the ui-reviewer, and look at the dialog in a browser on the dev data, then push and open the PR.

### Stage 2a — Rename the Mãe entry

**8. Settlement entry rename** - Rename the Mãe ledger entry type, its data collection and its repository members to Settlement entry names, keeping the file key and every behaviour, and delete the doc comments on the renamed lines. See spec §3 "Entry model".

**9. Stage 2a verification** - Run the suites, parity, `/simplify` and the architecture-reviewer, then push and open the PR, naming it a one-rename change.

### Stage 2b — V5: the Mãe Settlement account

**10. Settlement entry model** - Reshape the entry into an account-bound record with paid by, currency, a positive amount and an optional converted value, with its rules, and add the rule that finds Controle Mãe's account. See spec §3 "Entry model" and "Which account Controle Mãe serves".

**11. V5 file** - Persist the entries under their new collection with an account reference and raise the version guard to 5.

**12. Controle Mãe on the account** - Serve every `controle-mae` route from the Mãe account with the same shapes and values, setting paid by from the sign and refusing missing, zero or opposite-signed values on the values edit. See spec §3 "`controle-mae` on the account" and §5 2b.

**13. V4 → V5 step** - Add the step that creates the Mãe Settlement account, moves the entries into it, records the five pound entries as paid in GBP and refuses to write anything if a total would change, and register it after the V3 → V4 step. See spec §6.

**14. Import and data** - Make the spreadsheet import find or create the Mãe account and write its entries on it, and move the test seeds, test data and example data to V5 and the newer-version refusal tests to 6. See spec §3 "Import" and §4 "Parity, data and docs".

**15. Stage 2b verification** - Run the suites, the fresh-import test, a `migrate` of a temp copy of the live file (checking its summary against spec §6), parity, `/simplify` and the architecture-reviewer, then push and open the PR, naming the stage's size overage.

### Stage 2c — Shared per-currency balance

**16. Per-currency balance** - Let every record contribute per currency, with Settlement entries contributing in both of their account's currencies, and add the per-currency balance to the shared rule. See spec §3 "Per-currency balance".

**17. Readers and Controle Mãe totals** - Feed the entries to the balance reader without adding any transaction row, compute Controle Mãe's totals through the shared rule, list inactive accounts by any currency, and count entries as records that keep an account in use. See spec §3 "Entries in the balance reader, not in the rows", "Membership per currency" and "In use".

**18. Account screen picker** - Leave accounts with participants out of the account screen's picker until F12. See spec §3 "Picker".

**19. Stage 2c verification** - Run the suites, parity, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 3a — Group balances per currency and parity

**20. Group members per currency** - Give each Accounts page group member its balance per currency and its other participant, and return no totals for a Settlement group. See spec §3 "Group members" and §5 3a.

**21. Parity settlement family** - Add the per-month Settlement balance family on both sides, read from the Settlement group when the build has one and from the Controle Mãe entries otherwise, include the new entry collection in the month range, and document it in the parity README and the self-test's family list. See spec §3 "Parity".

**22. Stage 3a verification** - Regenerate the snapshot and web types, run the suites, the parity self-test and the parity run, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 3b — Settlement grid and PRD ticks

**23. Settlement balance text** - Add the helper that turns a Settlement account's balances into "Mãe owes you R$167.88 / £7.94", "You owe Mãe …" or "Settled". See spec §3 "Settlement grid".

**24. Settlement grid** - Add the Settlement grid to the Accounts page after Pots, with plain-text rows, the direction text and no totals. See spec §3 "Settlement grid" and §7 "UI states".

**25. Stage 3b verification and ticks** - Add the screen checklist rows. Run the suites, the parity self-test and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, and look at the Accounts page in a browser on the migrated dev copy. Tick the F11 acceptance criteria in the PRD in their own commit, each naming its test, and the cross-feature boxes spec §7 marks to tick, then push and open the PR.
