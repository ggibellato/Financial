# Implementation Plan: Test Hygiene Gate

**Prerequisites:**
- An up-to-date `main`, `gh` authenticated, `npm install` done in `Financial.Web`
- Each stage below is one PR; they are independent and may merge in either order

### Stage 1: Diff-Only Scanner and CI Wiring (PR1)

**1. Scanner** - Add `test-hygiene.sh`: parse the added lines of the PR diff, apply the rule table, print file:line messages and annotations, honour the allow comment, write the job summary, and exit non-zero on any error.

**2. Scanner self-test** - Add `test-hygiene.test.sh` that builds a throwaway git repository and pins every rule, the legacy-line exemption, the allow comment and the reasonless-allow failure.

**3. CI wiring** - Add the self-test step (always) and the scan step (pull requests only) to the `changes` job, reusing the merge-base the classifier step computes.

**4. Revert checks and documentation** - Prove each rule's self-test case fails when the rule is deleted, record it in the PR body, and document the gate in the pipeline document, the CI paragraph of `CLAUDE.md` and the implementation rules.

### Stage 2: ESLint Rules and Ticks (PR2)

**5. Vitest rules** - Add `@vitest/eslint-plugin` and enable `no-focused-tests` and `no-disabled-tests` as errors for web test files; confirm the existing suite lints clean.

**6. Playwright rules** - Add `eslint-plugin-playwright` and apply its recommended rules to `tests/e2e/**`, ready for F12.

**7. Revert checks and PRD ticks** - Show that a temporary `it.only(` and `it.skip(` fail `npm run lint`, record it in the PR body, then tick the satisfied F11 boxes in the PRD in a separate commit.
