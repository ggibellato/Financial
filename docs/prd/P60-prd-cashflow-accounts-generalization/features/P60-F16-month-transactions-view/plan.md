# Implementation Plan: Month Transactions View

**Prerequisites:**
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7. No new packages.
- The live file for parity is the 2026-11-05 download at `E:\dev\projetos\financial\data\data-cashflow.json` (SHA-256 starts `d83852d4`). Every PR runs `scripts/parity/Invoke-Parity.ps1 -Step All -LiveCopy "E:\dev\projetos\financial\data\data-cashflow.json"` against `p60-deployed-f15` and expects `Parity OK: 0 differences` with the SHA-256 unchanged. Parity reads that file and never writes it; local runs use `data/dev/data-cashflow.json`.
- Implement each PR on its own branch off an up-to-date `main`, starting stage 2 only after stage 1 is merged. Count the files before pushing: at most 8 non-test code files and about 15 in total (generated `openapi.ts` excluded). Propose a split instead of pushing anything larger.
- Strict no-comments policy in all code, XML docs included.
- Deploy only after stage 2.

### Stage 1 — Month rows route and parity figure

**1. Shared row builder** - Fold the account screen's bank and card row building into one private builder that works from the month's records read once, and switch the existing per-account transactions read to it with no change in what it returns. See spec §3 "Shared row builder".

**2. Month view** - Add the read that lists the month's rows of every account, active or inactive, each with its account's id, name, currency and active flag, leaving the bank-side copy of old settled card charges out and ordering newest first. See spec §3 "Row shape", "Old settled card charges", "Which accounts" and "Row order".

**3. Month rows route** - Expose the month view on the accounts controller at the route spec §5 defines, with its 400 for an invalid month.

**4. Parity figure family** - Add the after-side family that reports the grid's spend per category for every month, including a zero for every category Category totals list that the grid lacks, map it to the existing category total, and document it in the parity README and the self-test's family list. See spec §3 "Parity family" and "Comparing a figure with no before side".

**5. Stage 1 verification** - Regenerate the snapshot and web types, run the suites, the parity self-test and the parity run, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 2 — All transactions tab and PRD ticks

**6. Client and loading** - Add the API client call and type alias for the month rows, and the hook that loads them only while the tab is open, reloads on a month change and offers retry. See spec §3 "Tab and loading".

**7. Clear filters** - Let the column filters hook clear every column's selection at once.

**8. Totals** - Add the helper that sums a set of rows per currency into a Total and a Spend, exactly to the penny. See spec §3 "Footer".

**9. All transactions grid** - Add the grid with its columns, sorting, Category, Account and Type filters, Clear filters, footer, Inactive marker, account links, an Actions column (Edit where the row lives, Delete in place), and its loading, empty month, no match and load error states, stacking at narrow widths. See spec §3 "Columns", "Filters", "Footer", "Clear filters", "Links", "States" and "Narrow layout and keyboard".

**10. Monthly tab** - Add "All transactions" as Monthly's second tab, using Monthly's month picker and showing even while Monthly's own load is pending or failed.

**11. Stage 2 verification and ticks** - Add the screen checklist rows T1–T3. Run the suites, the parity self-test and the parity run, `/simplify`, the architecture-reviewer and the ui-reviewer, and look at the tab in a browser on the dev data. Tick the F16 acceptance criteria in the PRD in their own commit, each naming its test (cross-feature boxes only when every feature they name has shipped), then push and open the PR.
