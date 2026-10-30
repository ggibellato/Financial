# Implementation Plan: Accounts Page by Template and Card Totals

**Prerequisites:**
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7. No new packages.
- The user downloads the live file to `data/data-cashflow.json` before each stage's parity run (`scripts/parity/Invoke-Parity.ps1 -Step All`, baseline `p60-deployed-f09`). Parity reads that file and never writes it. Local runs use `data/dev/data-cashflow.json`.
- Implement each stage on its own branch off an up-to-date `main`, starting only after the previous stage's PR is merged. Count the files before pushing: at most 8 non-test code files and about 15 in total (generated `openapi.ts` excluded). Propose a split instead of pushing anything larger.
- Strict no-comments policy in all code, XML docs included.

### Stage 1: Total owed on the card screen

**1. Total owed rule** - Add the card's total owed to the card charge rule and make the statements' accumulated outstanding use it. See spec §3 "Total owed".

**2. Account detail and header** - Return the total owed on a card's account detail and show it in the header as "Total owed now" next to the month-end owed balance. See spec §3 "Card header".

**3. Parity** - Emit each active card's total owed in every month and map it to the accumulated outstanding figure.

**4. Stage 1 verification** - Regenerate the snapshot and web types, run the suites, parity, `/simplify`, the architecture-reviewer and the ui-reviewer, then push and open the PR.

### Stage 2: Grouped endpoint

**5. Listing for every template** - Apply the default group's membership rule to every template, and expose each account's template order. See spec §3 "Membership" and "Group order".

**6. Groups endpoint** - Add the grouped read with its DTOs, member active flag and total owed, and per-currency totals, behind `GET accounts/groups/by-template/{year}/{month}`. See spec §5.

**7. Stage 2 verification** - Regenerate the snapshot and web types, run the suites, the architecture test, parity, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 3: Next statement

**8. Next statement rule** - Add the rule that finds a card's oldest statement month with an unpaid charge, and return it on card members. See spec §3 "Next statement".

**9. Parity** - Emit the grouped endpoint's figures that have a baseline equivalent and map them. See spec §3 "Parity".

**10. Stage 3 verification** - Regenerate the snapshot and web types, run the suites, parity, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 4: Accounts page

**11. Data hook and client** - Add the client method and a hook that loads the groups in one request, replacing the three-request hook.

**12. Grids per template** - Render one grid per group with the template's heading, columns, footer and link rule, keeping the page's loading, error and empty states. See spec §3 "Web grids", "Card columns" and "Page states".

**13. Stage 4 verification** - Run the web suites and build, parity, `/simplify`, the architecture-reviewer and the ui-reviewer with `docs/ui/review-checklist.md`, then push and open the PR.

### Stage 5: Retire Other accounts and tick the PRD

**14. Remove the Other accounts read** - Delete the endpoint, its service method, its listing filter, its client method and its parity figure, and move its acceptance tests onto the Pots group.

**15. PRD ticks** - Tick the five F14 boxes in their own commit, each naming its test.

**16. Stage 5 verification** - Regenerate the snapshot and web types, run the suites, parity, `/simplify` and the architecture-reviewer, then push and open the PR.
