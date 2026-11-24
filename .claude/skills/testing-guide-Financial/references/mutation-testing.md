> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Mutation Testing

The nightly workflow (`.github/workflows/nightly.yml`) mutates the production code and fails a job
when its score drops below that entry's break threshold; the failure opens the `nightly-failure`
issue. Fix a drop by adding the missing tests, never by lowering the break (a lowered break needs
its reason recorded). Scores, break values and the raise rule (lowest nightly score of a week, minus
5) live in `docs/ci-affected-pipeline.md` §Break threshold. The review that set them is recorded in
`docs/mutation-review/mutation-review.md` (complete 2026-11-23); each slice's notes there show
worked examples of the classifications below.

## What the nightly measures

| Entry | Mutates | Tests it runs | What a new file needs |
|---|---|---|---|
| `stryker-dotnet` (one per project) | Both Domains, both Applications, both Infrastructures, `Shared.Abstractions`, `Shared.Infrastructure`, `Api` | That project's own test project (`Investment.Application` also `Investment.Infrastructure.Tests`) | Nothing: new code in these projects is measured the next night, so its tests belong in the project's own test project |
| `stryker-js` Web.Logic | `src/utils/**/*.ts`, `src/hooks/**/*.ts` | Only `src/utils/**/__tests__` and `src/hooks/**/__tests__` (`vitest.mutation.config.ts`) | A new hook or util gets its own test there; one tested only through a component or page test shows as NoCoverage |
| `stryker-js` Web.Components | The files listed one by one in `stryker.components.config.json` | The whole unit suite | A new component or page with its own logic (state, branching, a form, a grid, sorting or filtering) is added to the list in the PR that creates it; a pure layout wrapper stays out |

## Checking new tests locally

- **.NET, one file in seconds:** `pwsh scripts/stryker.ps1 -Target CashFlow.Domain -Mutate "**/PotOperation.cs"`
  (several patterns separated by commas; `-AlsoTestedBy <project>` when another test project covers
  the code). `scripts/mutation-test-value.ps1` then lists the Survived and NoCoverage mutants by file.
  From bash call it as `pwsh -NoProfile -Command "& ./scripts/stryker.ps1 -Mutate 'a','b'"`:
  `pwsh -File` passes the comma list as one string and mutates nothing.
- **Web:** `npm run test:mutation -- --mutate src/hooks/useX.ts` in `Financial.Web` (add
  `stryker.components.config.json` first for a component). Set `STRYKER_TEST_INCLUDE` to the test
  files that import the file under test; a full-suite run takes about two hours.
- **A web test that reads a file outside `Financial.Web`** uses `src/test-utils/findInAncestors.ts`.
  A fixed `../../..` path breaks inside `.stryker-tmp/sandbox-*`, Stryker logs no error and every
  mutant those tests cover turns NoCoverage.
- **Leftovers:** a `.stryker-tmp` folder inflates `vitest` test counts; a `Shared.Infrastructure` run
  can leave a `data.json` in the test `bin` that fails `Constructor_WithNullPath` until deleted.

## Reading survivors

- **Survived or NoCoverage is a change no test noticed.** Timeouts count as detected, so a run full
  of timeouts can fake a high score; Ignored, CompileError and RuntimeError are excluded.
- **Accepted survivors:** log and message wording, stable `useCallback`/`useMemo` dependency lists,
  `ArgumentNullException.ThrowIfNull` (ignored by the config). Spans are asserted, not accepted.
- **Challenge every "equivalent" claim before accepting it.** The review found many wrong ones: a
  focus-recovery branch reachable through browser Back, a "spare" request that reset an open form,
  a render that only differs under StrictMode's double effects. Check Back/forward navigation, open
  forms and StrictMode before calling a mutant equivalent.
- **StrykerJS reports module-level constants as Survived** (Intl formats, regexes, message strings
  evaluated before the mutant activates) even when tests catch them. Confirm by hand: apply the
  mutation, run the test file.
- **Application tests do not re-check Domain results.** A Domain rule's mutants are killed by the
  Domain tests; the service's tests assert its own choices (scope, clock, lookups, ordering,
  persistence, mapping, spans).
