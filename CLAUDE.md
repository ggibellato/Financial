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
Integrations/       GoogleCore, GoogleDrive, GoogleSheets, Observability, WebPageParser (vendor SDKs, no bounded-context types)
Tools/              CashFlowSpreadsheetImport, ImportGoogleSpreadSheets, InvestmentSpreadsheetImport
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

A coverage gate on every CI run bands the merged, whole-repo line-coverage % into
green (100%) / yellow (95–99.99%) / amber (90–94.99%) / red (<90%) for the `backend`,
`wpf`, and `web` jobs (the `Check coverage threshold` step, reading
`CoverageReport/Summary.json`'s `summary.linecoverage` on the two .NET jobs and
`coverage/coverage-summary.json`'s `total.lines.pct` on the web side). The step is
`continue-on-error: true`, so red visibly fails that step in the run but never blocks
`ci-status` or branch protection — a coverage drop is a visible signal to fix, not a
merge blocker.

`backend`'s and `wpf`'s reports are assembly-filtered so each measures only its own code:
`Financial.Architecture.Tests` project-references `Financial.App` (to check its dependency
rules) and `Financial.Presentation.Tests` loads the backend assemblies `Financial.App`
composes, so without filtering, each job's Cobertura output would include the other's
assemblies at whatever near-zero % incidental (non-behavioral) loading leaves them at,
silently distorting both numbers. `backend`'s `Publish coverage summary` step passes
`-assemblyfilters:+*;-Financial.Presentation.App` (exclude the WPF assembly, keep
everything else); `wpf`'s passes `-assemblyfilters:+Financial.Presentation.App` (keep only
the WPF assembly). Neither touches `coverlet.runsettings` — collection is unchanged;
only each job's own report aggregation is scoped.

The public API's shape is pinned by a committed OpenAPI snapshot
(`Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json`, asserted by `OpenApiContractTests`).
Domain-facing controllers return Application DTOs directly, so reshaping one is a wire-format change.
`Program.cs` registers a schema transformer that strips the spurious `["number","string"]` alternative
.NET's built-in generator adds to every decimal/int property — the wire format is always a plain JSON
number (no `JsonNumberHandling.WriteAsString` is configured anywhere), so this keeps the document
accurate rather than just permissive; `OpenApiContractTests.OpenApiDocument_NumericProperties_...`
pins that it stays stripped. When a change to the API is intended, regenerate the snapshot and review
the diff:

```powershell
$env:UPDATE_OPENAPI_SNAPSHOT=1; dotnet test Tests/Financial.Api.Tests; Remove-Item Env:\UPDATE_OPENAPI_SNAPSHOT
```

The `Remove-Item` matters: leave the variable set and every later run silently rewrites the snapshot
instead of checking it. In bash it is a one-shot prefix, so there is nothing to unset:

```bash
UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests
```

`Financial.Web/src/api/types.ts` is **not** hand-written — it's a thin layer of type aliases
(`export type ExpenseDto = Schema<'ExpenseDTO'>`, keeping the names every existing import already uses)
over `Financial.Web/src/api/generated/openapi.ts`, generated from the snapshot above via
`openapi-typescript`. After regenerating the backend snapshot, regenerate the frontend types the same
way — `cd Financial.Web && npm run generate-api-types` — and commit the result; a small handful of
frontend-only types with no backend counterpart (`SelectedNode`, `NodeType`, `InvestmentScope`) stay
hand-written above the aliases. `src/api/generated/__tests__/openapiFreshness.test.ts` (run by
`npm test`) fails if the committed generated file drifts from the snapshot, so forgetting this step is
caught in the same PR rather than at runtime. From there, `tsc -b` (part of `npm run build`, already in
the `web` CI job) is what actually tells you what to fix in the app: renaming or removing a field is a
type error at every call site that reads it, naming file:line, the same way the C# compiler does for
`Financial.App`.

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
npm run test:coverage   # vitest run --coverage — same command the web CI job's coverage gate uses
npm run smoke-test      # Playwright smoke test against a running API + web server (see .github/workflows/build.yml)
npm run generate-api-types  # regenerate src/api/generated/openapi.ts from the OpenAPI snapshot; commit the result
```

`API_BASE_URL` is read from `.env` (non-`VITE_`-prefixed, so it's explicitly wired into `vite.config.ts` via `define`) and must always be a relative path in Docker/production (`/api/v1/financial`) — never empty, or the SPA fallback route returns HTML instead of JSON for API calls.

### Docker

```
docker-compose up
```
Builds the React SPA and the API into a single image (`Dockerfile`), API serves both from port 8080. Volumes mount `./data` for the JSON files.

## CI

`.github/workflows/build.yml` runs only the jobs a change can affect: a `changes` job classifies the
diff with `.github/scripts/detect-changes.sh`, then `backend` (Windows, API build + all non-WPF tests
with coverage), `wpf` (Windows), `web` (Ubuntu lint+test+build) and `smoke` (publishes the full app
against seeded test JSON and runs the Playwright smoke test) run conditionally. Docs-only changes run
nothing; unknown paths or a missing base commit run everything, and every push to `main` always runs the
full pipeline. `ci-status` is the single required
check and passes when every job succeeded or was skipped. Rules and extension steps are in
`docs/ci-affected-pipeline.md`. PR titles are enforced as Conventional Commits
(`feat|fix|docs|chore|refactor|test|perf|ci|build`) by `semantic-pr.yml`.

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

# UI / UX invariants

These bind whenever a change affects `Financial.Web`, `Financial.App`, shared
presentation components, user-facing API error/validation contracts, or a
workflow visible in either front end.

1. **React-led UX with equivalent WPF outcomes.** `Financial.Web` (React) is
   the UX source of truth. Define, validate, and improve intended user workflows
   in React first. `Financial.App` (WPF) must provide equivalent user tasks,
   terminology, information hierarchy, field order, validation, action priority,
   status meaning, financial formatting, and outcomes. Equivalent does not mean
   identical controls, markup, or desktop interaction mechanics. Platform-native
   adaptation is allowed only when it preserves workflow meaning and usability.

2. **Standards stack.** Use Microsoft Fluent 2 as the primary visual and
   component design system; WCAG 2.2 AA as the accessibility baseline where
   applicable; and Nielsen Norman Group usability heuristics to evaluate
   workflow quality. Product/domain decisions in `docs/ui/` override general
   design guidance. Accessibility, security, and confirmed product requirements
   override visual preference and screenshot references.

3. **Task-first financial UX.** Optimize for fast, accurate, low-friction
   single-user financial workflows. Preserve context, prevent accidental data
   loss, make loading/saving/errors visible, keep financial values and totals
   unambiguous, and support dense data views without sacrificing readability.

4. **No competing UI systems.** Reuse established React and WPF components,
   themes, tokens, and resource systems. Do not add Material, Bootstrap, Ant
   Design, Carbon, another WPF UI framework, or another styling system without
   explicit approval and an ADR.

5. **Mandatory state design.** Every applicable user-facing change defines
   initial, loading, empty, validation, server-error, saving/progress, success,
   disabled, and unsaved-changes behavior. Do not implement only the happy path.

6. **Accessibility is implementation work.** UI changes must be keyboard
   operable, show visible focus, have accessible names and labels, avoid
   color-only meaning, support zoom/text scaling, and provide accessible
   equivalents for important charts and status information.

# Comments

**Priority rule:** never remove a comment used by Swagger or any other tooling. It outranks every removal criterion below.

**Default stance:** do not add comments.

- Prefer self-explanatory code: clear names, small functions, explicit types.
- Only add a comment if:
  - It is required by tooling (e.g., Swagger XML comments), OR
  - It documents a non-obvious business rule / constraint that cannot be expressed in the code, OR
  - It records a critical workaround or historical reason that would otherwise be impossible to infer.

**Never add comments that:**

- Restate what the code already says.
- Explain how something will be used elsewhere (that belongs in the caller or in docs, not inline).
- Describe obvious implementation details.

When editing existing code:

- Do not introduce new comments unless one of the allowed cases above applies.
- If an existing comment is redundant or obvious, you may remove it (as long as it’s not used by tooling).

# Rule files

Mandatory, not advisory. Read the file for the stage you are in **before producing output** — do not work from memory of it.

| When you are… | Read |
|---|---|
| Writing a PRD, spec or plan; deciding where a feature goes or how to slice it | `docs/rules/design.md` |
| Writing or changing any source file | `docs/rules/implementation.md` |
| Writing or changing tests | `testing-guide-Financial` skill, then `docs/rules/implementation.md` §Tests |
| Finishing a change | `docs/rules/implementation.md` §Definition of Done |
| Designing, changing, reviewing, or refactoring any user-facing UI/workflow | `docs/rules/ui.md`, then the relevant documents in `docs/ui/`; use the `fluent-ui` skill for significant UI work |
| Finishing a UI-affecting change | `docs/ui/review-checklist.md`, then `docs/rules/implementation.md` §Definition of Done |

`.claude/agents/architecture-reviewer.md` reviews every change against these files.
