---
name: accounts-generalization-loop
description: Controller loop for one P60 (CashFlow accounts generalization) feature or stage — review the PRD feature with the user, update the PRD, write and review the spec, hand implementation off to a separate session, then verify its result before push. Use when the user says "start F03", "next feature", "review the F04 report" or similar for P60.
---

# Accounts Generalization Loop (P60)

This session is the **controller**. It reviews, writes documents, opens docs PRs and checks results. It **never implements**: implementation runs in a separate session that the user starts.

**Inputs:** a feature ID (`F03`), optionally a stage (`F02 stage 2`), or an implementation report pasted back by the user.

**Fixed paths:**
- PRD: `docs/prd/P60-prd-cashflow-accounts-generalization/prd-cashflow-accounts-generalization.md`
- Feature folder: `docs/prd/P60-prd-cashflow-accounts-generalization/features/P60-F<NN>-<kebab-name>/`
- Design doc: `docs/pending/cashflow-accounts-generalization.md`
- Parity toolkit: `scripts/parity/` (baseline tag `p60-baseline`)

## Standing rules (apply at every step)

- **Commit means local only** for this session's own docs branches: push and open a PR only when the user says "push".
- **The implementation session pushes and opens its own PR** (decided 2026-10-19). This session verifies the open PR before the user merges it.
- **The user merges every PR.** Never self-approve, never use `--admin`, never use auto-merge.
- After every push, run `gh pr checks <n> --watch` and report the result. Make sure the run belongs to the commit just pushed, not the previous one.
- **PR titles** follow Conventional Commits (`docs(P60-F03): …`, `feat(P60-F03): …`).
- **PR bodies** include the PRD Section 9 checklist for the feature, then end with the Claude Code footer and the session URL.
- **Never commit to `main`.** Branch off an up-to-date `main` (`git fetch` then `git merge --ff-only origin/main`).
- **Never switch branches in the main working tree while an implementation session may be using it.** Check `git status` and `git branch --show-current` first. If that tree is on a feature branch or has changes, put controller work in a worktree outside the repo: `git worktree add ../financial-wt-<topic> -b <branch> origin/main`. Remove the worktree after its PR merges.
- **No `git stash`.** Never run migrations against `data/data-cashflow.json`. Never smoke-test on port 8080. Never print values from `.env` or `scripts/deploy.local.json`.
- **Strict no-comments policy** in all code, and it is stated in every hand-off prompt.
- **Deploy only at the end of P60**, so the parity baseline stays the original pre-P60 app.
- **The PRD stays the source of truth.** Whenever a decision at any step (feature review, spec review, implementation deviation) contradicts the PRD or the design doc, update them in the same PR as the change that made the decision. Later features are reviewed against the PRD, so drift there spreads.
- **One question at a time.** For each one, offer options with a recommendation first. When the user asks to clarify, answer before asking again.

## Step 1 — Review the feature

1. Read the feature in the PRD:
   - Section 5 stories, Section 6 (Consumes, Provides, Capabilities, Experience, Error Handling);
   - Section 9 criteria, plus the cross-feature criteria that name it;
   - its row in the Section 8 dependency table.
2. Check that every dependency is merged on `main`. If one isn't, stop and say which.
3. **Verify the PRD's claims against the code.** PRD text can be stale compared with what earlier features actually shipped. Grep for every class, endpoint, file and behaviour it names. Use one Explore agent for broad sweeps.
4. Present the review to the user:
   - what the feature delivers;
   - numbered findings, each with the evidence (file:line) and a recommendation.
   - Look for: deviations from the code; scaffolding with no first user (move it to the feature that first uses it); PR size over 8 non-test code files (split into stages, each leaving `main` deployable); deploy or data-file implications; contradictions with earlier decisions.
5. Discuss the findings until the user decides each one. Record the decisions in the plan file. Make no edits during the discussion unless the user asks.

## Step 2 — Update the PRD (only if findings change it)

1. Branch `docs/p60-f<nn>-prd-review`, using a worktree if needed.
2. Edit the PRD:
   - Keep Section 8 consistent: the dependency table, the Mermaid edges and Consumes/Provides must agree. Recompute the waves.
   - Add or adjust the Section 9 criteria and cross-feature criteria.
   - Update the design doc only where the PRD now contradicts it.
3. Commit locally, show the diff summary, and wait for "push". Then open the PR, check CI, and wait for the user to merge.

## Step 3 — Write the spec

1. Branch `docs/p60-f<nn>-spec` off the updated `main`.
2. Invoke the `spec-writer` skill with:
   - the feature and the PRD path;
   - the stage split agreed in Step 1;
   - "Strict no-comments policy in all code";
   - "Stop after spec.md and plan.md are saved and committed locally; implementation runs in a separate session."
3. `spec-writer` runs its own interview, one question at a time. Decide whatever the codebase can answer by reading it instead of asking.
4. Before committing, check the spec:
   - every Section 9 criterion maps to a named test or to the Definition of Done;
   - each stage stays deployable on its own;
   - exact package versions are pinned (at least two weeks old).

## Step 4 — Review the spec with the user

1. Summarize the interview answers and the decisions the user should check: anything spec-writer chose on its own.
2. Answer questions; apply changes as extra commits on the same branch.
3. For each decision, grep the PRD (Capabilities, Error Handling, Section 9 criteria) and the design doc for wording it contradicts, and fix that wording in the same branch, as its own commit.
4. Wait for "push". Then open or update the PR, check CI, and wait for the merge.

## Step 5 — Hand off to the implementation session

Give the user the exact commands, one stage per session:

```
git switch main && git pull --ff-only && git switch -c feature/P60-F<NN>-<short-name>
```
```
/implement-feature docs/prd/P60-prd-cashflow-accounts-generalization/features/P60-F<NN>-<kebab-name> only stage <N> (PR <A/B>). Strict no-comments policy. When the stage is done and verified, push the branch and open the PR: Conventional Commits title, the PRD Section 9 checklist in the body, then run gh pr checks. Do not merge. Parity run on a fresh live copy from Google Drive before pushing.
```

Remind the user:
- to download a fresh live copy for the parity run;
- that the stage limit matters, because without it implement-feature runs every step.

## Step 6 — Verify the implementation

When the user brings the report and the PR number back, verify the **open PR** independently, before the user merges it. Never trust a "complete" claim. Check out the PR branch in a worktree outside the repo (`git worktree add ../financial-wt-pr<n> <branch>`), so the main working tree is not disturbed.

1. **Branch and scope:**
   - `git log origin/main..origin/<branch>` shows only this stage's commits;
   - the PR is still open and not merged;
   - the diff matches the stage's files in the spec;
   - CI (`gh pr checks <n>`) is green on the PR's head commit.
2. **Build and tests:**
   - `dotnet build --configuration Release`, then `dotnet test`;
   - `cd Financial.Web && npm run build` if the web app was touched.
3. **Comments:** grep the diff's added lines for new `//`, `///`, `/*` and `#` comments. There must be none.
4. **Contract:** the OpenAPI snapshot diff contains only intended changes. If it changed, the generated types were regenerated.
5. **Parity:** check the report's run, or rerun `scripts/parity/Invoke-Parity.ps1 -Step All -LiveCopy <fresh copy>`. It must show 0 differences and an unchanged SHA-256 for the copy.
6. **Reviews:** `/simplify` and the architecture-reviewer ran (plus the ui-reviewer for UI stages), with their findings applied or their skips justified.
7. **PRD ticks:** only in the final stage's PR, in their own commit, and only for criteria actually verified. Cross-feature boxes are ticked only when every feature they name has shipped.
8. **PRD drift:** for each deviation in the report, check whether it contradicts the PRD or the design doc. If it does, agree the fix with the user and have it committed to the PR branch before the merge (never a separate PRD-only PR after the merge).
9. **Size:** count the non-test code files. Report any stage over 8, unless the overage is an exempt repeated change.
10. **PR body:** Conventional Commits title, the Section 9 checklist with each ticked box naming its test, and the footer.
11. Report to the user: the status, the acceptance checklist (✓, ✗ or —), deviations and any problems, and whether the PR is ready to merge. This session never implements: if something must change, give the user the exact fix request to paste into the implementation session, which pushes the fix to the same PR; then verify the new head commit again.

## Step 7 — After the merge

1. Update `main` (`git fetch`, then `git merge --ff-only origin/main`) and remove any worktree whose PR merged, including the PR verification worktree.
2. Save follow-ups and non-obvious decisions in memory: one file per fact, plus a line in `MEMORY.md`.
3. Continue with the next stage (Step 5) or the next feature (Step 1). Always ask before starting.
