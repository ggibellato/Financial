# Implementation Plan: Accounts Overview Tabs and Monthly Fold

**Prerequisites:**
- Node as for `Financial.Web`, .NET 10 SDK, PowerShell 7. No new packages.
- Parity: the 2026-11-10 live download at `E:\dev\projetos\financial\data\data-cashflow.json`, compared against `p60-deployed-f13`; parity reads the file and never writes it. Local runs use `data/dev/data-cashflow.json`.
- One branch per PR off an up-to-date `main`; start 1b only after 1a is merged. Count files before pushing (at most 8 non-test code files, about 15 in total); propose a split instead of pushing more.
- Strict no-comments policy in all code.
- Deploy after 1b.

### Stage 1a — Month picker on Accounts

**1. Address hook** - Add the hook that reads and writes the chosen month in the Accounts page address, falling back to the current month. See spec §3 "Address hook".

**2. Accounts page month** - Add the month picker to the Accounts page and drive the groups, heading and row links from the chosen month. See spec §3 "Month picker" and "Accounts tab".

**3. Shared styles and bank type** - Rename Monthly's stylesheet to the overview stylesheet used by both pages, and give the balance adjustment form its own bank balance shape. See spec §3 "CSS" and "Bank balance type".

**4. Stage 1a verification** - Run the web and backend suites, the parity run, `/simplify` and the reviewers, then push and open the PR.

### Stage 1b — Tabs, Summary and Monthly fold

**5. Tab support in the address** - Extend the address hook with the selected tab and its fallback. See spec §3 "Address hook".

**6. Summary data and Result** - Rename the Monthly data hook to the Summary hook without bank and card data, and add the Result helper. See spec §3 "Summary data" and "Result".

**7. Summary tab** - Add the Summary tab component with Category totals, Income by source and the Result line and its states. See spec §3 "Summary layout".

**8. Tabs on Accounts** - Add the tab list and render Accounts, Summary and All transactions, each only while selected and per month. See spec §3 "Tabs".

**9. Monthly retirement** - Remove the Monthly page, its menu item and the unused bank and card grids, and redirect the old address to the Summary tab. See spec §3 "Redirect".

**10. Docs** - Update `context.md` and the parity screen checklist for the Accounts tabs. See spec §3 "Parity screen checklist".

**11. Stage 1b verification and ticks** - Run the suites, the parity run, `/simplify` and the reviewers, look at the page in a browser on the dev data, tick the F01 criteria in the PRD in their own commit, then push and open the PR.
