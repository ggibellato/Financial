---
name: multi-session-implementation
description: Runs a PRD feature through planning, staged implementation and independent verification, each in its own context. The controller session reviews the PRD feature with the user, updates the PRD, writes the spec and plan, then for every stage starts an implementer agent and a verifier agent, relays their questions to the user and reports when the PR is ready to merge. Also the entry point for a manually started implementation session ("implement F10 stage 3"). Use whenever the user wants to start, continue or check a PRD feature or stage in this repo: "start F12", "plan F11", "next stage", "run stage 4", "PR up", "verify PR 1150", "merged", "implement F10 stage 3", even when they don't name the skill.
---

# Multi-Session Implementation

A feature goes through three roles. Each runs in its own context window, so no single session fills up with another role's file reads, test output and review passes.

| Role | Runs as | Model and effort | Does |
|---|---|---|---|
| **Controller** | The user's session, invoking this skill | Opus, high (planning in plan mode) | Talks to the user, reviews the PRD feature, updates the PRD, writes the spec and plan, starts the other two roles, relays questions, keeps the progress record. Never writes feature code. |
| **Implementer** | A subagent the controller starts, one per stage (or a session the user starts with `implement … stage N`) | Opus, medium | Builds one stage, tests and reviews it, pushes the branch and opens the PR. Asks instead of guessing. |
| **Verifier** | A fresh subagent the controller starts for each PR | Opus, high | Checks the open PR independently against `references/verifier.md` and returns a verdict. |

The user stays in charge: every review decision is theirs, every question reaches them one at a time with a recommendation, and **they merge every PR** (branch protection requires a review, and an agent approving or merging its own PR is blocked).

## Pick the mode

| The user says | Mode |
|---|---|
| "start F12", "plan F11", "review F12" | **Planning** (Steps 1–4) |
| "next stage", "run stage 4", "go" after a merge | **Stage loop** (Step 5) |
| "PR up", "verify PR 1150", "check 1150" | **Verification only** (Step 5.4 onward), for a stage the user implemented by hand |
| "merged" | **After the merge** (Step 6) |
| "implement F10 stage 3", or this skill was invoked inside an implementer agent | **Implementer**: read `references/implementer.md` and follow it; ignore the rest of this file |

When the PRD has a project file in `references/` (for example `references/P60.md` for the P60 PRD), read it at the start of every mode. It holds that PRD's extra checks, such as parity runs and deploy steps.

## Standing rules

- **Progress record.** Keep one memory file per feature, `project_<prd>_<feature>_progress.md` (for example `project_p60_f10_progress.md`), with a line in `MEMORY.md`. It holds the review decisions, the stage table (branch, PR, merge commit, verdict), items a stage deferred and anything the next session needs. Update it after every decision, verdict and merge. Memory loads in every session, so a new controller can pick up exactly where the last one stopped; read it first.
- **Git.** The controller's own docs work happens in a worktree outside the repo (`git worktree add ../<repo>-wt-<topic> -b <branch> origin/main`), never by switching branches in the user's checkout. "Commit" means a local commit; push only when the user says so. No `git stash`. Run `git branch --show-current` before any merge or fast-forward.
- **PRs.** Conventional Commits titles; bodies with a summary, a review guide (files in reading order, one line each), deviations, deferred items, the PRD Section 9 checklist with each stage's part noted, and the footer. After every push, run `gh pr checks <n> --watch` and confirm the run is on the head commit just pushed.
- **The PRD is the source of truth.** A decision at any step that contradicts the PRD, spec or plan is written back in the same PR as the change, in its own docs commit. Later features are reviewed against these documents, so drift spreads.
- **PR size** follows `docs/rules/design.md`: at most 8 non-test code files and about 15 in total, generated files excluded. A planned stage that doesn't fit is split in the spec; an implemented PR that doesn't fit goes back to be split, unless the user approves the overage.
- **Questions.** One at a time, options first with the recommended one marked, and answer the user's clarifying questions before asking the next. Decide whatever the code can answer by reading it instead of asking.
- **Data and secrets.** Follow `CLAUDE.md` and the project file: never write the live data file, never run migrations on it, never smoke-test on the live port, never print secrets.

## Planning

### Step 1 — Review the feature

1. Read the feature in the PRD: Section 5 stories; Section 6 (Consumes, Provides, Capabilities, Experience, Error Handling); its Section 8 dependency row; Section 9 criteria and the cross-feature criteria that name it.
2. Check every dependency is merged on `main`; if one isn't, stop and say which.
3. Verify the PRD's claims against the code: grep every class, route, file and behaviour it names, since text written before earlier features shipped is often stale. Use one Explore agent for broad sweeps so the file dumps stay out of this context.
4. Present numbered findings, each with evidence (`file:line`) and a recommendation. Look for: contradictions with the code or earlier decisions; scaffolding with no first user (move it to the feature that first uses it); data-file and deploy implications; the size of the work against the PR limit, with a proposed stage split where each stage leaves `main` deployable.
5. Discuss until the user decides each finding. Record the decisions in the progress file.

### Step 2 — Update the PRD (only when decisions change it)

Branch `docs/<prd>-f<nn>-prd-review` in a worktree. Keep Section 8 consistent (dependency table, Mermaid edges, Consumes/Provides, waves); add or adjust Section 9 criteria; update the design doc only where it now contradicts the PRD. Preserve the file's line endings. Commit locally, show the diff summary, wait for "push", open the PR, check CI, wait for the user's merge.

### Step 3 — Write the spec and plan

Branch `docs/<prd>-f<nn>-spec` off the updated `main`, then invoke the `spec-writer` skill with the feature, the PRD path, the agreed stage split, "strict no-comments policy in all code" and "stop after spec.md and plan.md are saved and committed locally". Before committing, check that:
- every Section 9 criterion maps to a named test or to the Definition of Done;
- each stage is deployable on its own and its file list fits the PR limit (count DTOs, DI and generated-contract changes too, as they are what usually pushes a stage over);
- the plan's stage headings are what the implementer will be told to run;
- package versions are pinned and at least two weeks old.

### Step 4 — Review the spec with the user

Summarize the interview answers and list every choice the spec-writer made on its own for the user to check. Apply changes as extra commits; fix contradicted PRD or design-doc wording in its own commit. Wait for "push", open or update the PR, check CI, wait for the merge. Then ask before starting stage 1.

## Step 5 — Stage loop

Run one stage at a time: stages depend on each other, and more than two or three agents at once saturate the machine.

**5.1 Brief.** Write the implementer brief:
```
Role: implementer. Read .claude/skills/multi-session-implementation/references/implementer.md and follow it.
Feature folder: <docs/prd/.../features/<feature>>
Stage: <N> (plan steps <a-b>), branch <feature/<PRD>-F<NN>-<short-name>>
Project file: <references/<PRD>.md or "none">
Notes for this stage: <items earlier stages deferred to it, user decisions since the spec, data-file choice for checks>
```

**5.2 Start the implementer** with the Agent tool: `subagent_type: "general-purpose"`, `model: "opus"`, `effort: "medium"`, `isolation: "worktree"`, the brief as the prompt. Keep its agent ID in the progress file; the same agent receives answers and fix requests later, with its context intact. Agents run in the background and you are notified when one finishes, so don't poll; tell the user what is running and wait. If `SendMessage` is listed only as a deferred tool, load it with ToolSearch before the first relay.

**5.3 Relay questions.** When the implementer stops with `QUESTIONS`, ask the user each one in turn (AskUserQuestion, recommended option first, the implementer's evidence in the question so it stands on its own). Send all answers back in one SendMessage to the implementer's ID. Record decisions that change the spec in the progress file. Repeat until it returns `REPORT`.

**5.4 Verify.** Start a fresh verifier with the Agent tool: `subagent_type: "general-purpose"`, `model: "opus"`, `effort: "high"`, `isolation: "worktree"`, prompt:
```
Role: verifier. Read .claude/skills/multi-session-implementation/references/verifier.md and follow it.
PR: <n>   Feature folder: <path>   Stage: <N>   Project file: <path or "none">
Decisions since the spec: <list, or "none">
```
Give it the PR and the documents, not the implementer's report or your own conclusions: an agent handed a conclusion tends to confirm it.

**5.5 Act on the verdict.**
- `READY`: tell the user the PR is ready to merge, with the verifier's summary (checklist, deviations accepted, anything deferred). Wait for "merged".
- `CHANGES NEEDED`: for findings that need a decision (a deviation the user might accept, an overage), ask the user first. Send the fix requests to the implementer's ID with SendMessage; when it reports the new head commit, send "verify the new head <sha>" to the same verifier and repeat 5.5.
- `BLOCKED` (CI red, merged already, wrong base): explain to the user and agree the next step.

**Manual stages.** If the user prefers to run a stage in their own session, give them:
```
git switch main && git pull --ff-only && git switch -c <branch>
```
```
/multi-session-implementation implement <feature folder> stage <N>. Notes: <stage notes>
```
When they say "PR up", continue at 5.4. If the PR was merged before the check, verify it after the merge the same way and report any fix as a follow-up.

## Step 6 — After the merge

1. Fast-forward `main` only where it is safe (check the current branch; never switch the user's checkout). Remove worktrees whose PR merged; agent worktrees with no changes clean themselves up.
2. Update the progress file: merge commit, verdict, deferred items with the stage that takes them.
3. Save non-obvious follow-ups in memory, one fact per file.
4. On the feature's last stage: confirm the PRD boxes were ticked in their own commit (cross-feature boxes only when every feature they name has shipped), then follow the project file's deploy steps, if any, when the user asks for a deploy.
5. Ask before starting the next stage or the next feature.
