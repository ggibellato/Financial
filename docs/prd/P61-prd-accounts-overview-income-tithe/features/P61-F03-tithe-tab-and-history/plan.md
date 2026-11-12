# Implementation Plan: Tithe Tab and History

**Prerequisites:**
- F01 and F02 merged.
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7, Playwright as already installed for `web-e2e`. No new packages.
- Parity: the 2026-11-10 live download at `E:\dev\projetos\financial\data\data-cashflow.json` against `p60-deployed-f13`; read only.
- One branch per PR off an up-to-date `main`; start 3b only after 3a is merged. Count files before pushing; propose a split instead of pushing more than the limit.
- Strict no-comments policy in all code.
- Deploy after 3b.

### Stage 3a — Tithe year route

**1. Month figures** - Return the income base and the tithe paid with the month's tithe, from the same calculation. See spec §3 "Month figures".

**2. Year query** - Add the service method and route that return the 12 months of a year, each equal to the month result. See spec §3 "Year route" and §5.

**3. Client and types** - Regenerate the OpenAPI snapshot and web types, and add the web client call and type alias. See spec §3 "Client".

**4. Stage 3a verification** - Run the suites and the parity run, `/simplify` and the architecture-reviewer, then push and open the PR.

### Stage 3b — Tithe tab

**5. Tithe data** - Add the hook that loads the month detail and the year history separately and updates the carry-forward decision with its messages. See spec §3 "Tab data".

**6. Tithe tab** - Add the tab with the detail, the checkbox and the history table with every state, and register it fourth on the Accounts page. See spec §3 "Detail", "History" and "Tab".

**7. Summary trimmed** - Remove the tithe lines and the checkbox from the Summary data, tab and income grid. See spec §3 "Summary trimmed".

**8. E2E journey** - Add the test seed's income source and the smoke journey that adds a bankless income and checks the Tithe tab. See spec §3 "E2E".

**9. Docs** - Describe the Tithe tab in `context.md`.

**10. Stage 3b verification and ticks** - Run the suites, the E2E job and the parity run, `/simplify` and the reviewers, look at the tab in a browser on the dev data, tick the F03 and Cross-Feature criteria in the PRD in their own commit, then push and open the PR.
