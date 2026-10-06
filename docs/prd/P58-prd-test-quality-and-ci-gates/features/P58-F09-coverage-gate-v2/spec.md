# Technical Specification: Coverage Gate v2

**Complexity:** medium (CI-only: one composite action, three small scripts with self-tests, workflow wiring, one config exclusion, one committed baseline file; no application code)

## 1. Technical Overview

**What.** Turn the three job-wide line floors into a ratchet that also measures branch coverage, and add the changed-code signal the PRD asks for:

1. `coverage-baseline.json` at the repo root holds per-job line and branch coverage (`backend`, `wpf`, `web`).
2. `.github/actions/coverage-gate` fails when line **or** branch coverage drops more than 0.5 points below the baseline, and fails closed when the baseline or the branch figure is missing. The 90% line floor stays as a tripwire.
3. Web coverage stops counting `src/test/**` and `src/test-utils/**`.
4. The sticky PR comment shows, per job: Line %, Branch %, delta against the baseline, Diff line %, Diff branch % and a verdict.
5. Diff coverage (changed lines and branches in the in-scope source directories) is computed from each job's merged Cobertura report against the PR's merge base, advisory first.
6. A PR that edits the coverage exclusions, or the baseline file, gets a warning line in the comment.

**Why.** Each job gates only line coverage at 90% with about 5 points of headroom, so a PR can delete branch handling or add an untested class and stay green. Branch coverage moved a lot during F06-F08 (last green `main` run: backend 90.4, wpf 82.9, web 87.7), so a ratchet taken now locks in real protection. Exclusions in `coverlet.runsettings` and `vite.config.ts` can grow in the same PR that adds the untested code they hide.

**Scope.** Core + Full (chosen by the user). PRD blocks: Consumes F06/F07/F08 (the baseline is taken from a suite they have already cleaned, trait-filtered and made deterministic); Provides the baseline and ratchet that F10's gains are captured by.

**Excluded:**
- Switching diff coverage to blocking. This feature ships advisory mode and one switch (D6); flipping it is a separate PR after the 2-4 week window and at least 20 PRs, as the PRD requires.
- Raising any threshold above today's numbers, F10's test additions, mutation scores (F14), changing what `coverlet.runsettings` excludes.

**Decisions (Auto-Accept, review and override):**

| # | Decision | Reason |
|---|---|---|
| D1 | Gate logic moves from the action's inline script into `.github/scripts/coverage-gate.ps1` (the action's inline copy is deleted, and the emoji bands live only in the script), with `coverage-gate.test.ps1` as its self-test, run in the `changes` job after the existing steps | The inline script cannot be tested; the repo's pattern for CI logic is a script plus a self-test run in `changes`. pwsh because the action already runs pwsh and the dev machine has pwsh, not Python. A few seconds in `changes` against minutes of Windows build time |
| D2 | Baseline values are `summary.linecoverage` and `summary.branchcoverage` from each job's reportgenerator `Summary.json` (one decimal, verified), the same field the gate reads | The 0.5-point tolerance is 10x the rounding step. `coverage-baseline.ps1` writes the file from the three summaries, so a bump is one command and never hand-typed |
| D3 | The baseline is taken from a `main` run **after** the web exclusion merged, read from that run's `coverage-report-*` artifacts; one rerun of that commit is compared before the file is committed (requested from the user, as a rerun is outward-facing) | PRD cross-feature criterion; the exclusion changes the web figures |
| D4 | The ratchet lives inside the three jobs' gate step, so a job skipped by path classification skips its ratchet and `ci-status` is unaffected | Matches today's skip semantics |
| D5 | The gate writes `line-delta` and `branch-delta` as outputs before it can fail, and the comment job reads them from `needs.<job>.outputs` | One baseline read, no second delta code path |
| D6 | Diff coverage ships advisory. `DIFF_COVERAGE_BLOCKING` (workflow `env`, default `false`) is the switch; when `true`, a diff figure below threshold fails the comment job. A missing merge base prints `not computed` and never blocks | The later blocking PR is a one-line change that cites the false-positive rate |
| D7 | Stage 4 begins by checking whether `diff-cover` reports branch figures. If it does, use it; if not (my expectation, not verified), a custom `diff-coverage.ps1` parses `git diff -U0 --no-color <merge_base> <head>` (the same form `test-hygiene.sh` uses) and a merged Cobertura report | The PRD thresholds include diff-branch; avoid ~150 lines of parser if a tool already does it |
| D8 | Each .NET job's `reportgenerator` call and the web job's both get the `Cobertura` report type, under the existing assembly filters, so diff coverage has one parser and one artifact shape (`CoverageReport/Cobertura.xml`). The web lcov file is not needed | The web artifact only uploads `CoverageReport/`, and lcov lives outside it; backend alone produces 19 Cobertura files |
| D9 | The `changes` job exposes `merge_base` and `head` as outputs (it already resolves them); the comment job checks out the repository, downloads only the three report artifacts, and reads the baseline file itself for the warning | No second merge-base logic; artifacts uploaded with `if: always()` so a failed ratchet still shows its diff |
| D10 | Scope for diff coverage: changed files under `Financial.*.Domain`, `Financial.*.Application`, `Financial.Api`, `Financial.App/ViewModels`, `Financial.Web/src/{hooks,utils,components,pages}`; thresholds 80% line, 70% branch; no in-scope lines shows `n/a` | PRD |
| D11 | The exclusion warning lists patterns **added** to `<Exclude>` in `coverlet.runsettings` or `coverage.exclude` in `vite.config.ts` between merge base and head, and a separate line flags a changed `coverage-baseline.json`. Both are advisory | Removed exclusions only raise the bar |

## 2. Component Overview

| File Path | New/Modified | Purpose |
|---|---|---|
| `coverage-baseline.json` | New | Per-job `line` and `branch` baseline |
| `.github/scripts/coverage-baseline.ps1` | New | Writes the baseline from three `Summary.json` paths |
| `.github/scripts/coverage-gate.ps1` | New | Floor, ratchet, fail-closed; step summary and outputs |
| `.github/scripts/coverage-gate.test.ps1` | New | Self-test for the gate and the baseline writer |
| `.github/actions/coverage-gate/action.yml` | Modified | Calls the script; new inputs `baseline-key`, `baseline-path`; new outputs `line-delta`, `branch-delta` |
| `.github/scripts/diff-coverage.ps1` and `.test.ps1` | New (D7) | Changed-code coverage |
| `.github/scripts/coverage-exclusions.ps1` and `.test.ps1` | New | Added-exclusion listing |
| `.github/workflows/build.yml` | Modified | `merge_base`/`head` outputs, `Cobertura` report type, baseline inputs, self-test steps, rebuilt `coverage-comment` |
| `Financial.Web/vite.config.ts` | Modified (Stage 1, done) | Exclude the two test-support folders |
| `CLAUDE.md`, `docs/ci-affected-pipeline.md` | Modified | Ratchet, baseline rule, diff coverage window |

## 3. Interfaces

**`coverage-baseline.json`:** `{ "backend": {"line": 95.7, "branch": 90.4}, "wpf": {...}, "web": {...} }` (the shape; committed values come from Stage 2).

**Gate failure messages (exact, asserted by the self-test):**

| Condition | Message |
|---|---|
| Baseline missing or invalid | `coverage-baseline.json missing or invalid: <reason>` |
| Key missing | `coverage-baseline.json has no entry for '<key>'` |
| No branch figure in the report | `<label> report has no branch coverage; the ratchet fails closed` |
| Line / branch below baseline - 0.5 | `<label> line|branch coverage <x>% is more than 0.5 below the baseline <b>%` |
| Line below 90 | `<label> line coverage <x>% is below the 90% floor` (existing) |

**Sticky comment:**

| Project | Line | Branch | Δ vs baseline (line / branch) | Diff line | Diff branch | Verdict |
|:---|---:|---:|---:|---:|---:|:---|
| Backend (.NET) | 🟢 95.7% | 🟢 90.4% | +0.0 / +0.0 | 92.0% (23/25) | 75.0% (6/8) | ✅ |

Below the table when applicable: `⚠ coverage-baseline.json changed in this PR` and `⚠ Coverage exclusions added: <pattern>, ...`. A skipped job shows `⚪ not run`; a job without outputs shows `⚠️ unavailable` (the existing fallbacks); diff cells show `n/a` or `not computed`.

## 4. Testing Strategy

The logic under test is PowerShell, so the self-tests are PowerShell scripts run in `changes` (after the existing steps), plus real-run verification. Rows are table-driven where cases only differ by input.

**coverage-gate.test.ps1**

| Case | Assertions |
|---|---|
| Line and branch at baseline | Exit 0, deltas 0 |
| Table: (metric, delta) = line/branch × -0.4 (pass), -0.6 (fail) | Exit code and the matching message |
| Line below the 90 floor with a lower baseline | Exit 1, floor message |
| Improvement above baseline | Exit 0, positive deltas, outputs written |
| Table: baseline file missing / unparsable / key missing / no branch figure | Exit 1, the matching message |
| Baseline writer from three summaries | File content equals the summaries' figures |

**diff-coverage.test.ps1** (shape depends on D7; with a custom script):

| Case | Assertions |
|---|---|
| All changed lines covered / one uncovered | 100% / correct ratio and counts |
| `condition-coverage="50% (1/2)"` on a changed line | 1 of 2 branches |
| Out-of-scope file / docs-only diff | Ignored / `n/a` |
| Windows absolute paths in Cobertura | Mapped to repo-relative |
| Empty merge base | `not computed`, exit 0 even with blocking on |
| Blocking on, below threshold / advisory, below threshold | Exit 1 / exit 0 |
| Exactly 80% line and 70% branch | Pass |

**coverage-exclusions.test.ps1:** added coverlet pattern listed; removed pattern silent; added vite exclude listed; unchanged files silent; both files combined.

**Real-run verification (recorded in the PR bodies):**
- Stage 2: two runs on the baseline commit report identical figures (artifact comparison).
- Stage 2: the self-test rows for -0.4 and -0.6 stand in for a scratch-branch demonstration; one scratch-branch run confirms the wiring end to end.
- Stages 3-5: the sticky comment on the stage PRs shows the new columns and warnings (Stage 5's PR touches an exclusion file on purpose to show the ⚠ line).

**Acceptance mapping (PRD Section 9, F09):** baseline exists for all 3 jobs → committed file plus the Stage 2 comparison; -0.6 fails / -0.4 passes → the gate table rows; missing baseline fails closed → the gate table rows; web excludes the two folders → Stage 1 (done); every PR comment shows the five columns → the stage PR comments; diff coverage advisory with a separate blocking PR → the advisory-mode row and the `DIFF_COVERAGE_BLOCKING` default; exclusion ⚠ line → the exclusions rows. Cross-feature: baseline from a `main` run after F06-F08, two runs identical → Stage 2 comparison.

## 5. Error Handling

- Baseline missing or unparsable: the gate fails with `coverage-baseline.json missing or invalid`.
- Merge base unavailable (shallow clone): diff coverage reports `not computed` and does not block; the ratchet still runs.
- A job skipped by path classification: its ratchet and comment row are skipped, and `ci-status` treats it as passed.
