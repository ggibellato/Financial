# CI: affected-only pipeline

`.github/workflows/build.yml` runs only the jobs a pull request can affect. The mapping from paths to
jobs lives in one place, `.github/scripts/detect-changes.sh`; the workflow just consumes its outputs.

Every push to `main` (i.e. every merge) runs the full pipeline regardless of the diff: `main` must
always be deployable, and the merge commit is what gets deployed.

## Path groups

The repo keeps its flat layout. These are the groups the detection script recognises, in the order it
checks them (first match wins):

| Group | Paths | Why it is its own group |
|---|---|---|
| Docs | `docs/`, `specs/`, `dev-util/`, `.claude/`, `.specify/`, `LICENSE`, `.gitignore`, `.editorconfig`, `.dockerignore`, and `*.md` outside the source directories below | Nothing here reaches a build. A Markdown file inside `Financial.*`, `Tests/`, `Tools/` or `Integrations/` follows that directory's rule |
| Contract (server side) | `Financial.Api/`, `Tests/Financial.Api.Tests/` (includes the OpenAPI snapshot) | The HTTP surface `Financial.Web` is compiled against |
| Contract (DTOs) | `Financial.*.Application/DTOs/` | Wire format for the SPA **and** linked in-process into the WPF app |
| Contract (client side) | `Financial.Web/src/api/` | Hand-written TypeScript mirror of the DTOs (`types.ts`, client) |
| WPF | `Financial.App/` (also runs `backend`), `Tests/Financial.Presentation.Tests/` | Desktop front end; nothing else depends on it. `Financial.App` also runs `backend` because `Financial.Architecture.Tests`, which pin its references, run there |
| Web | `Financial.Web/` | React SPA; nothing else depends on it |
| Backend core | `Financial.*.Domain/`, `Financial.*.Application/`, `Financial.*.Infrastructure/`, `Financial.Shared.*/`, `Integrations/`, `Tools/`, `Tests/`, `coverlet.runsettings` | Shared libraries; WPF references them directly, the API hosts them |
| Infra | `.github/`, `Dockerfile*`, `docker-compose*`, `Financial.slnx`, `global.json`, `nuget.config`, `Directory.*`, `scripts/`, `deploy/`, `data/` | Build/deploy plumbing and data templates; can affect anything |
| Unclassified | anything else | Treated as Infra (fail-safe) |

## Affected rules

| Change in… | backend | wpf | wpf-e2e | web | web-e2e |
|---|:-:|:-:|:-:|:-:|:-:|
| Docs | – | – | – | – | – |
| Contract (server side) | ✔ | – | – | ✔ | ✔ |
| Contract (DTOs) | ✔ | ✔ | ✔ | ✔ | ✔ |
| Contract (client side) | – | – | – | ✔ | ✔ |
| WPF (`Financial.App/`) | ✔ | ✔ | ✔ | – | – |
| WPF tests (`Tests/Financial.Presentation.Tests/`) | – | ✔ | – | – | – |
| WPF E2E tests (`Tests/Financial.App.E2ETests/`) | – | – | ✔ | – | – |
| Web | – | – | – | ✔ | ✔ |
| Backend core | ✔ | ✔ | ✔ | – | ✔ |
| Infra / unclassified / no base commit / diff failure | ✔ | ✔ | ✔ | ✔ | ✔ |
| Any push to `main` | ✔ | ✔ | ✔ | ✔ | ✔ |

Jobs:

- **backend** (Windows) — builds `Financial.Api` and runs every `Tests/*.Tests.csproj` listed in `Financial.slnx` except the two WPF ones, `Financial.Presentation.Tests` and `Financial.App.E2ETests` (so `Financial.Architecture.Tests` run here, and only here), with coverage and a blocking 90% coverage gate (see `CLAUDE.md`). The run filters out `Category=E2E` and `Category=Live`: every test class declares a `Category` trait (`Unit`, `Integration`, `Live`, `E2E`, `Smoke`), enforced by `CategoryTraitCoverageTests`, and `Live` tests (real web-parser sites) never run on a PR.
- **wpf** (Windows) — builds `Financial.App` and runs `Financial.Presentation.Tests`, with coverage (scoped to `Financial.Presentation.App` only) and the same blocking coverage gate as `backend`.
- **wpf-e2e** (`windows-latest`) — runs whenever the `wpf_e2e` flag is set (changes to `Financial.App/`, the backend it composes, or the E2E tests themselves): builds `Financial.App` and `Tests/Financial.App.E2ETests` (Release) and runs `--filter Category=Smoke`. The suite launches the built exe once per test against throwaway copies of the test data and drives it through UI Automation (FlaUI/UIA3); it needs the runner's interactive desktop. On failure it uploads `TestResults/e2e-artifacts` (a screenshot, the app log and the exit state per failed test) as `wpf-e2e-artifacts`. It is in `ci-status`'s `needs`, is excluded from `backend`'s `dotnet test` by the `Category!=E2E` filter (so it never runs under coverage), and `Tests/Financial.App.E2ETests/*` changes run `wpf` only. **Fallback rule:** if the hosted desktop proves unreliable (more than 2 infrastructure failures, not test failures, in its first 20 runs), move the job to the nightly workflow and drop it from `ci-status`'s `needs` in a PR that records the decision here.
- **web** (Ubuntu) — `npm run lint`, `npm run test:coverage`, `npm run build`, with the same blocking coverage gate as `backend`/`wpf`.
- **coverage-comment** (Ubuntu) — posts one combined sticky PR comment: per project the line and branch coverage, the delta against `coverage-baseline.json`, the diff coverage, the job verdict, a warning when the PR changes the baseline file, and a link to the run's coverage-report artifacts. Each job's reports include a Cobertura file (`coverage-report-*` artifacts) for the diff-coverage stage. Not in `ci-status`'s `needs`, so a failure here (e.g. the comment action itself erroring) never blocks merge.
- **web-e2e** (Ubuntu) — publishes the API with the built SPA and runs the `@playwright/test` `@smoke` specs (`npm run smoke-test`; `playwright-report/` and `test-results/` are uploaded as `web-e2e-artifacts` on failure). The suite refuses to start unless `SMOKE_APP_URL` is set and the app reports the `E2E-TEST-DATA` sentinel category. Runs whenever either side of the HTTP boundary changed, even when a backend/web job was skipped.
- **ci-status** — always runs; the only check branch protection should require. Passes when every job succeeded or was skipped by `changes`; fails if change detection failed or any job failed/was cancelled — including a red coverage gate, since that now fails its job.

Security-relevant configuration (`Financial.Api/appsettings*.json`, `Program.cs`, auth/CORS setup) sits under `Financial.Api/` and therefore hits the Contract rule; `Dockerfile`, compose files and anything under `deploy/` hit Infra and run everything.

## Safeguards

- `.github/scripts/detect-changes.test.sh` pins which jobs each kind of path triggers. It sources the classifier and runs as the first step of the `changes` job, so a rule mistake that would silently skip jobs fails `changes` and `ci-status`.
- `.github/scripts/test-hygiene.sh` runs in `changes` on pull requests and scans only the lines the change adds (`git diff -U0`) for `DateTime.Now/Today/UtcNow` in tests or in any `Financial.*` production file (the permanent `ProductionClockReadsTests` architecture test also scans the whole tree and allows `TimeProvider.System` only in `DependencyInjection` registrations, `MonthYearPicker` and `DebouncedJsonStorage`), `Task.Delay(`/`Thread.Sleep(`/`Skip =` in `Tests/`, and `.only(`/`.skip(`/`waitForTimeout(` in the web sources (`new Date()` in a web test with no fake clock is a warning). Legacy violations on unchanged lines are never reported. Append `// hygiene-allow: <reason>` to exempt one line; the reason is mandatory and exemptions are listed in the job summary. `.github/scripts/test-hygiene.test.sh` pins every rule against a throwaway repository and runs in `changes` first. ESLint backs it up in `npm run lint`: `vitest/no-focused-tests` and `vitest/no-disabled-tests` on web test files, and the `eslint-plugin-playwright` recommended rules on `Financial.Web/tests/e2e/**`. Run it locally with `bash .github/scripts/test-hygiene.test.sh`, or scan a branch with `bash .github/scripts/test-hygiene.sh main HEAD`.
- **Coverage ratchet.** `coverage-baseline.json` (repo root) holds each job's line and branch coverage. `.github/scripts/coverage-gate.ps1`, called by the `coverage-gate` action, fails a job when line or branch coverage is more than 0.5 points below its baseline entry (`backend`, `wpf`, `web`), on top of the 90% line floor, and fails closed (`coverage-baseline.json missing or invalid`) when the file, the job's entry or the report's branch figure is missing. `.github/scripts/coverage-gate.test.ps1` pins every branch of that logic and runs in `changes`. The numbers are the `summary.linecoverage`/`summary.branchcoverage` figures of each job's reportgenerator `Summary.json` (one decimal). A job skipped by `changes` skips its ratchet.
- **Diff coverage (advisory).** On pull requests the `coverage-comment` job runs `.github/scripts/diff-coverage.ps1` over each job's Cobertura report and the `git diff -U0` against the merge base, and shows the covered/total changed lines and branches (Diff line, Diff branch). It counts only changed `.cs`/`.ts`/`.tsx` files under `Financial.*.Domain`, `Financial.*.Application`, `Financial.Api`, `Financial.App/ViewModels` and `Financial.Web/src/{hooks,utils,components,pages}`; the thresholds are 80% of lines and 70% of branches. `diff-cover` was not used: it counts partially covered branch lines, not covered/total branches, and cannot read the web report. A missing merge base or report shows `not computed` and never blocks. `diff-coverage.test.ps1` pins the logic and runs in `changes`.
- **Diff coverage window.** The check is advisory: `DIFF_COVERAGE_BLOCKING` (workflow `env`, default `false`) is the only switch. Observe it for 2-4 weeks. Flip it to `true` in a separate one-line PR once at least 20 PRs have been through it and at most 10% of the below-threshold flags were false positives; that PR cites the count.
- **Exclusion guard.** On pull requests the ``coverage-comment`` job runs ``.github/scripts/coverage-exclusions.ps1``, which compares the ``<Exclude>`` patterns in ``coverlet.runsettings`` and the ``coverage.exclude`` entries in ``Financial.Web/vite.config.ts`` between the merge base and the head, and puts a ``⚠ Coverage exclusions added: ...`` line in the comment when the PR adds any. It is advisory: removing a pattern is silent, and nothing fails. ``coverage-exclusions.test.ps1`` pins it and runs in ``changes``.
- **Updating the baseline.** Never by hand: download the three `coverage-report-*` artifacts of a green `main` run and run `pwsh .github/scripts/coverage-baseline.ps1 -Backend <Summary.json> -Wpf <Summary.json> -Web <Summary.json>`, then commit the file in its own PR. Raise it when coverage has genuinely improved; lowering it needs a stated reason in the PR.
- **Baseline reminder.** After a push to ``main`` the ``baseline-reminder`` job (not in ``ci-status``'s ``needs``) runs ``.github/scripts/baseline-reminder.ps1`` over the line and branch deltas the three jobs publish. When any is more than 0.5 above ``coverage-baseline.json`` it comments on the merged PR, naming the figures and the command to raise the baseline. It is raise-only and never edits the file; it stays silent when nothing rose, when a job was skipped, or when the commit has no PR. ``baseline-reminder.test.ps1`` runs in ``changes``.
- A path no rule recognises runs everything, and the step summary names it — add a rule rather than living with the full run.
- No base commit (first push, force-push that orphaned `github.event.before`) or a failing `git diff` runs everything.
- The `changes` job uses the merge-base with the PR base, so a stale branch is diffed against the commit it forked from, not the current `main` tip.
- Skipped jobs are still reported on the PR as *skipped*; required-check status comes from `ci-status` alone, so a skipped job can never leave a PR stuck on "expected" checks.

## Branch protection

Replace the three per-job required checks with the gate:

```bash
gh api -X PATCH repos/ggibellato/Financial/branches/main/protection/required_status_checks \
  --input - <<'EOF'
{ "strict": true, "contexts": ["ci-status", "semantic-pr"] }
EOF
```

Do this after the workflow is merged, otherwise PRs opened before the merge will wait on a check that never reports.

## Merge-gate bypass

`main` is protected: `ci-status` and `semantic-pr` are required, and one approving review is needed.
`enforce_admins` is deliberately `false`. This is a single-owner repository and administrator
override is the escape hatch for an emergency (for example, a broken `ci-status` that blocks its own
fix). Any administrator merge on a red build must be followed by a fix PR that restores green; do
not leave `main` red.

## Nightly pipeline

`.github/workflows/nightly.yml` runs on a cron (02:00 UTC) and on `workflow_dispatch`. It is **not** a required check and is not in `ci-status`'s `needs`; it only reports. Scheduled workflows run from the default branch, so use `workflow_dispatch` on a branch to try a change.

- **stryker-dotnet** (`windows-latest`, one matrix entry per Domain project) — runs Stryker.NET (`.config/dotnet-tools.json`, config `.config/stryker-config.json`) over `Financial.CashFlow.Domain` and `Financial.Investment.Domain` against their own test project only (run from that project's folder: from the repository root Stryker scans `Financial.slnx` and tests every mutant against every test project that references the Domain assembly, about 15 times slower), excluding `Category=Live` and `Category=E2E`. `.github/scripts/mutation-score.ps1` turns the JSON report into a score (Killed + Timeout over Killed + Timeout + Survived + NoCoverage; Ignored, CompileError and RuntimeError are excluded) in the step summary, and the HTML and JSON reports are uploaded as `stryker-dotnet-<name>`. Reproduce locally with `dotnet tool restore`, then in `Tests/Financial.CashFlow.Domain.Tests`: `dotnet stryker --config-file ../../.config/stryker-config.json --project Financial.CashFlow.Domain.csproj`.
- **stryker-js** (`ubuntu-latest`) — runs StrykerJS (`Financial.Web/stryker.config.json`, `npm run test:mutation`) over `src/utils/**/*.ts` and `src/hooks/**/*.ts` only (no `.tsx` components, generated code or test helpers) with the TypeScript checker, against the utils and hooks tests only (`vitest.mutation.config.ts`; the full web suite makes the initial test run time out). `ignoreStatic` skips mutants in code that runs once at module load, which would otherwise take most of the time. Incremental mode keeps `reports/stryker-incremental.json` in an `actions/cache` entry, so a run without source changes reuses it. The score (same script, name `Web.Logic`) goes to the step summary and the report is uploaded as `stryker-js`. A full run takes about 30 minutes; run `npm run test:mutation` in `Financial.Web` to reproduce it.
- **e2e-web** (`ubuntu-latest`) — the whole Playwright suite (`npm run test:e2e`, not only `@smoke`) through `.github/actions/web-e2e`, the same composite action the PR `web-e2e` job uses with `npm run smoke-test`: it builds the SPA, publishes the API, seeds the test JSON, starts the app and waits for `/health`. The caller checks out the repository and uploads `web-e2e-artifacts` (report, traces, screenshots, videos) on failure.
- **e2e-wpf** (`windows-latest`) — every `Category=E2E` FlaUI test (the PR job runs `Category=Smoke`), uploading `wpf-e2e-artifacts` on failure.
- **live** (`windows-latest`) — `dotnet test Financial.slnx --filter "Category=Live"`, the verifications against the real sites. The test step has `continue-on-error`, so a changed or unreachable site never turns the run red; `.github/scripts/test-results-summary.ps1` reads the trx and lists every failed test with its message in the step summary. Fixing a stale verification is a normal PR, not part of the nightly.
- **ac-traceability** (`ubuntu-latest`) — `.github/scripts/ac-traceability.ps1` reads every checkbox in `docs/prd/**/prd-*.md` whose text starts with an id (`**P52-F02-name-01**`) and looks for that id in the tests: `[Trait("AC", "<id>")]` under `Tests/**/*.cs` and `[AC <id>]` in the `Financial.Web/src` test titles. The step summary lists, per PRD, the ids no test carries; tags naming an id no PRD has are listed as orphans; PRDs with no ids at all get one collapsed line each (criteria count, "no ids"), because they cannot be traced. Advisory: untraced criteria never fail the job, only a script error does. To make a criterion traceable, give its checkbox an id and tag the test that proves it.
- **report** (`ubuntu-latest`, the only job with `issues: write`) — runs after the blocking-class jobs (`stryker-dotnet`, `stryker-js`, `e2e-web`, `e2e-wpf`, `ac-traceability`; never `live`) and, when any of them failed or was cancelled (a job that hits its timeout ends as cancelled), calls `.github/scripts/nightly-issue.ps1`: it creates the `nightly-failure` label if missing, then comments the failed job names and the run link on the single open `nightly-failure` issue, or opens it when none is open. The issue is never closed automatically: close it by hand once the cause is fixed, and the next failure opens a new one. The job is skipped on a fully green night. Tests of the script run against a fake `gh`.
- **Running it locally.** `pwsh scripts/stryker.ps1` runs both Domain projects, prints the same score line and opens the HTML reports; `-Target CashFlow|Investment` picks one, `-Mutate "**/Entities/Expense.cs"` re-runs a single file in seconds after adding tests, `-NoOpen` skips the browser. Read the report's Survived and No coverage mutants: each is a change no test noticed.
- **Break threshold.** `thresholds.break` is unset (0 for Stryker.NET) until three scheduled runs exist; then a PR sets it to the lowest of those three scores minus 5, per tool.

## Extending

- **New shared library** (e.g. `Financial.Shared.Something/`): already covered by the `Financial.Shared.*/` pattern.
- **New bounded context** (`Financial.Foo.{Domain,Application,Infrastructure}`): covered by the `Financial.*.Domain/` family of patterns; its DTOs fall under the DTO contract rule automatically.
- **New front end or service**: add a job to `build.yml`, an output to the `changes` job, a `case` branch in `detect-changes.sh` for its paths (with a case in `detect-changes.test.sh`), and list it in `ci-status`'s `needs`. Decide which contract rule(s) should also set its flag.
- **New test project**: add it to `Financial.slnx`; the backend job discovers it from there.

Test a rule change locally before pushing:

```bash
bash .github/scripts/detect-changes.sh origin/main HEAD
```

Run the classifier self-test with `bash .github/scripts/detect-changes.test.sh`.
