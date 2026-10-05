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

| Change in… | backend | wpf | web | smoke |
|---|:-:|:-:|:-:|:-:|
| Docs | – | – | – | – |
| Contract (server side) | ✔ | – | ✔ | ✔ |
| Contract (DTOs) | ✔ | ✔ | ✔ | ✔ |
| Contract (client side) | – | – | ✔ | ✔ |
| WPF (`Financial.App/`) | ✔ | ✔ | – | – |
| WPF tests (`Tests/Financial.Presentation.Tests/`) | – | ✔ | – | – |
| Web | – | – | ✔ | ✔ |
| Backend core | ✔ | ✔ | – | ✔ |
| Infra / unclassified / no base commit / diff failure | ✔ | ✔ | ✔ | ✔ |
| Any push to `main` | ✔ | ✔ | ✔ | ✔ |

Jobs:

- **backend** (Windows) — builds `Financial.Api` and runs every `Tests/*.Tests.csproj` listed in `Financial.slnx` except the two WPF ones, `Financial.Presentation.Tests` and `Financial.App.E2ETests` (so `Financial.Architecture.Tests` run here, and only here), with coverage and a blocking 90% coverage gate (see `CLAUDE.md`).
- **wpf** (Windows) — builds `Financial.App` and runs `Financial.Presentation.Tests`, with coverage (scoped to `Financial.Presentation.App` only) and the same blocking coverage gate as `backend`.
- **wpf-e2e** (`windows-latest`) — runs whenever `wpf` runs: builds `Financial.App` and `Tests/Financial.App.E2ETests` (Release) and runs `--filter Category=Smoke`. The suite launches the built exe once per test against throwaway copies of the test data and drives it through UI Automation (FlaUI/UIA3); it needs the runner's interactive desktop. On failure it uploads `TestResults/e2e-artifacts` (a screenshot, the app log and the exit state per failed test) as `wpf-e2e-artifacts`. It is in `ci-status`'s `needs`, is excluded from `backend`'s `dotnet test` by filter (so it never runs under coverage), and `Tests/Financial.App.E2ETests/*` changes run `wpf` only. **Fallback rule:** if the hosted desktop proves unreliable (more than 2 infrastructure failures, not test failures, in its first 20 runs), move the job to the nightly workflow and drop it from `ci-status`'s `needs` in a PR that records the decision here.
- **web** (Ubuntu) — `npm run lint`, `npm run test:coverage`, `npm run build`, with the same blocking coverage gate as `backend`/`wpf`.
- **coverage-comment** (Ubuntu) — posts one combined sticky PR comment with backend/wpf/web's coverage %, gate verdict, and a link to the run's coverage-report artifacts. Not in `ci-status`'s `needs`, so a failure here (e.g. the comment action itself erroring) never blocks merge.
- **smoke** (Ubuntu) — publishes the API with the built SPA and runs the `@playwright/test` `@smoke` specs (`npm run smoke-test`; `playwright-report/` and `test-results/` are uploaded as an artifact on failure). The suite refuses to start unless `SMOKE_APP_URL` is set and the app reports the `E2E-TEST-DATA` sentinel category. Runs whenever either side of the HTTP boundary changed, even when a backend/web job was skipped.
- **ci-status** — always runs; the only check branch protection should require. Passes when every job succeeded or was skipped by `changes`; fails if change detection failed or any job failed/was cancelled — including a red coverage gate, since that now fails its job.

Security-relevant configuration (`Financial.Api/appsettings*.json`, `Program.cs`, auth/CORS setup) sits under `Financial.Api/` and therefore hits the Contract rule; `Dockerfile`, compose files and anything under `deploy/` hit Infra and run everything.

## Safeguards

- `.github/scripts/detect-changes.test.sh` pins which jobs each kind of path triggers. It sources the classifier and runs as the first step of the `changes` job, so a rule mistake that would silently skip jobs fails `changes` and `ci-status`.
- `.github/scripts/test-hygiene.sh` runs in `changes` on pull requests and scans only the lines the change adds (`git diff -U0`) for `DateTime.Now/Today/UtcNow` in tests or in production files that use `TimeProvider`, `Task.Delay(`/`Thread.Sleep(`/`Skip =` in `Tests/`, and `.only(`/`.skip(`/`waitForTimeout(` in the web sources (`new Date()` in a web test with no fake clock is a warning). Legacy violations on unchanged lines are never reported. Append `// hygiene-allow: <reason>` to exempt one line; the reason is mandatory and exemptions are listed in the job summary. `.github/scripts/test-hygiene.test.sh` pins every rule against a throwaway repository and runs in `changes` first. ESLint backs it up in `npm run lint`: `vitest/no-focused-tests` and `vitest/no-disabled-tests` on web test files, and the `eslint-plugin-playwright` recommended rules on `Financial.Web/tests/e2e/**`. Run it locally with `bash .github/scripts/test-hygiene.test.sh`, or scan a branch with `bash .github/scripts/test-hygiene.sh main HEAD`.
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
