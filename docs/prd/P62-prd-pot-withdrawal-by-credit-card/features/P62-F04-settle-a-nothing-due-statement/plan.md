# Implementation Plan: Settle a Nothing-Due Statement

**Prerequisites:**
- P62 F01–F03 merged; branch off an up-to-date `main` (`git merge --ff-only origin/main` first); start 4b only after 4a is merged.
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7. No new packages.
- Count files before pushing; propose a split instead of pushing past the size target.
- Strict no-comments policy (the controller's `///` docs stay: they feed OpenAPI).
- Browser checks on a dev data copy only; never the live file or port 8080.

### Stage 4a: Settle and unsettle (backend)

**1. Settled marker** - Give a card statement an optional settled date, a closed state and the settle/unsettle guards, and keep the date out of the data file when empty. See spec §3 "How settled is stored" and §6.

**2. Paid rule includes settled** - Make the shared paid-statement rule treat a settled statement as paid. See spec §3 "Paid-statement rule".

**3. Settle and unsettle use cases** - Add the two service operations with their checks, writing or removing the investment withdrawals in one save, and refuse mark paid on a settled statement. See spec §4 "Rules in detail".

**4. Routes and reads** - Expose the two routes and report the settled date on the statement reads, then regenerate the OpenAPI snapshot and web types. See spec §5.

**5. Stage 4a verification** - Run the suites, `/simplify` and the architecture reviewer, then push and open the PR.

### Stage 4b: Statements tab (web)

**6. Client and state** - Add the client calls and the settle form and unsettle confirmation state beside pay and unmark. See spec §4 "Frontend".

**7. Settle form and row actions** - Add the settle form and the row action, status and Unmark settled on the card Statements tab, with every state. See spec §4.

**8. Docs** - Describe settling a nothing-due statement in `context.md`.

**9. Stage 4b verification and ticks** - Run the suites, `/simplify`, the architecture and UI reviewers and the review checklist, look at it in a browser on dev data, tick the F04 criteria and cross-feature 04 in the PRD in their own commit, then push and open the PR.
