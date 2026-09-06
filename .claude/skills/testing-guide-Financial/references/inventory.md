> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Inventory — full folder-by-folder classification (Phase 1.3, 2026-09-06)

Every top-level folder of the repo root and of every project listed in `Financial.slnx` (plus
`Financial.Web`, which is outside the solution). `Classification` is an artifact type (with
its `../artifacts/*.md` guide), "out of scope — reason", or "resolved in Phase 3 — decision".

## Repository root

| Folder | Project | Classification | Notes |
|---|---|---|---|
| `.claude/` | root | out of scope — agent/skill config | contains this skill |
| `.github/` | root | out of scope — CI config | `workflows/build.yml` and `scripts/detect-changes.sh` are referenced by `e2e-environment.md`; the shell script itself is untested |
| `.specify/` | root | out of scope — speckit templates/scripts | |
| `data/` | root | Example & seed data (`../artifacts/example-and-seed-data.md`) | only `*.example.json` tracked; live files gitignored and never touched by tests |
| `deploy/` | root | out of scope — gitignored local deployment output | `Financial.App/`, `Financial.Web/` binaries, `start-*.ps1` |
| `docs/` | root | out of scope — documentation | `docs/prd/**` §9 is the AC source (`feature-traceability.md`); `docs/rules/*.md`, `docs/ui/*.md` are the binding standards |
| `scripts/` | root | out of scope — PowerShell deploy tooling, git hooks | |
| `specs/` | root | out of scope — speckit feature specs | secondary AC source (Given/When/Then), noted in `feature-traceability.md` |
| `Integrations/` | root | see per-project rows below | |
| `Tests/` | root | test projects (see below) | |
| `Tools/` | root | see per-project rows below | |
| `Financial.*` project folders | root | see per-project rows below | |
| `fix-tithe.js` (file) | root | out of scope — one-off script | |
| `docker-compose*.yml`, `Dockerfile`, `coverlet.runsettings` (files) | root | out of scope — build/deploy config | `coverlet.runsettings` exclusions are quoted in `file-conventions.md` |

## Financial.Api

| Folder | Classification | Notes |
|---|---|---|
| `Controllers/` | Controllers, middleware, API helpers (`../artifacts/api-controllers-and-middleware.md`) | 32 files |
| `Middleware/` | same | `DomainExceptionMappingMiddleware` — no suffix convention, classified by reading the file |
| `Helpers/` | same | `SyncStatusResolver` |
| `DTOs/` | API contract snapshot (`../artifacts/api-contract-snapshot.md`) | 3 API-only DTOs; shape pinned by the snapshot |
| `Properties/` | out of scope — launch settings | |
| `Program.cs` (file) | DI modules (`../artifacts/dependency-injection-modules.md`) | composition root |

## Financial.App (WPF)

| Folder | Classification | Notes |
|---|---|---|
| `ViewModels/` | WPF ViewModels (`../artifacts/wpf-viewmodels.md`) | 108 files incl. `Admin/`, `CashFlow/`, `Investment/`, `Settings/` |
| `Converters/` | WPF converters, behaviors, helpers (`../artifacts/wpf-converters-behaviors-helpers.md`) | 18 |
| `Behaviors/` | same | 5 — no suffix; classified by reading |
| `Helpers/` | same | 4 |
| `Input/` | same | `DecimalInputHelper` |
| `Navigation/` | same | `NavTree` |
| `Views/` | WPF views, components, controls (`../artifacts/wpf-views-and-xaml.md`) | 133 XAML + code-behind |
| `Components/` | same | 14 |
| `Controls/` | same | 6 |
| `Services/` | resolved in Phase 1 — `IDialogService`/`DialogService` WPF-shell wrapper, stubbed in ViewModel tests, itself untested by design | noted in `wpf-views-and-xaml.md` |
| `Properties/` | out of scope — assembly/settings | |
| `tools/` | out of scope — `create-app-ico.ps1` build script | |
| `App.xaml.cs`, `MainWindow.xaml.cs` (files) | DI modules / views | composition root; `MainWindow` code-behind is view wiring |

## Financial.CashFlow.* and Financial.Investment.*

| Folder | Classification | Notes |
|---|---|---|
| `*.Domain/Entities/`, `ValueObjects/`, `Rules/` | Domain entities and rules (`../artifacts/domain-entities-and-rules.md`) | Investment also has `Exceptions/` (one type) |
| `*.Domain/Enums/` | out of scope — no behaviour | |
| `*.Application/Services/` | Application services (`../artifacts/application-services.md`) | 23 + 21 |
| `*.Application/Validation/` | Parsers / validators (`../artifacts/application-parsers-validators.md`) | |
| `*.Application/DTOs/`, `Configuration/` (options + DTO-like records), `Enums/` | API contract snapshot (`../artifacts/api-contract-snapshot.md`) | shape pinned by the snapshot; options binding tested in DI tests |
| `*.Application/Interfaces/` | out of scope — abstractions | implemented by Infrastructure and doubles |
| `*.Application/Exceptions/` | tested through services and middleware mapping | |
| `*.Application/DependencyInjection/` | DI modules (`../artifacts/dependency-injection-modules.md`) | |
| `*.Infrastructure/Persistence/`, `Repositories/` | JSON persistence & repositories (`../artifacts/json-persistence-and-repositories.md`) | |
| `*.Infrastructure/Services/` | External HTTP services & fetchers (`../artifacts/external-http-services.md`) | Frankfurter (CashFlow), Yahoo + fetchers (Investment) |
| `*.Infrastructure/DependencyInjection/`, `Configuration/` | DI modules | |
| `Financial.Investment.Infrastructure/DTOs/`, `Interfaces/` | tested through the services that use them | `AssetValueRequestDTO`, `IFinanceService`, `IAssetPriceFetcher` |

## Financial.Shared.*

| Folder | Classification | Notes |
|---|---|---|
| `Shared.Abstractions/Persistence/`, `Sync/`, `Resilience/`, `Configuration/` | JSON persistence & repositories | interfaces + `RetryPolicy`, `SyncStatus`, `RepositoryProviderResolver`; tested via `Shared.Infrastructure.Tests` (`SyncStatusTests`, `TransientRetryPolicyTests`) |
| `Shared.Abstractions/Observability/` | Observability (`../artifacts/observability-integration.md`) | span helpers, `TelemetryAttributeKeys`, `NoOpTelemetryTracer` |
| `Shared.Infrastructure/Persistence/`, `Resilience/`, `Hosting/` | JSON persistence & repositories | `LocalJsonStorage`, `DebouncedJsonStorage`, `RemoteJsonStorage`, factories, `TransientRetryPolicy`, `ShutdownFlushHostedService` |

## Integrations/

| Folder | Classification | Notes |
|---|---|---|
| `GoogleCore/`, `GoogleDrive/`, `GoogleSheets/` | Google SDK wrappers (`../artifacts/google-sdk-wrappers.md`) | raw clients are the documented accepted gap |
| `Observability/` | Observability (`../artifacts/observability-integration.md`) | |
| `WebPageParser/` | Web scraping parsers (`../artifacts/web-scraping-parsers.md`) | |
| `GoogleFinancialSupport/` | out of scope — stale, only `bin/`/`obj/`, not in `Financial.slnx` | safe to delete |

## Tools/

| Folder | Classification | Notes |
|---|---|---|
| `CashFlowSpreadsheetImport/` | Spreadsheet import tools (`../artifacts/spreadsheet-import-tools.md`) | 46 files; excluded from coverage gate, not from testing |
| `InvestmentSpreadsheetImport/` | same | 7 files |
| `ImportGoogleSpreadSheets/` | resolved in Phase 3 — out of scope; one-off manual WPF importer (13 files, no tests) | user decision 2026-09-06 |

## Tests/

| Folder | Classification | Notes |
|---|---|---|
| `Financial.*.Tests/` (15 projects) | test projects; layer per `file-conventions.md` | |
| `Financial.TestUtilities/` | shared doubles + seed data | not a test project; excluded from coverage |
| `Financial.Api.Tests/Contract/`, `TestData/` | API contract snapshot; Example & seed data | |
| `Financial.Architecture.Tests/Infrastructure/` | Architecture rules (`../artifacts/architecture-rule-tests.md`) | `ProjectAssembly` helper |
| `Financial.GoogleFinancialSupport.Tests/`, `Financial.CashFlowBankMigration.Tests/`, `Financial.CashFlowBankOpeningBalanceMigration.Tests/`, `Financial.CashFlowIncomeMigration.Tests/`, `Financial.CashFlowPaymentStateMigration.Tests/` | out of scope — stale, only `bin/`/`obj/`, no csproj, not in `Financial.slnx` | leftovers of the consolidated migration tools; safe to delete |
| `**/TestResults/` | out of scope — gitignored coverage output | |

## Financial.Web

| Folder | Classification | Notes |
|---|---|---|
| `src/hooks/` | React hooks (`../artifacts/react-hooks.md`) | 37 files |
| `src/components/`, `src/components/grid/` | React components (`../artifacts/react-components.md`) | 48 + 2 |
| `src/pages/` | React pages (`../artifacts/react-pages.md`) | 22 pages + `RootRedirect.tsx` |
| `src/api/` | Web API client (`../artifacts/web-api-client.md`); `generated/` + `types.ts` → API contract snapshot | |
| `src/utils/`, `src/context/`, `src/navigation/` | Web utils, context, navigation (`../artifacts/web-utils-context-navigation.md`) | |
| `src/__tests__/`, `src/**/__tests__/` | tests | |
| `src/test/`, `src/test-utils/` | test helpers (`renderWithFluent.tsx`, `selectedNodeTestWrapper.tsx`) | |
| `src/setupTests.ts`, `src/main.tsx`, `src/App.tsx` (files) | setup / entry / shell | `App.tsx` tested as a page; `main.tsx` excluded from coverage |
| `src/assets/`, `src/styles/`, `src/theme/` | out of scope — static assets, CSS, token object | |
| `scripts/` | E2E (`e2e-environment.md`) | `smoke-test.mjs` |
| `public/`, `.vite/`, `dist/` | out of scope — static/build output | |
| config files (`vite.config.ts`, `tsconfig*.json`, `eslint.config.js`, `.env*`) | out of scope — build config | quoted in `file-conventions.md` |

## Seven recurring categories (presence)

| Category | Present? | Where (see `cross-cutting-concerns.md`) |
|---|---|---|
| Time/clock/concurrency | yes | `TimeProvider` in 5 services + `DebouncedJsonStorage`; `FakeTimeProvider`, `ObservableFakeClock`, `vi.useFakeTimers` |
| UI state matrix | yes (partial assertions) | `docs/rules/ui.md`; page/component tests |
| Accessibility | yes (Unit); E2E keyboard journey missing | RTL role/name queries, `user.tab()` |
| Contract / generated-type drift | yes | `OpenApiContractTests`, `openapiFreshness.test.ts`, `tsc -b` |
| Frontend mocks own backend | yes | `vi.mock('../../api/financialApiClient')` |
| Fitness functions | yes | `Financial.Architecture.Tests` |
| Test-data contract | yes | `ExampleDataFileTests` ×2 |
