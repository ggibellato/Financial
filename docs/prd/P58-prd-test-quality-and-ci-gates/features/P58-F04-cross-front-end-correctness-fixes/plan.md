# Implementation Plan: Cross-Front-End Correctness Fixes

**Prerequisites:**
- .NET 10 SDK, `npm install` done in `Financial.Web`, an up-to-date `main`
- Each stage below is one PR; stages 1 and 2 are independent and may merge in either order. Stages 3 to 5 are the Full Scope and merge in order (3 first: 4 and 5 call the new endpoint)

### Stage 1: Single Round-Up Rule (PR1)

**1. Domain rule** - Add the static round-up function to `Expense` (2 dp, away from zero, non-positive gives 0) and make `RoundUpSuggestion` use it. Add the committed literal case table.

**2. WPF adoption** - Have the expense workflow view model call the Domain function and format the suggestion at a fixed 2 dp.

**3. Web mirror** - Export the existing TypeScript computation and keep it as the mirror of the Domain rule.

**4. Tests** - Drive the Domain theory and a vitest table test from the shared JSON, collapse each front end's value-table tests into one interaction test asserting `0.60`, and delete the replaced tests.

### Stage 2: Local Date, Config Guard and Ticks (PR2)

**5. Local date** - Make `todayIsoDate()` return the local calendar date, with tests at the BST-midnight, month-end and month-start boundaries.

**6. Pinned-date test rewrites** - Replace the recomputed `toISOString().slice(0, 10)` expectations with pinned times and literal dates.

**7. API base URL guard** - Make the config module throw on an unset or empty value and replace the test that pinned the empty string.

**8. Revert checks and PRD ticks** - Prove each new test fails on a deliberate revert and record it in the PR body, then tick the satisfied Core F04 boxes in the PRD in a separate commit, leaving the Full Scope box unticked until stage 5.

### Stage 3: Server Split Rule and Status Endpoint (PR3, Full Scope)

**9. Application rule** - Add `ReserveSplitRule` with the tolerance check and the single warning text, and make `ReserveBucketService` build its Create/Update `Warning` from it.

**10. Status service and endpoint** - Add `ReserveSplitStatusDTO`, `IReserveBucketService.GetSplitStatus()` and `GET /reserve-buckets/split-status`; regenerate the OpenAPI snapshot and the web types and review the diff.

**11. Import tool** - Make `ReserveBucketMigrationSummary` use `ReserveSplitRule.IsBalanced` and drop its copy of the tolerance.

**12. Tests** - Rule, service and controller tests as listed in the spec; the contract test pins the new path.

### Stage 4: React Reads the Server Status (PR4, Full Scope)

**13. API client** - Add `getReserveSplitStatus` to the client and its type alias.

**14. Hooks** - Have `useReserva` and `useReserveBuckets` load the status with the list and after each change, expose the server warning, and delete `utils/reserveBucketSplit.ts` with its tests.

### Stage 5: WPF Reads the Server Status and Ticks (PR5, Full Scope)

**15. View models** - Have `ReservaViewModel` and `ReserveBucketsViewModel` read the status from the service on refresh and after each change, replacing the computed property and the tolerance constants; keep the XAML bindings.

**16. Final checks and PRD tick** - Grep that the tolerance and message exist only in `ReserveSplitRule`, then tick the F04 Full Scope box in the PRD in a separate commit.
