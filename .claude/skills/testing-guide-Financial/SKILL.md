---
name: testing-guide-Financial
description: >
  Testing guide for Financial. Reference this skill when planning features,
  implementing code, creating tests, or reviewing changes in Financial. Covers
  what to test, at which of three layers (Unit, Integration — including
  mandatory AC-tracing tests, E2E), and how to set up each test — organized by
  artifact type.
  Triggers on: planning Financial features, implementing Financial features,
  writing tests for Financial, reviewing Financial code, reviewing Financial tests,
  what should I test in Financial, how to test Financial, Financial test guide.
---

## 0. Purpose

This guide helps you decide **what to test**, at **which of three layers** — Unit,
Integration (intra-domain), or E2E (System) — and **how to set up tests** for each
type of artifact and each feature in `Financial`. Integration carries a mandatory,
explicitly-tagged subset of tests that trace to acceptance criteria — see
`references/feature-traceability.md`. Every layer also requires negative-path
coverage, not just the happy path — see `references/negative-path-testing.md`.
When working on a specific artifact type, read the corresponding guide in
`artifacts/`. Supporting references are in `references/`.

This revision replaces the previous guide, which called the in-process `WebApplicationFactory` suite "E2E"; here that suite is Integration and E2E is the Playwright smoke run (§1, §6).

## 1. Testability Foundations

- **External providers vs owned infrastructure, concretely.** External (faked above Unit):
  the Frankfurter exchange-rate API (`FrankfurterExchangeRateProvider`), Yahoo Finance
  (`YahooFinanceService`), the scraped Google Finance / StatusInvest / DadosMercado pages
  (`Integrations/WebPageParser`), the Google Drive and Sheets APIs, and the OTLP collector.
  Owned (real at Integration and E2E): the JSON data files through `LocalJsonStorage` (always a
  `Guid`-named temp copy, never `data/*.json`), both contexts' Application and Infrastructure
  graphs, the real DI container, and the published `Financial.Api` process
  (`references/external-providers.md`).
- **The mock boundary in framework terms.** A backend Integration test derives from
  `ApiEndpointTests` (`Tests/Financial.Api.Tests/ApiEndpointTests.cs`), which owns an
  `ApiTestFactory : WebApplicationFactory<Program>` pointed at temp JSON files. The only things
  `ConfigureTestServices` replaces are `IExchangeRateProvider` (external) and `TimeProvider`
  (non-determinism); every service, repository and serializer is real. A standalone
  Infrastructure Integration test wires
  `new CashFlowJsonRepository(data, new LocalJsonStorage(tempPath), new CashFlowSerializerAdapter())`
  with no stub in sight. `Financial.Web` is a separate deployable reached over HTTP, so its own
  Integration tests fake the API with `vi.mock('../../api/financialApiClient')` — the sanctioned
  exception in `references/mock-health-rules.md`. `Financial.App` (WPF) composes the backend
  in-process in `App.xaml.cs`, so WPF Integration tests wire real services, not a fake API.
- **Integration's dual role.** Contract-proving: `CashFlowJsonRepositoryTests.ApplyAndSaveAsync_WritesSerializedDataThroughStorage`
  proves repository ↔ `LocalJsonStorage` ↔ `CashFlowSerializerAdapter` round-trip. AC-tracing
  (none yet — the first follows this shape): a test under `Tests/Financial.Api.Tests/Acceptance/`
  tagged `[Trait("AC", "P42-F01-payments-due-aggregation-backend-08")]` posts through the real
  host and asserts overdue payments are excluded. Same base class, temp files and fakes; only
  the tag and the assertion differ.
- **The E2E environment.** The CI `smoke` job in `.github/workflows/build.yml` publishes the API,
  embeds the built SPA in `wwwroot`, seeds `Tests/Financial.Api.Tests/TestData/*.test.json`,
  starts `dotnet Financial.Api.dll` on port 8080 and runs `Financial.Web/scripts/smoke-test.mjs`
  in Chromium. Two real processes, real HTTP, real JSON files — that is E2E here. Reproduce the
  same steps locally on a free port. WPF has no E2E harness. See `references/e2e-environment.md`.
- **Why DI tests exist, and where.** `AddFinancialCashFlowInfrastructure`, `AddFinancialInfrastructure`,
  `AddObservability` and `AddGoogleDriveFileClient` register into a real `ServiceCollection`; a
  missing registration or an unsupported `CashFlow:Repository:Provider` only fails at
  `GetRequiredService<T>()` time — Integration (`artifacts/dependency-injection-modules.md`).
- **Version-specific behaviour.** xUnit 2.9.3 on VSTest: `dotnet test --filter` supports `AC=` /
  `AC~` on traits and `--list-tests` dry-runs a filter. Vitest 4.1's `--tagsFilter` refuses any
  tag not declared in `test.tags`, which is why Web AC tags live in the test name. Fluent UI v9
  under Vitest needs the two `vi.mock` shims already in `src/setupTests.ts`; tabster focus is
  unreliable in jsdom. WPF ViewModels run on MTA xUnit threads with no `Dispatcher`.
- **Negative paths, concretely.** Unit: `ExpenseServiceTests.AddExpenseAsync_WithZeroValue_RecordsFailedSpanWithException`.
  Integration, provider failure: `FrankfurterExchangeRateProviderTests.GetHistoricalRateAsync_WhenHttpRequestThrows_ReturnsNull`.
  Integration, rejected write: `CashFlowJsonRepositoryTests.ApplyAndSaveAsync_WhenWriteFails_PropagatesException`.
  E2E: the smoke script has no failure journey yet; the next journey added must be a rejected
  submission surfaced in the UI — see `references/negative-path-testing.md`.
- **Binding rules from `docs/rules/implementation.md` §Tests.** Shared doubles live only in
  `Tests/Financial.TestUtilities` (`StubCashFlowRepository`, `StubInvestmentRepository`,
  `RecordingTelemetryTracer`, `RecordingLogger<T>`, `FakeTimeProvider`, `TestDataPaths`) — never a
  local copy. API tests derive from `ApiEndpointTests`; never `new ApiTestFactory()` in a test
  class. Elsewhere: constructor + `Create*` helper with optional parameters; recorders are
  instance fields, never `static`. Prove failure paths with the recorders and assert the
  exception **type** is logged, never a message or a value. No mocking framework exists (no
  Moq/NSubstitute in any csproj, no MSW in `package.json`); stubs are hand-written.

## 2. Testing Criteria

Worth testing in this codebase:
- **Every acceptance criterion in `docs/prd/P{NN}-prd-{slug}/prd-{slug}.md` §9**, one tagged
  Integration test each, including the negative bullets ("… are excluded", "fails silently") —
  `references/feature-traceability.md`.
- **The failure side of every branch, provider call and storage write** — a
  `FrankfurterExchangeRateProvider` test for non-2xx, malformed body and transport exception; a
  repository test for an unwritable path — `references/negative-path-testing.md`.
- Domain entity invariants and `Rules/` calculators (`Expense.SetRoundUpAmount` bounds,
  `XirrCalculator.Calculate`, `TitheRule`) → Unit.
- Application service branching and the span/log contract (`StartSpan` / `MarkFailed`) → Unit
  with `StubCashFlowRepository` + `RecordingTelemetryTracer`.
- Parsers/validators in `Validation/` (`CurrencyParser`, `EntityIdResolver`) → Unit, every
  malformed input.
- Repository ↔ `LocalJsonStorage` ↔ serializer round-trips, `DebouncedJsonStorage` timing with
  a fake clock, reference converters → Integration.
- DI extension methods resolving the real container, including the unsupported-provider branch.
- Controller status codes and `DomainExceptionMappingMiddleware` mappings through the real host.
- The OpenAPI snapshot and generated `openapi.ts` staying in sync (`OpenApiContractTests`,
  `openapiFreshness.test.ts`) → Integration.
- Dependency-direction rules (`Tests/Financial.Architecture.Tests`) and example data files
  (`ExampleDataFileTests`) → Integration.
- WPF ViewModel state, commands, validation and span recording → Unit; XAML column bindings
  against DTO properties → contract test.
- React hooks' loading/error/retry states, components' roles and keyboard handlers, pages' full
  state matrix with the API client faked.

NOT worth testing here:
- Controllers' business behaviour in isolation — only constructor/`[FromBody]` null guards
  (`ControllerGuardClauseTests`); everything else goes through the host.
- Raw Google SDK clients (accepted gap, `artifacts/google-sdk-wrappers.md`); live CSS selectors
  in `GoogleFinance.cs` / `StatusInvest.cs` (manual `[Fact(Skip = …)]` runs).
- DTO shape by hand (the OpenAPI snapshot pins it); `Tools/ImportGoogleSpreadSheets` (one-off
  importer, out of scope by decision); framework behaviour (WPF binding engine, react-router,
  ASP.NET model binding, `System.Text.Json`).

## 3. Feature Implementation Checklist

When implementing or changing a feature, walk this checklist. Every feature gets an
AC-tracing Integration row; every artifact you create or modify gets its own row.

| Created/modified | Required tests | Guide |
|---|---|---|
| A feature (new or changed AC) | Integration (AC-tracing): one tagged test per §9 bullet incl. negative ones, real host/graph, providers faked | `references/feature-traceability.md` |
| Domain entity / value object / `Rules/` calculator | Unit: each invariant, each rejected input, boundary values | `artifacts/domain-entities-and-rules.md` |
| Application service (`*Service.cs`) | Unit: each branch + failed-span/log assertion (stub repo); Integration: through the API host or WPF graph | `artifacts/application-services.md` |
| Parser / validator (`Validation/`) | Unit: every accepted form and every malformed input | `artifacts/application-parsers-validators.md` |
| `Add*` DI extension or composition-root registration | Integration: real container resolves, unsupported config throws | `artifacts/dependency-injection-modules.md` |
| Repository, serializer, converter, storage | Integration: temp-file round-trip + rejected write + fake-clock timing | `artifacts/json-persistence-and-repositories.md` |
| HTTP-backed provider / price fetcher | Integration: fake `HttpMessageHandler` success, non-2xx, malformed, thrown; Unit for fetchers over `IFinanceService` | `artifacts/external-http-services.md` |
| Scraper parsing function | Unit: parsing branches; live selectors stay `Skip` | `artifacts/web-scraping-parsers.md` |
| Google SDK wrapper / retry / translator | Unit for pure policy code; DI Integration for registration | `artifacts/google-sdk-wrappers.md` |
| Observability wiring | Integration: options binding, tracer registration, unreachable collector | `artifacts/observability-integration.md` |
| Controller / middleware / API helper | Integration via `ApiEndpointTests`: status codes, problem details, logging redaction | `artifacts/api-controllers-and-middleware.md` |
| Any DTO or endpoint shape change | Integration: regenerate snapshot + `npm run generate-api-types` + `tsc -b` | `artifacts/api-contract-snapshot.md` |
| New project or cross-context reference | Integration: architecture rule test | `artifacts/architecture-rule-tests.md` |
| `data/*.example.json` or `TestData/*.test.json` | Integration: real loader still parses | `artifacts/example-and-seed-data.md` |
| WPF ViewModel / `*FormValidation` | Unit: state, commands, validation, span; select via `TreeNodeViewModel.IsSelected` | `artifacts/wpf-viewmodels.md` |
| WPF converter / behavior / helper | Unit: each branch incl. null/unset input | `artifacts/wpf-converters-behaviors-helpers.md` |
| WPF view / XAML / component | Binding-contract test for grids; manual run per `docs/rules/ui.md` | `artifacts/wpf-views-and-xaml.md` |
| Spreadsheet import / migration tool | Unit for resolvers; Integration on temp JSON + in-memory workbook | `artifacts/spreadsheet-import-tools.md` |
| React hook | Unit: loading, success, error, retry with `apiClient` faked | `artifacts/react-hooks.md` |
| React component | Unit: roles, labels, keyboard, each visual state | `artifacts/react-components.md` |
| React page | Integration (frontend): full state matrix with the API faked | `artifacts/react-pages.md` |
| `financialApiClient.ts` method | Unit: URL/method/body + `ApiError` on non-2xx | `artifacts/web-api-client.md` |
| Web util / context / navigation | Unit; route ↔ sidebar agreement test | `artifacts/web-utils-context-navigation.md` |
| Cross-process journey (Web ↔ API) | E2E: extend the smoke script incl. one rejected journey | `references/e2e-environment.md` |

**How to use:** after implementing a feature, first add its AC-tracing Integration row, then
walk every artifact row for anything you created or modified — for each row, confirm both the
success case and its failure/negative case are covered. Skip rows that don't apply.

## 4. Artifact Type Quick Reference

When creating or modifying an artifact — or implementing a feature — read the
corresponding guide for the complete recipe.

| Artifact Type | Pattern | Layer(s) | Guide |
|---|---|---|---|
| Feature AC coverage | `docs/prd/P{NN}-prd-{slug}/prd-{slug}.md` §9 | Integration (AC-tracing) | `references/feature-traceability.md` |
| Domain entities, value objects, rules | `Financial.*.Domain/{Entities,ValueObjects,Rules}/*.cs` | Unit | `artifacts/domain-entities-and-rules.md` |
| Application services | `Financial.*.Application/Services/*Service.cs` | Unit + Integration | `artifacts/application-services.md` |
| Parsers / validators | `Financial.*.Application/Validation/*.cs` | Unit | `artifacts/application-parsers-validators.md` |
| DI modules | `*/DependencyInjection/*ServiceCollectionExtensions.cs`, `Program.cs`, `App.xaml.cs` | Integration | `artifacts/dependency-injection-modules.md` |
| JSON persistence & repositories | `*/Persistence/*.cs`, `*/Repositories/*.cs`, `Financial.Shared.Infrastructure/Persistence/*.cs` | Integration (+ Unit for converters) | `artifacts/json-persistence-and-repositories.md` |
| External HTTP services & fetchers | `Financial.*.Infrastructure/Services/*.cs` | Integration (fake handler) + Unit | `artifacts/external-http-services.md` |
| Web scraping parsers | `Integrations/WebPageParser/*.cs` | Unit (+ manual live) | `artifacts/web-scraping-parsers.md` |
| Google SDK wrappers | `Integrations/Google{Core,Drive,Sheets}/*.cs` | Unit + Integration (DI) | `artifacts/google-sdk-wrappers.md` |
| Observability | `Integrations/Observability/*.cs` | Integration | `artifacts/observability-integration.md` |
| Controllers, middleware, API helpers | `Financial.Api/{Controllers,Middleware,Helpers}/*.cs` | Integration (+ Unit guards) | `artifacts/api-controllers-and-middleware.md` |
| API contract snapshot & generated types | `Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json`, `Financial.Web/src/api/generated/openapi.ts` | Integration | `artifacts/api-contract-snapshot.md` |
| Architecture rules | `Tests/Financial.Architecture.Tests/*RuleTests.cs` | Integration | `artifacts/architecture-rule-tests.md` |
| Example & seed data | `data/*.example.json`, `Tests/**/TestData/*.test.json` | Integration | `artifacts/example-and-seed-data.md` |
| WPF ViewModels | `Financial.App/ViewModels/**/*.cs` | Unit (+ Integration for AC) | `artifacts/wpf-viewmodels.md` |
| WPF converters, behaviors, helpers | `Financial.App/{Converters,Behaviors,Helpers,Input,Navigation}/*.cs` | Unit | `artifacts/wpf-converters-behaviors-helpers.md` |
| WPF views, components, controls | `Financial.App/{Views,Components,Controls}/**/*.xaml` | Contract (binding) + manual | `artifacts/wpf-views-and-xaml.md` |
| Spreadsheet import tools | `Tools/{CashFlowSpreadsheetImport,InvestmentSpreadsheetImport}/**/*.cs` | Unit + Integration | `artifacts/spreadsheet-import-tools.md` |
| React hooks | `Financial.Web/src/hooks/use*.ts` | Unit | `artifacts/react-hooks.md` |
| React components | `Financial.Web/src/components/**/*.tsx` | Unit | `artifacts/react-components.md` |
| React pages | `Financial.Web/src/pages/*Page.tsx` | Integration (frontend) | `artifacts/react-pages.md` |
| Web API client | `Financial.Web/src/api/financialApiClient.ts` | Unit | `artifacts/web-api-client.md` |
| Web utils, context, navigation | `Financial.Web/src/{utils,context,navigation}/*.ts*` | Unit | `artifacts/web-utils-context-navigation.md` |
| Critical cross-process journeys | `Financial.Web/scripts/smoke-test.mjs` | E2E | `references/e2e-environment.md` |
| Future types | — | — | `artifacts/future-types.md` |

## 5. Anti-patterns — Do NOT Do This

- ❌ **Call an in-process test "E2E"** — anything on `ApiEndpointTests` / `WebApplicationFactory`
  is Integration; only the Playwright smoke run is E2E (§1, `references/e2e-environment.md`)
- ❌ **Mock anything other than a declared external provider** in Integration or E2E tests —
  repositories, `LocalJsonStorage`, serializers and services stay real (`references/mock-health-rules.md`)
- ❌ **Skip an explicit AC-tracing test because an endpoint test happens to exercise the same
  path** — incidental coverage isn't AC coverage (`references/feature-traceability.md`)
- ❌ **Write an AC test without its `[Trait("AC", …)]` / `[AC …]` tag** — it can't be audited
- ❌ **Unit test a controller's business behaviour** — only guard clauses are unit tested; the
  rest goes through the host (`artifacts/api-controllers-and-middleware.md`)
- ❌ **Mock `HttpClient`, `TimeProvider` or `IJsonStorage` with a framework** — inject a fake
  `HttpMessageHandler`, a `FakeTimeProvider`, or a `LocalJsonStorage` on a temp file (§1)
- ❌ **Skip DI resolution tests for a new `Add*` method or provider branch** (`artifacts/dependency-injection-modules.md`)
- ❌ **Test only the happy path** of a branch, provider call or storage write
  (`references/negative-path-testing.md`)
- ❌ **Write mirror tests** — asserting a DTO field equals the value you just passed in (§2)
- ❌ **Assert on a logged message text or a financial value in a log** — assert the exception
  type name via `RecordingLogger<T>` (`docs/rules/implementation.md`)
- ❌ **Construct `ApiTestFactory` inside a test class, or copy a stub locally** — derive from
  `ApiEndpointTests`; add doubles to `Tests/Financial.TestUtilities`
- ❌ **Assign `SelectedNode` directly in a WPF ViewModel test** — select through
  `TreeNodeViewModel.IsSelected` (`artifacts/wpf-viewmodels.md`)
- ❌ **Mock individual `fetch` calls or add a second `ResizeObserver` / Fluent shim** — mock
  `financialApiClient` at module level; `setupTests.ts` already shims (`references/gotchas.md`)
- ❌ **Change a DTO or endpoint without regenerating the snapshot and `openapi.ts`**
  (`artifacts/api-contract-snapshot.md`)
- ❌ **Run tests or migrations against `data/*.json`, or smoke-test on the live app's port 8080**
  (`references/e2e-environment.md`)

## 6. Layer Boundary Note

This guide uses a three-layer model — Unit, Integration (intra-domain), E2E
(System) — but "Integration" and "E2E" mean something narrower than in many
guides. What most guides call "integration" or "e2e" for an in-process test
through the real HTTP stack (`ApiEndpointTests` over `WebApplicationFactory<Program>`) is
**Integration** here — real wiring, one process, no cross-service deployment.
**E2E** is reserved for tests that run more than one deployed process: the CI `smoke`
job's published `Financial.Api.dll` plus Playwright's Chromium driving the built SPA.
Acceptance-criteria coverage does not get its own layer either: it's a mandatory,
explicitly-tagged subset of Integration tests (see `references/feature-traceability.md`)
— same setup and mock boundary as any other Integration test, distinguished only by
what it asserts and how it's tagged.

## 7. References

| Topic | File |
|---|---|
| External provider strategies (what's faked, what's owned) | `references/external-providers.md` |
| Mock/stub health rules & the external-provider-only boundary | `references/mock-health-rules.md` |
| Where features/ACs live and how AC-tracing Integration tests cite them | `references/feature-traceability.md` |
| Negative-path/failure-case patterns per layer | `references/negative-path-testing.md` |
| The production-like multi-service E2E environment | `references/e2e-environment.md` |
| File naming, directory structure, coverage philosophy | `references/file-conventions.md` |
| Stack-specific gotchas & pitfalls | `references/gotchas.md` |
| Time/concurrency, UI state matrix, accessibility, contract drift, fitness functions, test data, and the frontend-mocks-own-backend exception | `references/cross-cutting-concerns.md` |
| Phase 2 web research log — what was searched, found, and where it landed | `references/research-notes.md` |
| Full folder-by-folder classification from Phase 1.3 | `references/inventory.md` |

## 8. How to Use This Guide

This guide is organized as a multi-file skill:
- **This file (SKILL.md)** — always loaded. Core rules, quick reference, anti-patterns.
- **`artifacts/`** — one file per artifact type. Read the relevant file for the type
  you're touching.
- **`references/`** — supporting content, including `references/feature-traceability.md` (how to
  write and organize the mandatory AC-tracing Integration tests for a feature) and
  `references/negative-path-testing.md` (failure-case patterns required at every layer).

When working on a feature:
1. Check §3 (Feature Implementation Checklist) — add the feature's own AC-tracing row, then a
   row per artifact touched.
2. Read `references/feature-traceability.md` for how to structure the AC-tracing tests,
   `references/negative-path-testing.md` for each layer's failure cases, and the relevant
   `artifacts/*.md` file(s) for each artifact.
3. Consult other `references/` files as needed. Guided test-writing and test-audit workflows
   are out of scope for this guide; no sibling skill provides them.

## 9. Test Baseline (as of generation)

Snapshot from Phase 1.5, captured on 2026-09-06 (HEAD `b8a842a9`). Counts are `[Fact]` / `[Theory]`
attributes (not expanded `InlineData` rows) and `it(` / `test(` calls. Re-run later and compare
to spot drift — layers losing coverage, or AC-tracing tests not keeping pace with features.

| Artifact type / area | Existing test count | Layer breakdown | AC-tracing tests identifiable? |
|---|---|---|---|
| Api.Tests (controllers, middleware, contract, observability) | 446 in 42 files | Integration: ~430, Unit (guard clauses): ~16, E2E: 0 | No — none tagged |
| Architecture.Tests | 14 in 7 files | Integration: 14 | No |
| CashFlow.Domain.Tests | 259 in 23 files | Unit: 259 | No |
| CashFlow.Application.Tests | 515 in 25 files | Unit: 515 | No (one comment cites PRD P18 F04) |
| CashFlow.Infrastructure.Tests | 87 in 13 files | Integration: ~70, Unit (converters/resolver): ~17 | No |
| Investment.Domain.Tests | 193 in 15 files | Unit: 193 | No |
| Investment.Application.Tests | 259 in 17 files | Unit: 259 | No |
| Investment.Infrastructure.Tests | 184 in 22 files | Unit (fetchers/adapters): ~120, Integration (repo, DI, Yahoo, example): ~64 | No |
| Shared.Infrastructure.Tests | 48 in 7 files | Integration: 48 | No |
| GoogleIntegrations + Observability + WebPageParser Tests | 17 + 23 + 25 in 14 files | Unit: ~50, Integration (DI/config): ~15, manual-skipped live: 2 files | No |
| CashFlowSpreadsheetImport + InvestmentSpreadsheetImport Tests | 184 + 48 in 32 files | Unit: ~160, Integration (temp JSON/backup): ~72 | No |
| Presentation.Tests (WPF) | 1102 in 108 files | Unit: ~1098, Integration (DI): 2 classes, contract (XAML binding): 1 class | No |
| Financial.Web (vitest) | 1468 in 119 files | Unit (hooks/components/utils/client): ~1100, Integration (pages, App, freshness): ~370 | No |
| Playwright smoke | 1 script | E2E: 1 journey, 0 failure journeys | No |

Total: 3404 .NET tests across 325 files, 1468 web tests across 119 files, 1 E2E script, as of 2026-09-06.
