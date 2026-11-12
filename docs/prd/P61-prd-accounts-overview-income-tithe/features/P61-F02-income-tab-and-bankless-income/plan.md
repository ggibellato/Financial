# Implementation Plan: Income Tab and Bankless Income

**Prerequisites:**
- F01 merged (Accounts tabs and the address hook).
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7. No new packages.
- Parity: the 2026-11-10 live download at `E:\dev\projetos\financial\data\data-cashflow.json` against `p60-deployed-f13`; read only.
- One branch per PR off an up-to-date `main`; start 2b only after 2a is merged. Count files before pushing; propose a split instead of pushing more than the limit.
- Strict no-comments policy in all code.
- Deploy after 2b.

### Stage 2a — Bankless incomes in the month view

**1. Optional account fields** - Make a month row's account fields and a transaction row's currency optional. See spec §3 "Optional account fields" and §5.

**2. Bankless rows** - Add one month-view row per income of the month with no bank, ordered with the others. See spec §3 "Bankless rows".

**3. API types** - Regenerate the OpenAPI snapshot and the web's generated types, and keep the account screen grid compiling with no visible change. See spec §3 "Account screen currency".

**4. All transactions "No bank"** - Show bankless rows as "No bank" with their filter value, plain-text Description (no link) and their own footer total. See spec §3 "Grid "No bank"" and "Footer".

**5. Parity self-test** - Pin that a bankless row changes no parity figure. See spec §3 "Parity".

**6. Stage 2a verification** - Run the suites, the parity self-test and the parity run, `/simplify` and the reviewers, then push and open the PR.

### Stage 2b — Income tab

**7. No-bank default** - Add the form setting that starts a new income on "No bank". See spec §3 "No-bank default".

**8. Income tab data** - Add the hook that loads the month's incomes, the income sources and the accounts that receive income, and wires the form, delete and refresh, including the stale-row path. See spec §3 "Income tab data" and "404".

**9. Income tab** - Add the tab's grid, filters, footer, New income, edit and delete with every state, and register it third on the Accounts page. See spec §3 "Grid", "Form and delete" and "Tab".

**10. Docs** - Describe the Income tab and bankless income in `context.md` and align the PRD's new-income date wording.

**11. Stage 2b verification and ticks** - Run the suites and the parity run, `/simplify` and the reviewers, look at the tab in a browser on the dev data, tick the F02 criteria in the PRD in their own commit, then push and open the PR.
