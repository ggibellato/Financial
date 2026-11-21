# Implementation Plan: Card-Paid Pot Withdrawal

**Prerequisites:**
- P62 PRD merged; branch off an up-to-date `main` (`git merge --ff-only origin/main` first).
- .NET 10 SDK, Node as for `Financial.Web`, PowerShell 7. No new packages.
- One PR for the whole feature (about 8 non-test files including the generated types); count files before pushing and propose a split if it grows past the limit.
- Strict no-comments policy in all code.
- No live data file is touched; dev data under `data/dev/` only.

### Stage 1: Domain

**1. Card payment value** - Add the small record that carries the card and its statement month into the withdrawal rule. See spec §3 "Domain input".

**2. Card branch of the withdrawal rule** - Teach the withdrawal rule to write the pot movement and an unpaid card charge on the statement month under one operation id, rejecting a statement before the expense month. See spec §4 "Rules in detail".

**3. Operation read-out** - Let an operation report its card, statement month and investment withdrawal status, and update description and category in place. See spec §3 "Investment withdrawal status" and "Edit after the statement is paid".

### Stage 2: Application and API

**4. Request and response fields** - Add the card and statement fields to the withdrawal request and the operation response. See spec §5.

**5. Payment validation** - Resolve none, bank or card from the request with the statement default and the paid-statement check, in the order the spec lists. See spec §4 "Payment resolution".

**6. Service flows** - Wire card create, unlocked rebuild, locked in-place edit and the delete lock into the Pots service, with statement creation and calendar sync around the save. See spec §4 "Rules in detail" and §3 "Calendar sync".

**7. Contract regeneration** - Regenerate the OpenAPI snapshot and the web's generated API types and confirm the web still builds with today's Withdraw form unchanged. See spec §2.

**8. Docs** - Describe card-paid pot withdrawals and the pending investment withdrawal in `context.md`.

**9. Verification and ticks** - Run the backend and web suites, `/simplify` and the architecture reviewer, tick the F01 criteria in the PRD in their own commit, then push and open the PR.
