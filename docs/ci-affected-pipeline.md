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
| Docs | `docs/`, `specs/`, `dev-util/`, `.claude/`, `.specify/`, `*.md`, `LICENSE`, `.gitignore`, `.editorconfig`, `.dockerignore` | Nothing here reaches a build |
| Contract (server side) | `Financial.Api/`, `Tests/Financial.Api.Tests/` (includes the OpenAPI snapshot) | The HTTP surface `Financial.Web` is compiled against |
| Contract (DTOs) | `Financial.*.Application/DTOs/` | Wire format for the SPA **and** linked in-process into the WPF app |
| Contract (client side) | `Financial.Web/src/api/` | Hand-written TypeScript mirror of the DTOs (`types.ts`, client) |
| WPF | `Financial.App/`, `Tests/Financial.Presentation.Tests/` | Desktop front end; nothing else depends on it |
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
| WPF | – | ✔ | – | – |
| Web | – | – | ✔ | ✔ |
| Backend core | ✔ | ✔ | – | ✔ |
| Infra / unclassified / no base commit / diff failure | ✔ | ✔ | ✔ | ✔ |
| Any push to `main` | ✔ | ✔ | ✔ | ✔ |

Jobs:

- **backend** (Windows) — builds `Financial.Api` and runs every `Tests/*.Tests.csproj` listed in `Financial.slnx` except the WPF one, with coverage and a non-blocking coverage-threshold gate (see `CLAUDE.md`).
- **wpf** (Windows) — builds `Financial.App` and runs `Financial.Presentation.Tests` + `Financial.Architecture.Tests`.
- **web** (Ubuntu) — `npm run lint`, `npm run test:coverage`, `npm run build`, with the same non-blocking coverage-threshold gate as `backend`.
- **smoke** (Ubuntu) — publishes the API with the built SPA and runs the Playwright smoke test. Runs whenever either side of the HTTP boundary changed, even when a backend/web job was skipped.
- **ci-status** — always runs; the only check branch protection should require. Passes when every job succeeded or was skipped by `changes`; fails if change detection failed or any job failed/was cancelled.

Security-relevant configuration (`Financial.Api/appsettings*.json`, `Program.cs`, auth/CORS setup) sits under `Financial.Api/` and therefore hits the Contract rule; `Dockerfile`, compose files and anything under `deploy/` hit Infra and run everything.

## Safeguards

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

## Extending

- **New shared library** (e.g. `Financial.Shared.Something/`): already covered by the `Financial.Shared.*/` pattern.
- **New bounded context** (`Financial.Foo.{Domain,Application,Infrastructure}`): covered by the `Financial.*.Domain/` family of patterns; its DTOs fall under the DTO contract rule automatically.
- **New front end or service**: add a job to `build.yml`, an output to the `changes` job, a `case` branch in `detect-changes.sh` for its paths, and list it in `ci-status`'s `needs`. Decide which contract rule(s) should also set its flag.
- **New test project**: add it to `Financial.slnx`; the backend job discovers it from there.

Test a rule change locally before pushing:

```bash
bash .github/scripts/detect-changes.sh origin/main HEAD
```
