# Implementation Plan: Cross-Front-End Correctness Fixes

**Prerequisites:**
- .NET 10 SDK, `npm install` done in `Financial.Web`, an up-to-date `main`
- Each stage below is one PR; they are independent and may merge in either order

### Stage 1: Single Round-Up Rule (PR1)

**1. Domain rule** - Add the static round-up function to `Expense` (2 dp, away from zero, non-positive gives 0) and make `RoundUpSuggestion` use it. Add the committed literal case table.

**2. WPF adoption** - Have the expense workflow view model call the Domain function and format the suggestion at a fixed 2 dp.

**3. Web mirror** - Export the existing TypeScript computation and keep it as the mirror of the Domain rule.

**4. Tests** - Drive the Domain theory and a vitest table test from the shared JSON, collapse each front end's value-table tests into one interaction test asserting `0.60`, and delete the replaced tests.

### Stage 2: Local Date, Config Guard and Ticks (PR2)

**5. Local date** - Make `todayIsoDate()` return the local calendar date, with tests at the BST-midnight, month-end and month-start boundaries.

**6. Pinned-date test rewrites** - Replace the recomputed `toISOString().slice(0, 10)` expectations with pinned times and literal dates.

**7. API base URL guard** - Make the config module throw on an unset or empty value and replace the test that pinned the empty string.

**8. Revert checks and PRD ticks** - Prove each new test fails on a deliberate revert and record it in the PR body, then tick the satisfied F04 boxes in the PRD in a separate commit, leaving the Full Scope box unticked.
