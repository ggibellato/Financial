# Implementation Plan: Pots Template and Reserva Migration

**Prerequisites:**
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7. No new packages.
- The user downloads the live file to `data/data-cashflow.json` before each stage's parity run (`scripts/parity/Invoke-Parity.ps1 -Step All`, baseline `p60-deployed-f08`). Parity reads that file and never writes it. Local runs use `data/dev/data-cashflow.json`.
- Implement each stage on its own branch off an up-to-date `main`, starting only after the previous stage's PR is merged. Count the files before pushing: at most 8 non-test code files and about 15 in total (generated `openapi.ts` excluded), except stages 2 and 8 as spec §3 names. Propose a split instead of pushing anything larger.
- Strict no-comments policy in all code, XML docs included.

### Stage 1: Pots template

**1. Template** - Add the three capabilities and the Pots template to the template table, so Admin → Accounts can create an empty Pots account. See spec §3 "Template".

**2. Stage 1 verification** - Run the suites, the architecture test, parity, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 2: V4 — Reserva and its pots

**3. Pots in the account** - Let a Pots account hold its pots, add the Reserva rule, and make bucket creation add to Reserva. Remove the separate bucket collection. The Reserva rule is removed in F13 together with the `reserve` endpoints.

**4. V4 file** - Write pots inside the account, load bucket references from every Pots account, and raise the version guard to 4.

**5. V3→V4 step (pots)** - Add the step that creates Reserva from the buckets, as spec §6 rule 1 describes (including the refusal of a V3 file that already holds a Pots account), and register it after the V2→V3 step.

**6. Import and data** - Seed the Reserva account and its pots in the import, stop carrying buckets over separately, and move the seed, test and example data to V4.

**7. Stage 2 verification** - Run the suites, the fresh-import test, parity, `/simplify` and the architecture-reviewer, then push and open the PR, naming the stage's size overage.

### Stage 3: Shared balance

**8. Movements in the shared rule** - Make movements count toward their pot's account, add the per-pot sum, and feed movements to every balance.

**9. Readers** - Switch the reserve balances, the overdraft check and the snapshot total to the rule, delete the old sums, and count an account that owns pots as in use.

**10. Stage 3 verification** - Run the suites, parity, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 4: Category setting (server)

**11. Category on the account** - Add the category setting with its file field and the step's category part, set it in the import, and use it for the withdrawal's bank expenses. Delete the "Reserva" name lookup.

**12. Category delete guard** - Count a category set on an account as a reference when deleting categories and in their in-use flag.

**13. Stage 4 verification** - Run the suites, parity, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 5: Category setting (web)

**14. Account DTOs and Admin** - Carry the category and the capabilities on accounts, require a category for Pots accounts, and add the category select to the account dialog.

**15. Withdrawal categories** - Exclude Pots accounts' categories by id on the Reserva page, and delete the web name constant.

**16. Stage 5 verification** - Regenerate the snapshot and web types, run the suites, parity, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 6: Snapshot source (server)

**17. Named Pots account** - Rename the source and give the investment account its Pots account, with its rules, file field, step part and in-use count. Keep today's wire value mapped to Reserva.

**18. Suggestion** - Compute the suggestion from the named account through the rule, with the new source text.

**19. Stage 6 verification** - Run the suites, `/simplify` and the architecture-reviewer. Run parity, and list the intended snapshot source text differences in the PR, then push and open it.

### Stage 7: Snapshot source (web)

**20. DTOs and Admin investment accounts** - Switch the investment account DTOs to the new source value and the Pots account id, and add the Pots account select and list text.

**21. Stage 7 verification** - Regenerate the snapshot and web types, run the suites, parity, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 8: Income split target (server)

**22. Split targets** - Replace the income source and income booleans with Pots accounts, with their rules, file fields, step part, in-use counts and the import seed. Keep today's DTO booleans mapped as spec §3 says.

**23. Split into the chosen account** - Build and rebuild an income's split movements in the Pots account it names.

**24. Stage 8 verification** - Run the suites, the fresh-import test, parity, `/simplify` and the architecture-reviewer, then push and open the PR, naming the stage's size overage.

### Stage 9: Income sources (web)

**25. Income source DTOs and Admin** - Switch the income source DTOs to the Pots account id, and replace the auto-split switch with a Pots account select and list column.

**26. Stage 9 verification** - Regenerate the snapshot and web types, run the suites, parity, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 10: Income form (web)

**27. Income DTOs and form** - Switch the income DTOs to the Pots account id, and give the income form the split checkbox and Pots account picker with every state, on Monthly and the account screen.

**28. Stage 10 verification** - Regenerate the snapshot and web types, run the suites, parity, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 11: Other accounts and PRD ticks

**29. Other accounts** - Add the other-accounts endpoint and the Accounts page section with its states, and the parity Pots total figure with its mapping and self-test case.

**30. Stage 11 verification and ticks** - Regenerate the snapshot and web types, run the suites, parity, `/simplify`, the architecture-reviewer and the ui-reviewer. Tick the F09 acceptance criteria in the PRD in their own commit (cross-feature boxes only when every feature they name has shipped), then push and open the PR.
