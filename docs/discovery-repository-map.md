# Repository Map — High-Level Survey

Read-only software-archaeology pass over the existing (brownfield) codebase, produced as the first step of introducing Spec-Driven Development (SDD). Captures the repository's structure and major project responsibilities as they exist today — not an ideal or recommended structure.

Every conclusion below is classified:

- **CONFIRMED** — directly supported by repository evidence.
- **INFERRED** — a reasonable interpretation of the implementation, not explicitly documented.
- **UNKNOWN** — the repository does not provide enough information to determine this.

This file reflects the corrected state after human review — see "Corrections applied during review" at the end.

## Solution structure

**CONFIRMED** — `Financial.slnx` (new-style .slnx solution format) organizes projects into 5 solution folders: `/DDD/Investment/`, `/DDD/CashFlow/`, `/DDD/Shared/`, `/Integrations/`, `/Tools/`, `/Tests/`, `/Presentation/`. This is a solution-folder view only — actual project files sit flat at repo root, per `CLAUDE.md`.

## Bounded contexts (DDD)

**CONFIRMED** — Two independent bounded contexts, each split Domain → Application → Infrastructure:

- **Investment**: `Financial.Investment.Domain`, `.Application`, `.Infrastructure`
- **CashFlow**: `Financial.CashFlow.Domain`, `.Application`, `.Infrastructure`
- **Shared**: `Financial.Shared.Infrastructure` (JSON persistence primitives shared by both)

**CONFIRMED** (from `context.md`) — The two domains are explicitly documented as having no cross-domain references; each owns its own JSON data file and repository configuration.

## Presentation layer

**CONFIRMED**:

- `Financial.Api` — ASP.NET Core, serves REST API under `/api/v1/financial` and hosts the built React SPA (`MapFallbackToFile`) — single deployed process in Docker.
- `Financial.App` — WPF desktop client, and a **client for both bounded contexts**, not Investment-only. `Financial.App/Views/CashFlow/` and `Financial.App/ViewModels/CashFlow/` contain a full parallel implementation (banks, cards, reserve, controle mãe, annual summary, bills, income splits, transfers, etc.), alongside the Investment-side UI.
- `Financial.Web` — React + TypeScript SPA (Vite), not part of the `.slnx`. Has both Investment pages (`ActiveInvestmentsPage`, `AnnualSummaryPage`, `PortfolioSummaryTab`, etc.) and CashFlow-only pages (`MonthlyPage`, `ReservaPage`, `ControleMaePage`, `MensaisPage`).

Note: `context.md` previously stated CashFlow was "Implemented as React pages only... not available in the WPF app." That line was stale/incorrect — contradicted directly by the repository — and has since been removed from `context.md`.

## Integrations & Tools

**CONFIRMED** — Two categories with different intents:

- `Integrations/GoogleFinancialSupport` and `Integrations/WebPageParser` — live, referenced in `.slnx`. Fetch/parse external asset price & classification data (Google Finance scraping, asset classification lookups).
- `Tools/CashFlowSpreadsheetImport` and `Tools/ImportGoogleSpreadSheets` — one-off migration utilities (per `README.md`), also referenced in `.slnx`.

A set of orphaned directories was found during this pass and has since been removed: `Integrations/CashFlowBankMigration`, `CashFlowBankOpeningBalanceMigration`, `CashFlowIncomeMigration`, `CashFlowPaymentStateMigration`, and `Integrations/FinancialToolSupport` contained only stale `bin`/`obj` build output with no source files and were not referenced anywhere in `.slnx` or any `.csproj` — leftovers from an earlier consolidation of 5 migration tools into the single `CashFlowSpreadsheetImport` tool. `Integrations/` now contains only `GoogleFinancialSupport` and `WebPageParser`, matching `.slnx`.

## Tests

**CONFIRMED** — One test project per layer per context (`Financial.CashFlow.Domain.Tests`, `.Application.Tests`, `.Infrastructure.Tests`, same for Investment), plus `Financial.Api.Tests`, `Financial.Presentation.Tests`, `Financial.Architecture.Tests` (layer-boundary enforcement), `Financial.Shared.Infrastructure.Tests`, `Financial.CashFlowSpreadsheetImport.Tests`, and a shared `Financial.TestUtilities` project.

The four orphaned test folders corresponding to the removed migration tools (`Financial.CashFlowBankMigration.Tests`, `.CashFlowBankOpeningBalanceMigration.Tests`, `.CashFlowIncomeMigration.Tests`, `.CashFlowPaymentStateMigration.Tests`) were likewise not referenced in `.slnx` and have since been removed.

## Persistence

**CONFIRMED** — No database. Each bounded context persists to a single JSON document (`data/data-investment.json`, `data/data-cashflow.json`), loaded once at process startup, via `LocalJson` or `GoogleDrive` provider (config-selected per context). Only `*.example.json` templates are tracked in git.

## Configuration / deployment

**CONFIRMED**:

- `Dockerfile` + `docker-compose.yml` — single-image build serving API + SPA on port 8080.
- `.github/workflows/build.yml` — three CI jobs: .NET build+test (Windows), web lint+test+build (Ubuntu/Node 24), Playwright browser smoke test.
- `.github/workflows/semantic-pr.yml` — enforces Conventional Commit PR titles.
- `scripts/deploy.ps1` + `deploy/` — manual, local-only, non-CI publish tooling to a git-ignored `deploy/` folder, targeting `GoogleDrive` storage.
- `.vscode/tasks.json`, `scripts/hooks`, `scripts/update-main.ps1`, `scripts/delete-branches.ps1` — local dev-workflow helper scripts.

## Documentation

**CONFIRMED** — `context.md` (domain/feature narrative source of truth), `README.md` (setup/config), `docs/prd/P01`–`P33` (33 sequential PRD folders, one per shipped feature — evidence of an existing informal spec-driven history predating this SDD initiative), `docs/app-comments-update.md`, `docs/app-todo.md`, `docs/app-workflow.md`, `dev-util/ai/level-web-front-end.md` (WPF-as-source-of-truth parity doc) + `wpf-screenshots`.

## SDD scaffolding already present

**CONFIRMED** — `.specify/` directory and `.claude/skills/speckit-*` were already scaffolded into the working tree ahead of this discovery pass, i.e. the Spec-Kit tooling had been set up but not yet exercised against this codebase.

## Corrections applied during review

This file reflects two corrections made by human review of the initial draft:

1. **Financial.App scope** — originally reported as Investment-only; corrected to reflect it as a client for both bounded contexts (verified via `Financial.App/Views/CashFlow/` and `Financial.App/ViewModels/CashFlow/`).
2. **`context.md:77`** — the stale "CashFlow: React pages only, not available in the WPF app" line was identified as contradicted by the repository and has been removed from `context.md` by the user.

Also reflects two cleanup actions taken by the user during review:

3. Orphaned `Integrations/` directories (build artifacts only, unreferenced by `.slnx`) removed.
4. Orphaned `Tests/` folders corresponding to those same removed projects (unreferenced by `.slnx`) removed.
