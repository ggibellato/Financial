# Implementation Plan: Nightly Quality Pipeline

**Prerequisites:**
- An up-to-date `main` and `gh` authenticated with permission to dispatch workflows and read run results
- Stages merge in order: 2 reuses the score script and the workflow from 1, 3 reuses the composite action, 4 needs every job to exist
- PowerShell 7 (`pwsh`) for the scripts, as in the F09 stage scripts; write script files with the Write tool, not shell heredocs
- No change to product code or to any test project; the nightly runs the suites exactly as they are
- `CLAUDE.md` may carry uncommitted edits unrelated to this feature; stage only this feature's hunk, or leave the pointer to stage 4 after that edit is committed
- Each stage proves its jobs with a `workflow_dispatch` run on its own branch before the PR is marked ready, and records the run link in the PR body

### Stage 1: Workflow Skeleton and Stryker.NET (PR1)

**1. Workflow skeleton** - Create `nightly.yml` with the cron and manual triggers, least-privilege permissions, the shared environment (`TZ`, `LANG`) and the `stryker-dotnet` job; document that it is not part of `ci-status`.

**2. Stryker.NET tooling** - Pin `dotnet-stryker` in a local tool manifest and add the shared config with the report formats, a zero break threshold and the filter that keeps `Live` and `E2E` tests out of the mutation run.

**3. Mutation score script** - Add `mutation-score.ps1` and its self-test: one score definition for both tools, a summary row, a step output and a fail-closed path for a missing report.

**4. Domain matrix job** - Run Stryker.NET per Domain project, publish the HTML and JSON reports as artifacts and the score in the step summary; wire the new self-test into the `changes` job of the PR pipeline and ignore the output folders.

### Stage 2: StrykerJS (PR2)

**5. StrykerJS tooling** - Add the dev dependencies, the `test:mutation` script and `stryker.config.json` with the vitest runner, the TypeScript checker, the `mutate` globs for `src/utils` and `src/hooks`, the exclusions and a zero break threshold.

**6. Nightly job with incremental cache** - Add the `stryker-js` job that restores and saves the incremental file with `actions/cache`, runs the mutation, uploads the report and feeds the same score script; ignore the tool's output folders.

**7. Cache proof** - Dispatch the workflow twice on the branch with no source change and record both durations in the PR; confirm the report's file list holds no `.tsx`, generated or test-helper paths.

### Stage 3: Full E2E and Live Checks (PR3)

**8. Shared web E2E action** - Extract the build, publish, seed, start and health-wait steps of the `web-e2e` job into `.github/actions/web-e2e`, taking the command to run as an input, and switch the PR job to it with `npm run smoke-test`; the PR run of this stage proves the refactor changed nothing.

**9. Full browser job** - Add `e2e-web` calling the action with `npm run test:e2e`, uploading the same artifacts on failure as the PR job.

**10. Full WPF job** - Add `e2e-wpf` on `windows-latest` building the app and the E2E project and running the whole `Category=E2E` set, with the PR job's artifact upload.

**11. Live job and result summary** - Add `test-results-summary.ps1` with its self-test and the `live` job running `Category=Live` with `continue-on-error`, listing failures in the summary; confirm the selected tests equal F07's tagged set.

### Stage 4: Traceability, Failure Issue, Documentation, PRD Ticks (PR4)

**12. Traceability script** - Add `ac-traceability.ps1` and its self-test: id-carrying checkboxes against `Trait("AC", …)` and `[AC …]` test titles, a per-PRD list of untraced ids, one summary line for each PRD without ids, and orphan tags.

**13. Traceability job** - Add `ac-traceability` to the workflow, publishing the report in the step summary as advisory.

**14. Failure issue** - Add `nightly-issue.ps1` with its self-test and the `report` job that lists the failed blocking-class jobs and opens or updates the single `nightly-failure` issue; the `live` job is excluded.

**15. Documentation** - Add the "Nightly pipeline" section to `docs/ci-affected-pipeline.md` (jobs, schedule, artifacts, reading the report, the issue rule, the break-threshold rule: none until three runs, then the lowest score minus five, set by a PR per tool) and the one-line pointer in `CLAUDE.md`.

**16. End-to-end drill** - Dispatch a full run, then a run with a forced failure on the branch to prove the issue is created and then commented, and delete the drill's issue; record the run links in the PR.

**17. PRD acceptance criteria** - In a separate commit, tick the F14 boxes and the cross-feature boxes this feature satisfies; leave unticked any criterion not demonstrated on a green dispatch run.
