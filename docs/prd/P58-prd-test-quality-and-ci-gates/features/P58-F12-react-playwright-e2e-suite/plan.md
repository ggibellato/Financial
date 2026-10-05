# Implementation Plan: React Playwright E2E Suite

**Prerequisites:**
- An up-to-date `main`, `gh` authenticated, `npm install` done in `Financial.Web`
- Stage 2 depends on Stage 1 being merged (the suite needs the sentinel in the test data)
- Never run the suite against the live Docker port (8080) or the live data files; use the local recipe in `e2e-environment.md`

### Stage 1: Test-Data Sentinel (PR1)

**1. Sentinel record** - Add the inactive `E2E-TEST-DATA` category to `data-cashflow.test.json` and update every existing assertion that counts categories.

**2. Verification and docs** - Run the full API test suite and the current smoke script to prove nothing else notices the record, and note the sentinel in the testing guide's example-and-seed-data reference.

### Stage 2: Playwright Test Project and CI Job (PR2)

**3. Runner and guard** - Replace the `playwright` library dependency with `@playwright/test`, add the config, global setup (URL required, health poll, sentinel check) and shared fixture, the ignore/typecheck/vitest-exclude entries, and the `smoke-test` and `test:e2e` scripts.

**4. Smoke specs** - Write the six `@smoke` specs (app loads, investment asset, add expense, blank-value validation, forced server error, historic average), locating by role and label only, and delete `smoke-test.mjs`.

**5. CI job** - Run `npm run smoke-test` in the `smoke` job and upload the report and results on failure, keeping the job name so `ci-status` is unchanged.

**6. Local verification** - Drive the guard's three abort cases and the full suite locally on a non-live port against temp data copies; push one deliberately failing commit to confirm the artifacts upload, then revert it.

### Stage 3: Documentation and PRD Ticks (PR3)

**7. Documentation** - Rewrite the e2e-environment reference for the new driver and specs, and update the pipeline document and the CLAUDE.md command lines (including `--headed`, `--ui`, `--debug` and the locator rules).

**8. PRD amendments and ticks** - Amend the F12 boxes for the sixth spec and the blank-value wording, then tick the satisfied boxes in a separate commit.
