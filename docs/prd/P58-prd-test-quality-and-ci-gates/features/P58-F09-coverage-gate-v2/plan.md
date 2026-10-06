# Implementation Plan: Coverage Gate v2

**Prerequisites:**
- An up-to-date `main`, `gh` authenticated, `pwsh` available locally
- F06, F07 and F08 are on `main` (they are)
- Stages merge in order; each is one PR that leaves `main` deployable. Stage 2 needs a `main` run that includes Stage 1
- CI is the only place the numbers can be produced; where a step needs a rerun of a `main` run, ask the user to trigger it
- A defect found while building a script is fixed in the stage that owns that script

### Stage 1: Web Helper Exclusion (PR1)

**1. Exclusion** - Exclude the web test-support folders from vitest coverage so the web number reflects product code, and note it in the coverage paragraph of CLAUDE.md. Includes the F09 spec and plan.

### Stage 2: Baseline and Ratchet (PR2)

**2. Baseline source** - After Stage 1 merges, read the three coverage summaries of the resulting `main` run, ask the user to rerun that run once, and confirm both runs report identical line and branch figures per job.

**3. Gate script** - Move the gate logic into a script that reads the summary and the baseline, applies the floor, the 0.5-point ratchet and the fail-closed rules, and writes the step summary and outputs; add its self-test and run it in the `changes` job.

**4. Baseline file and wiring** - Commit `coverage-baseline.json` with the confirmed figures, pass each job's key and the baseline path into the action, and expose the deltas as outputs.

**5. Documentation and proof** - Document the ratchet and the baseline-update rule in CLAUDE.md and `docs/ci-affected-pipeline.md`, and record a scratch-branch demonstration that a drop above 0.5 fails and a drop below 0.5 passes.

### Stage 3: Comment Extension (PR3)

**6. Merged reports as artifacts** - Add the merged Cobertura report to the backend and wpf report outputs so the artifacts carry what diff coverage needs, and confirm the web artifact carries its lcov file.

**7. Comment job** - Rebuild the `coverage-comment` job to check out the repository, download the three artifacts, and lay out Line, Branch, delta against the baseline and verdict per project, plus the warning when `coverage-baseline.json` changed in the PR.

### Stage 4: Diff Coverage, Advisory (PR4)

**8. Diff coverage script** - Add the script that maps the PR's changed lines and branches onto the Cobertura and lcov reports for the in-scope directories and reports the figures against the 80% and 70% thresholds, with its self-test run in the `changes` job.

**9. Comment columns and the switch** - Add the Diff line and Diff branch columns to the comment, make the blocking switch an environment value defaulting to off, and document the observation window and the criteria for the later blocking PR.

### Stage 5: Exclusion Guard, Docs and Ticks (PR5)

**10. Exclusion guard** - Add the script that lists exclusion patterns added by the PR in `coverlet.runsettings` and the vite config, with its self-test, and show the warning line in the comment.

**11. Documentation** - Finish the pipeline document and CLAUDE.md for diff coverage and both warnings.

**12. PRD ticks** - Tick the satisfied F09 boxes and the F09 cross-feature box in their own commit.
