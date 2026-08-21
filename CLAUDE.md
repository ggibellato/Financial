# Project

Personal financial management tool consolidating investment transactions (Brazil + UK brokers) and household cash flow (income, expenses, bank/card balances, savings reserve, recurring bills). Single-user, self-hosted — not built to scale to multiple tenants. See `context.md` for full domain/feature context and `README.md` for setup, configuration, storage providers, and deploy tooling.

## Solution layout

The solution (`Financial.slnx`) is organized as two DDD bounded contexts plus shared/presentation layers, each split into Domain → Application → Infrastructure. Project folders sit flat at the repo root (no `DDD/`/`Presentation/` grouping folders):

```
Financial.Investment.{Domain,Application,Infrastructure}   Investment bounded context
Financial.CashFlow.{Domain,Application,Infrastructure}     CashFlow bounded context
Financial.Shared.Infrastructure                             JSON/Google Drive storage primitives shared by both contexts
Financial.Api                                                ASP.NET Core, serves REST API + hosts the built SPA
Financial.App                                                WPF desktop client
Financial.Web                                                React + TypeScript SPA, separate from the .slnx
Integrations/       CashFlowSpreadsheetImport, GoogleFinancialSupport, ImportGoogleSpreadSheets, WebPageParser
Tests/              One test project per Domain/Application/Infrastructure/Presentation project
```

Dependency direction is strict: Domain has no dependencies; Application depends on Domain; Infrastructure depends on Application; Presentation (Api/App) composes both bounded contexts' Application + Infrastructure. Each layer registers itself into DI via an `Add<Context><Layer>()` extension method (e.g. `AddFinancialCashFlowApplication`, `AddFinancialInfrastructure`) called from `Financial.Api/Program.cs`. The `.claude/agents/architecture-reviewer.md` agent enforces Clean Architecture/DDD/SOLID on every change — expect layer-violation pushback if a Domain or Application project reaches into Infrastructure.

`Financial.Api` both serves the JSON API under `/api/v1/financial` and hosts the compiled React SPA from `wwwroot` (`MapFallbackToFile("index.html")`), so in production there is a single deployed process. `Financial.App` (WPF) and `Financial.Web` (React) are two independent front ends built against the same API and are expected to stay at feature parity — the WPF app is treated as the UX source of truth (see `dev-util/ai/level-web-front-end.md`).

## Persistence model

Both bounded contexts read/write a single JSON document each (`data-investment.json` for Investment, `data-cashflow.json` for CashFlow), via `Financial.Shared.Infrastructure`. Storage provider is `LocalJson` or `GoogleDrive`, selected per-context via config (`Investment:Repository:Provider` / `CashFlow:Repository:Provider`). **The JSON file is loaded once at process startup** — after any migration or manual edit to the data file, the app/API process must be restarted (not just re-queried) for changes to take effect. Real data files (`data/data-investment.json`, `data/data-cashflow.json`) are gitignored; only `*.example.json` templates are tracked. Never run import/migration tools against the live data file — verify against a temp copy first.

## Common commands

### .NET (API, WPF app, Integrations, all tests)

```
dotnet restore
dotnet build --configuration Release
dotnet test                                    # run all test projects
dotnet test Tests/Financial.CashFlow.Domain.Tests   # run a single test project
dotnet test --filter "FullyQualifiedName~ExpenseTests.Should_Reject_Negative_Value"  # single test
```

Tests use **xUnit** + **FluentAssertions**.

Coverage is collected on every CI run and published as a summary table on the workflow run page
(`.github/workflows/build.yml`, the `Publish coverage summary` step). `coverlet.runsettings` declares the
`XPlat code coverage` collector and its one exclusion, generated `obj/**` code — passing `--settings` is
what turns collection on, so there is no `--collect` argument anywhere. To reproduce it locally:

```
dotnet test --settings coverlet.runsettings --results-directory TestResults
```

No coverage threshold is enforced; a drop never fails the build.

The public API's shape is pinned by a committed OpenAPI snapshot
(`Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json`, asserted by `OpenApiContractTests`).
Domain-facing controllers return Application DTOs directly, so reshaping one is a wire-format change
for `Financial.Web` — whose `types.ts` is hand-written and will not fail to compile against it. When a
change to the API is intended, regenerate the snapshot and review the diff:

```powershell
$env:UPDATE_OPENAPI_SNAPSHOT=1; dotnet test Tests/Financial.Api.Tests; Remove-Item Env:\UPDATE_OPENAPI_SNAPSHOT
```

The `Remove-Item` matters: leave the variable set and every later run silently rewrites the snapshot
instead of checking it. In bash it is a one-shot prefix, so there is nothing to unset:

```bash
UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests
```

Anything the diff shows as removed or renamed is a breaking change; update `types.ts` in the same PR.

Run the API locally (serves API only, expects the SPA dev server separately):
```
dotnet run --project Financial.Api
```
Dev config points `Investment:DataJsonFile` / `CashFlow:DataJsonFile` at `../../../../data/*.json` (repo-root `data/`) and CORS allows `localhost:5173`/`5174`. Do not run against `.env` values or ports that overlap a live Docker deployment (default Docker port is 8080) — check `netstat` before smoke-testing locally.

### Financial.Web (React + TypeScript + Vite)

```
cd Financial.Web
npm install
npm run dev            # Vite dev server on 5173, proxies /api to localhost:5190
npm run lint
npm run build           # tsc -b && vite build — run this (not just vitest) to catch type errors
npm test                 # vitest run
npm run test:watch
npm run smoke-test      # Playwright smoke test against a running API + web server (see .github/workflows/build.yml)
```

`API_BASE_URL` is read from `.env` (non-`VITE_`-prefixed, so it's explicitly wired into `vite.config.ts` via `define`) and must always be a relative path in Docker/production (`/api/v1/financial`) — never empty, or the SPA fallback route returns HTML instead of JSON for API calls.

### Docker

```
docker-compose up
```
Builds the React SPA and the API into a single image (`Dockerfile`), API serves both from port 8080. Volumes mount `./data` for the JSON files.

## CI

`.github/workflows/build.yml` runs three jobs on every PR: `.NET build+test` (Windows), `web lint+test+build` (Ubuntu/Node 24), and a `browser-smoke-test` job that publishes the full app, boots it against seeded test JSON data, and runs the Playwright smoke test end to end. PR titles are enforced as Conventional Commits (`feat|fix|docs|chore|refactor|test|perf|ci|build`) by `semantic-pr.yml`.

# GIT Policy

**Commit (conventional format):**
```bash
git commit -m "short description

Detailed description if there are many files or changes"
```

**Never:**
- Update git config
- Run destructive commands (force push, hard reset) without explicit request
- Skip hookd (--no-verify) unless requested
- Use `git commit --amend` unles explicity safe
- Force push to main/master

# Architecture invariants

These bind at every stage — discovery, design, implementation, review. Everything else is in the rule files below.

1. **Dependency direction.** Presentation → Application → Domain. Domain depends on nothing. Infrastructure implements interfaces declared by Domain or Application. Domain must never reference Infrastructure.

2. **Where code lives.**

   | Layer | Owns | Never contains |
   |---|---|---|
   | Domain | Entities, value objects, domain rules, domain events | Framework code, persistence, I/O |
   | Application | Use cases, services, commands/queries, DTOs, validators | Persistence implementation details |
   | Infrastructure | Repositories, storage, external APIs, file system access | Business logic |
   | Presentation | Controllers, endpoints, API contracts, UI | Business logic |

3. **Bounded contexts stay isolated.** Investment and CashFlow never reference each other; anything genuinely shared goes in `Financial.Shared.*`.

4. **Vertical slices only.** Every change ships as a complete working increment — implementation, configuration, tests and docs together. Never scaffolding, placeholders, or disconnected infrastructure.

5. **`main` is always deployable.** After every merge it builds, passes the required tests, and starts under `docker-compose up` with existing functionality intact.

6. **Right-sized, not over-engineered.** Single-user, self-hosted, one install per person. Follow the standards; don't build for scale that will never arrive.

# Rule files

Mandatory, not advisory. Read the file for the stage you are in **before producing output** — do not work from memory of it.

| When you are… | Read |
|---|---|
| Writing a PRD, spec or plan; deciding where a feature goes or how to slice it | `docs/rules/design.md` |
| Writing or changing any source file | `docs/rules/implementation.md` |
| Writing or changing tests | `testing-guide-Financial` skill, then `docs/rules/implementation.md` §Tests |
| Finishing a change | `docs/rules/implementation.md` §Definition of Done |

`.claude/agents/architecture-reviewer.md` reviews every change against these files.
