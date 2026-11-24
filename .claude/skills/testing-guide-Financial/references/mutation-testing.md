> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Mutation Testing

The nightly mutates the production code; a job fails when its score drops below its break
threshold, and that opens the `nightly-failure` issue. Fix a drop with the missing tests, not a
lower break. What each entry mutates, the score formula, the thresholds and the local commands are
in `docs/ci-affected-pipeline.md` §Nightly pipeline; `docs/mutation-review/` holds worked examples.

## What a new file needs

- **.NET code in a project on the nightly list** (both Domains, Applications and Infrastructures,
  `Shared.Abstractions`, `Shared.Infrastructure`, `Api`): nothing extra, it is measured the next
  night against the project's own test project, so its tests belong there.
- **A web hook or util:** its own test in `src/hooks/__tests__` or `src/utils/__tests__`. Web.Logic
  runs only those tests, so a hook tested only through a component or page shows as NoCoverage.
- **A web component or page with its own logic** (state, branching, a form, a grid, sorting or
  filtering): add it to `Financial.Web/stryker.components.config.json` in the PR that creates it.
  A pure layout wrapper stays out.

## Checking new tests locally

- **.NET:** `pwsh scripts/stryker.ps1 -Target <project> -Mutate "**/File.cs"` (flags in the
  pipeline doc's "Running it locally"). From bash call it as
  `pwsh -NoProfile -Command "& ./scripts/stryker.ps1 -Mutate 'a','b'"`: `pwsh -File` passes the
  comma list as one string and mutates nothing.
- **Web hook or util:** `npm run test:mutation -- --mutate src/hooks/useX.ts` in `Financial.Web`,
  with `STRYKER_TEST_INCLUDE` (comma-separated globs) narrowing the tests to the files that import it.
- **Web component:** `npm run test:mutation -- stryker.components.config.json --mutate src/components/X.tsx`.
  The default config runs only the hook and util tests, so every component mutant would show as
  NoCoverage. `STRYKER_TEST_INCLUDE` has no effect here and the whole suite runs, so mutate one file.
- **A web test that reads a file outside `Financial.Web`** uses `src/test-utils/findInAncestors.ts`.
  A fixed `../../..` path breaks inside `.stryker-tmp/sandbox-*`, Stryker logs no error and every
  mutant those tests cover turns NoCoverage.
- **Leftovers:** a `.stryker-tmp` folder inflates `vitest` test counts; a `Shared.Infrastructure` run
  can leave a `data.json` in the test `bin` that fails `Constructor_WithNullPath` until deleted.

## Reading survivors

- **Survived or NoCoverage is a change no test noticed.** Timeouts count as detected, so a run full
  of timeouts can fake a high score.
- **Accepted survivors:** log and message wording, stable `useCallback`/`useMemo` dependency lists.
  A missing span assertion is never accepted.
- **Do not call a mutant equivalent until you have checked Back/forward navigation, open forms and
  StrictMode double effects.** The review found many wrong claims, such as a focus-recovery branch
  that browser Back reaches.
- **StrykerJS reports module-level constants as Survived** (Intl formats, regexes, message strings
  evaluated before the mutant activates) even when tests catch them. Confirm by hand: apply the
  mutation, run the test file.
- **Application tests do not re-check Domain results.** The Domain tests kill a rule's mutants; the
  service's tests assert its own choices (scope, clock, lookups, ordering, persistence, mapping,
  spans).
