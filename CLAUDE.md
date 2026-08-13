# Project

Personal financial management tool consolidating investment transactions (Brazil + UK brokers) and household cash flow (income, expenses, bank/card balances, savings reserve, recurring bills). Single-user, self-hosted — not built to scale to multiple tenants. See `context.md` for full domain/feature context and `README.md` for setup, configuration, storage providers, and deploy tooling.

## Solution layout

The solution (`Financial.slnx`) is organized as two DDD bounded contexts plus shared/presentation layers, each split into Domain → Application → Infrastructure:

```
DDD/Investment/   Financial.Investment.{Domain,Application,Infrastructure}
DDD/CashFlow/     Financial.CashFlow.{Domain,Application,Infrastructure}
DDD/Shared/       Financial.Shared.Infrastructure   (JSON/Google Drive storage primitives shared by both contexts)
Presentation/      Financial.Api  (ASP.NET Core, serves REST API + hosts the built SPA)
                    Financial.App  (WPF desktop client)
                    Financial.Web  (React + TypeScript SPA, separate from the .slnx)
Integrations/       CashFlowSpreadsheetImport, GoogleFinancialSupport, ImportGoogleSpreadSheets, WebPageParser
Tests/              One test project per Domain/Application/Infrastructure/Presentation project
```

Dependency direction is strict: Domain has no dependencies; Application depends on Domain; Infrastructure depends on Application; Presentation (Api/App) composes both bounded contexts' Application + Infrastructure. Each layer registers itself into DI via an `Add<Context><Layer>()` extension method (e.g. `AddFinancialCashFlowApplication`, `AddFinancialInfrastructure`) called from `Financial.Api/Program.cs`. The `.claude/agents/architecture-reviewer.md` agent enforces Clean Architecture/DDD/SOLID on every change — expect layer-violation pushback if a Domain or Application project reaches into Infrastructure.

`Financial.Api` both serves the JSON API under `/api/v1/financial` and hosts the compiled React SPA from `wwwroot` (`MapFallbackToFile("index.html")`), so in production there is a single deployed process. `Financial.App` (WPF) and `Financial.Web` (React) are two independent front ends built against the same API and are expected to stay at feature parity — the WPF app is treated as the UX source of truth (see `dev-util/ai/level-web-front-end.md`).

## Persistence model

Both bounded contexts read/write a single JSON document each (`data.json` for Investment, `data-cashflow.json` for CashFlow), via `Financial.Shared.Infrastructure`. Storage provider is `LocalJson` or `GoogleDrive`, selected per-context via config (`Investment:Repository:Provider` / `CashFlow:Repository:Provider`). **The JSON file is loaded once at process startup** — after any migration or manual edit to the data file, the app/API process must be restarted (not just re-queried) for changes to take effect. Real data files (`data/data.json`, `data/data-cashflow.json`) are gitignored; only `*.example.json` templates are tracked. Never run import/migration tools against the live data file — verify against a temp copy first.

## Common commands

### .NET (API, WPF app, Integrations, all tests)

```
dotnet restore
dotnet build --configuration Release
dotnet test                                    # run all test projects
dotnet test Tests/Financial.CashFlow.Domain.Tests   # run a single test project
dotnet test --filter "FullyQualifiedName~ExpenseTests.Should_Reject_Negative_Value"  # single test
```

Tests use **xUnit** + **FluentAssertions**. Coverage collection excludes generated `obj/**` code (`coverlet.runsettings`).

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

# Architecture Rules (Mandatory)

These rules are mandatory and non-negotiable for all generated code.

## Clean Code

* Follow Clean Code principles.
* Functions must have a single responsibility.
* Avoid long methods.
* Avoid code duplication.
* Use meaningful names.
* No magic strings or magic numbers.
* Keep cyclomatic complexity low.

## SOLID

All implementations must follow SOLID principles.

* Single Responsibility Principle
* Open Closed Principle
* Liskov Substitution Principle
* Interface Segregation Principle
* Dependency Inversion Principle

## Architecture

The solution follows Clean Architecture.

Layers:

* Domain
* Application
* Infrastructure
* Presentation

Dependency direction:

Presentation -> Application -> Domain

Infrastructure implements interfaces defined by Domain or Application.

Domain must never depend on Infrastructure.

## Domain Layer

Contains:

* Entities
* Value Objects
* Domain Services
* Domain Events

Must contain no framework code.

Must contain no database code.

## Application Layer

Contains:

* Use Cases
* Commands
* Queries
* DTOs
* Validators

Coordinates business workflows.

Must not contain persistence implementation details.

## Infrastructure Layer

Contains:

* Database implementations
* External APIs
* Messaging
* File system access
* Repository implementations

Must depend on abstractions.

## Presentation Layer

Contains:

* Controllers
* Endpoints
* UI
* API Contracts

Must not contain business logic.

## Testing

Every new feature must include:

* Unit tests
* Integration tests where applicable

No feature is complete without tests.

## Before Writing Code

Always:

1. Explain where the feature belongs.
2. Identify impacted layers.
3. Explain why the design follows Clean Architecture.
4. Identify SOLID principles being applied.

## Before Finishing

Perform a self-review and verify:

* Clean Code
* SOLID
* Clean Architecture
* Test coverage
* No layer violations

If any rule is violated, stop and propose a correction.

## Definition of Done

A feature is NOT complete unless:

* Architecture reviewed.
* Clean Architecture respected.
* SOLID principles respected.
* No layer violations.
* No business logic in Presentation.
* No infrastructure concerns in Domain.
* Unit tests added.
* Integration tests added when appropriate.
* Existing tests still pass.
* New code is documented where necessary.

Before marking work as complete, provide a checklist showing compliance with all Definition of Done items.

## Application details

This is a personal project and is intended to be installed a copy for each person that will use.
Does not require to scale or should not also have many updates or changes.

It should follow all the standars above but also know that it does not OVER ENGINEERING.