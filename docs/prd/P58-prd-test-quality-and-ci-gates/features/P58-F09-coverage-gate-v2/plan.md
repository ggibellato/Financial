# Implementation Plan: Coverage Gate v2

**Prerequisites:**
- An up-to-date `main`, `gh` authenticated, `pwsh` available locally
- F06, F07 and F08 are on `main` (they are)
- Stages merge in order; each is one PR that leaves `main` deployable. Stage 2 needs a `main` run that includes Stage 1
- CI is the only place the numbers can be produced; where a step needs a rerun of a `main` run, ask the user to trigger it
- Docs go in the stage that introduces the behaviour

### Stage 1: Web Helper Exclusion (PR1)

**1. Exclusion** - Exclude the web test-support folders from vitest coverage so the web number reflects product code, and note it in the coverage paragraph of CLAUDE.md. Includes the F09 spec and plan.

### Stage 2: Baseline and Ratchet (PR2)

**2. Baseline source** - After Stage 1 merges, read the three coverage summaries of the resulting `main` run, ask the user to rerun that run once, and confirm both runs report identical line and branch figures per job.

**3. Gate script** - Move the gate logic out of the action into a script that reads the summary and the baseline, applies the floor, the 0.5-point ratchet and the fail-closed rules, and writes the step summary and outputs; delete the action's inline copy, add the self-test and run it in the `changes` job.

**4. Baseline file and wiring** - Add the small script that writes `coverage-baseline.json` from the three summaries, commit the file with the confirmed figures, pass each job's key and the baseline path into the action, and expose the deltas as outputs.

**5. Documentation and proof** - Document the ratchet and the one-command baseline update in CLAUDE.md and `docs/ci-affected-pipeline.md`, and record one scratch-branch run showing the wiring fails on a drop above 0.5.

### Stage 3: Comment Extension (PR3)

**6. Reports and outputs** - Add the merged Cobertura report to the three jobs' reportgenerator calls, upload the coverage artifacts even when a job fails, and expose the merge base and head from the `changes` job.

**7. Comment job** - Rebuild the `coverage-comment` job to check out the repository, download the three artifacts, and lay out Line, Branch, delta from the gate's outputs and verdict per project, plus the warning when `coverage-baseline.json` changed in the PR.

### Stage 4: Diff Coverage, Advisory (PR4)

**8. Tool check** - Decide between an existing diff-coverage tool and a custom script by checking whether the tool reports branch figures; build the custom script, with its self-test, only if it does not.

**9. Comment columns and the switch** - Add the Diff line and Diff branch columns to the comment, make the blocking switch an environment value defaulting to off, and document the observation window and the criteria for the later blocking PR in the pipeline document.

### Stage 5: Exclusion Guard and Ticks (PR5)

**10. Exclusion guard** - Add the script that lists exclusion patterns added by the PR in `coverlet.runsettings` and the vite config, with its self-test, show the warning line in the comment, and add it to the pipeline document.

**11. PRD ticks** - Tick the satisfied F09 boxes and the F09 cross-feature box in their own commit.
