# Implementation Plan: Low-Value Test Removal and Consolidation

**Prerequisites:**
- .NET 10 SDK, `npm install` done in `Financial.Web`, an up-to-date `main`, `gh` authenticated
- Each stage below is one PR and is independent of the others; merge order does not matter, but each PR re-measures counts and coverage against the then-current `main`
- Before Stage 1, record the baseline counts and the last `main` coverage-comment values (backend, wpf, web)

### Stage 1: Pure Deletions and Dead Guards (PR1)

**1. Baseline** - Run the counting commands on `main` and note the exact starting numbers and coverage rows in the PR body.

**2. Controller null-body tests and dead guards** - Delete the null-body controller tests and the `request is null` branches they exercise, only where the body parameter is non-nullable and bound by `[ApiController]`.

**3. Duplicate and echo tests** - Delete the `ConvertBack_*Throws*` tests, the duplicate ID tests (keeping one per aggregate root), the `SyncState` member-count test and the reflection assertion in `OpenLotTrackerTests`.

**4. Web static tests** - Delete the static-presence web tests and the per-endpoint camelCase tests.

**5. Verification** - Record the after-counts, compare coverage with `main`, and list the retained covering test for each removed group in the PR body.

### Stage 2: Constructor Null-Guard Theory (PR2)

**6. Guard helper** - Add the shared reflection helper that enumerates public constructors, builds the other arguments, and asserts the `ArgumentNullException` and its parameter name, with an explicit allowlist.

**7. Per-assembly theories** - Add one constructor-guard theory per production assembly that has constructor tests today.

**8. Retire per-class tests** - Delete the per-class null-guard tests and `ControllerGuardClauseTests.cs`; add guards or allowlist entries for every unguarded parameter the theory finds.

**9. Revert checks** - Prove, for at least one constructor in each assembly, that deleting a guard fails the theory naming the type and parameter, then record the counts and coverage comparison in the PR body.

### Stage 3: Other .NET Parameterization (PR3)

**10. Collection and converter tests** - Collapse `CashFlowDataTests` into `IdCollection<T>` tests plus one wiring theory, and the reference-converter tests into one generic base with a derived class per converter.

**11. Migrator, triplets, pairs and echoes** - Fold the migrator scaffold tests, the finance-service triplets, the retry sync/async pairs and the getter echoes into theories and one `Create_AssignsAllFields` per entity.

**12. Verification** - Prove one broken behaviour per group fails exactly one case, and record counts, coverage and retained-test mapping in the PR body.

### Stage 4: Web Tables (PR4)

**13. Dialog contract tables** - Move the shared form-dialog contract into `describe.each` tables driven by a per-dialog configuration, keeping dialog-specific tests in their own files.

**14. List-tab tables** - Do the same for the repeated list-tab template tests.

**15. Verification** - Prove removing one behaviour from one dialog fails only that row, and record counts, web coverage and the retained-test mapping in the PR body.

### Stage 5: Final Measurement and Ticks (PR5)

**16. Totals** - Re-run the counting commands against `main` after Stages 1–4 and report the cumulative drop and the per-job coverage against the original baseline; if the drop is under 550, list further candidates instead of forcing the number.

**17. Housekeeping** - Ask for confirmation before deleting the seven untracked ghost test directories locally and note the outcome in the PR.

**18. PRD ticks** - Tick the satisfied F06 boxes in the PRD in a separate commit.
