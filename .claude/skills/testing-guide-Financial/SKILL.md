---
name: testing-guide-Financial
description: >
  Testing guide for Financial. Reference this skill when planning features,
  implementing code, creating tests, or reviewing changes in Financial.
  Covers what to test, at which layer, and how to set up each test —
  organized by artifact type.
  Triggers on: planning Financial features, implementing Financial features,
  writing tests for Financial, reviewing Financial code, reviewing Financial tests,
  what should I test in Financial, how to test Financial, Financial test guide.
---

## 0. Purpose

This guide helps you decide **what to test**, at **which layer**, and **how to set up tests** for
each type of artifact in `Financial` — domain entities and rules, application services,
validators, JSON persistence, external HTTP providers, Google and scraping integrations, DI
modules, controllers and middleware, the OpenAPI contract, console tools, and the React hooks,
components and pages. When working on a specific artifact type, read the corresponding guide in
`artifacts/` for the complete recipe. Supporting references (external systems, mock rules, file
conventions, gotchas, AC tracing, negative paths, the E2E environment) are in `references/`.

How the test classes are *wired* (shared doubles, `ApiEndpointTests`, `Create*` helpers, pinned
clock, `Category` traits) is binding in `docs/rules/implementation.md` §Tests; this guide never
contradicts it and does not repeat it beyond what a recipe needs.

## 1. Testability Foundations

- **Layers are the `Category` trait, not the folder.** `Unit` = in process, no host, no real file
  or network (Domain, Application, Frankfurter/Google/Observability policy code, architecture
  rules, host-less `Api.Tests` classes). `Integration` = a real temp JSON file, a real
  `ServiceCollection`, or the real host through `ApiEndpointTests` (Infrastructure projects, import
  tools, host-based `Api.Tests`). `Live` = real third-party sites, nightly only. Playwright is the
  only E2E. `CategoryTraitCoverageTests` fails a class without a trait.
- **The JSON file is the database, and it is owned.** Both contexts persist one document each
  through `LocalJsonStorage`. It costs nothing to run for real, so repository, serializer, loader
  and host tests use a `Guid`-named temp copy and never fake it — a stubbed repository cannot catch
  a broken reference converter or a lost field on round-trip. `StubCashFlowRepository` /
  `StubInvestmentRepository` exist only so Application **Unit** tests can test branches without I/O.
- **One mock boundary: the third party.** Frankfurter, Yahoo, scraped pages, Google Drive / Sheets /
  Calendar and the OTLP collector are faked; everything we own stays real above Unit. In ASP.NET
  Core 10 that boundary is a `ConfigureTestServices` + `RemoveAll<T>()` swap, and `ApiTestFactory`
  accepts exactly four: `IExchangeRateProvider` (a stub by default), `TimeProvider` (always
  `TestClock.At()` unless overridden), `ICalendarProvider`, and opting back into real FX. For HTTP
  providers the seam is the transport: a real `HttpClient` over `FakeHttpMessageHandler`, so URL
  building and JSON parsing run for real — never a mocked `HttpClient`.
- **No mocking framework, on purpose.** There is no Moq/NSubstitute/MSW. Doubles are hand-written
  classes in `Tests/Financial.TestUtilities` (`Stub*`, `Fake*`, `Recording*`); an unexpected call
  fails loudly instead of returning a default.
- **Configured libraries are tested with their real configuration.** `AddHttpClient` base
  addresses, `AddObservability` option binding, the OTLP exporter setup, the `Repository:Provider`
  switch and `System.Text.Json` resolver settings are contracts *we* wrote; a DI test resolves them
  from a real container. The container resolves at runtime, so a missing registration only fails on
  `GetRequiredService<T>()` — compile time cannot catch it.
- **Observability is part of the service contract.** Every service operation opens a span named
  `<Context>.<Service>.<Operation>` and marks it failed before rethrowing. A bare `Throw<T>()`
  passes with the `catch` deleted, so failure tests also call
  `_tracer.ShouldHaveFailedSpan<T>(spanName)`, and logged failures are asserted by exception
  **type**, never by message (messages embed financial values).
- **The SPA's only fake is its own backend.** The API is a separate deployable reached over HTTP,
  so Vitest tests replace `financialApiClient` at module level with `vi.mock` + `vi.hoisted` typed
  mocks; hooks, components, router and contexts stay real. Fluent UI v9 focus (tabster) is not
  reliable in jsdom — assert handlers and accessible names there, real focus in Playwright.
- **The contract is pinned at both ends.** Controllers return Application DTOs, so a DTO reshape is a
  wire change: `OpenApiContractTests` pins the snapshot, `openapiFreshness.test.ts` pins the
  generated `openapi.ts`, and `tsc -b` breaks every call site that read a renamed field.
- **Time is controlled, never waited on.** Production reads time only through an injected
  `TimeProvider` (`ProductionClockReadsTests`); tests use `TestClock` or `FakeTimeProvider` and
  `Advance`. A queued continuation may not have armed its timer yet — wait on a counter
  (`ObservableFakeClock.TimersArmed`) before advancing.

## 2. Testing Criteria

Worth testing here:
- **Every PRD §9 acceptance criterion**, positive and negative, as one tagged Integration (or page)
  test — `references/feature-traceability.md`.
- **Domain factory guards, state transitions and `Rules/` calculators** (`Expense.SetRoundUpAmount`,
  `XirrCalculator`, `PotSplitAllocator`, `CardStatementLedger`) — both sides of every boundary.
- **Application services with branching** — each branch plus its failed span and logged exception type.
- **Parsers and validators in `Validation/`** — every accepted form and every malformed input.
- **Repository ↔ storage ↔ serializer round-trips** and reference converters — real temp file.
- **Debounce, retry and flush timing** in `DebouncedJsonStorage`, `RetryPolicy`, `GoogleRetryPolicy`.
- **External HTTP providers' contract** — success, non-2xx, malformed body, thrown transport error, timeout.
- **DI extension methods** — resolution from a real container, including the unsupported-provider branch.
- **Controller status codes and `DomainExceptionMappingMiddleware` mappings** through the host,
  including the log redaction rule.
- **The OpenAPI snapshot and generated web types staying in sync.**
- **Dependency-direction and isolation rules** (`Tests/Financial.Architecture.Tests`).
- **Example and seed JSON files** still loading through the real loader.
- **React hooks' loading / error / retry**, components' roles, labels and keyboard handlers, pages'
  full UI state matrix (`docs/rules/ui.md`).

NOT worth testing here:
- **Controller business behaviour in isolation** — controllers delegate; prove them through the host.
- **Per-class constructor null guards** — each project's `ConstructorGuardTests` covers them.
- **DTO or entity shape by hand** — the OpenAPI snapshot pins DTOs; a getter with no logic is covered
  by its factory test.
- **Framework behaviour** — ASP.NET model binding and routing, `System.Text.Json`, react-router,
  HtmlAgilityPack, recharts SVG.
- **Raw Google SDK request plumbing beyond the fake-handler contract**, and live CSS selectors in a
  PR run (those are `Category=Live`).
- **The same parser or rule re-asserted through every caller**, and variant repetition of one path.
- **`Tools/ImportGoogleSpreadSheets`** (legacy one-off importer, out of scope by decision).

### Test value is a review requirement

Judge a test by what it verifies, never by how many tests, files or covered lines it adds. Every
new or changed test must answer **yes** to at least one question, and the review rejects it otherwise:
- Does it verify behaviour that could realistically break (a branch, a boundary, a mapping, a write)?
- Does it protect a business rule or a PRD acceptance criterion?
- Would the regression it catches otherwise reach the user (wrong total, lost data, blocked flow)?

Low value, and rejected even when it raises coverage:
- **Re-stating the implementation or the framework** — a basic React component "renders", a
  `Set`/`List`/`Dictionary` behaving like one, a DTO echoing its input, a constant equal to itself.
  A component test earns its place by asserting a validation rule, an accessible interaction, or a
  state the user depends on.
- **The same assertion at two layers.** Test each behaviour at the lowest layer that gives enough
  confidence. A higher layer may touch the same rule only to prove something the lower one cannot
  (module integration, persistence, the API contract, the user journey), and it asserts *that*.
- **When a higher-level test finds a defect**, add the focused lower-level test that would have
  caught it, then keep the higher one only if it still proves something else.
- **Coverage-driven tests.** The CI coverage gate is a tripwire for untested code, not a target to
  hit with filler (`references/file-conventions.md`).

Shape of the suite: many Unit tests; Integration where a real boundary is crossed (the JSON file,
the container, the host, a provider's contract); a small, deliberate set of E2E journeys, each
justified by the questions in `references/e2e-environment.md`.

## 3. Feature Implementation Checklist

When implementing a new feature, use this checklist so every artifact has the right coverage. For
each artifact created or modified, check the required layers — and the failure case of each:

| Artifact created / modified | Required tests | Guide |
|---|---|---|
| A feature with PRD §9 criteria | Integration (AC-tracing): one tagged test per bullet, negative ones included | `references/feature-traceability.md` |
| Domain entity, value object, `Rules/` type | Unit: each invariant, each rejected input, both boundary sides | `artifacts/domain-entities-and-rules.md` |
| Application service with branching | Unit: each branch + failed span + logged type (stub repo); Integration: through the host | `artifacts/application-services.md` |
| Application service that only reads/writes the repository | Integration: through the host, persisted state reloaded from disk | `artifacts/application-services.md` |
| Parser / validator | Unit: every accepted and malformed input | `artifacts/application-parsers-validators.md` |
| Repository, serializer, converter, storage, FX rate store | Integration: temp-file round-trip, rejected write; Unit: converters | `artifacts/json-persistence-and-repositories.md` |
| HTTP provider, price fetcher, FX provider | Unit: fake handler success + four failure modes; fetchers over stubs | `artifacts/external-http-services.md` |
| Scraper parsing function | Unit on fixtures; `Live` check when a selector changes | `artifacts/web-scraping-parsers.md` |
| Google SDK wrapper, OAuth client, retry/translator | Unit: fake handler or pure policy; DI registration | `artifacts/google-sdk-wrappers.md` |
| Observability wiring | Unit: option binding, exporter setup; Integration: unreachable collector via host | `artifacts/observability-integration.md` |
| `Add*` DI extension / `Program.cs` registration | Integration: real container resolves; unsupported config throws | `artifacts/dependency-injection-modules.md` |
| Controller / middleware / API helper | Integration via `ApiEndpointTests`: status codes, problem details, log redaction | `artifacts/api-controllers-and-middleware.md` |
| DTO, route or verb change | Regenerate snapshot + `npm run generate-api-types` + `npm run build` | `artifacts/api-contract-snapshot.md` |
| New project or cross-project reference | Architecture rule test that fails on the forbidden reference | `artifacts/architecture-rule-tests.md` |
| `data/*.example.json`, `TestData/*.test.json` | Integration: real loader still parses | `artifacts/example-and-seed-data.md` |
| Import / migration / report tool | Unit: resolvers, formatters; Integration: temp JSON + in-memory workbook | `artifacts/console-tools.md` |
| React hook | Unit: loading, success, error, retry, mutation failure | `artifacts/react-hooks.md` |
| React component | Unit: roles, names, keyboard, each visual state it owns | `artifacts/react-components.md` |
| React page | Page test: full state matrix with `apiClient` faked | `artifacts/react-pages.md` |
| `financialApiClient` method | Unit: verb, URL, body, `ApiError` on non-2xx | `artifacts/web-api-client.md` |
| Web util / context / navigation entry | Unit; sidebar ↔ route agreement | `artifacts/web-utils-context-navigation.md` |
| New or changed critical journey | E2E: Playwright spec incl. one failure journey | `references/e2e-environment.md` |

**How to use:** after implementing a feature, add its AC-tracing row first, then walk each row for
anything you created or modified, read the guide, and confirm both the success and the failure case
exist. Skip rows that don't apply.

## 4. Artifact Type Quick Reference

When creating or modifying an artifact, read the corresponding guide for the complete recipe.

| Artifact Type | Pattern | Test Layer(s) | Guide |
|---|---|---|---|
| Domain entities, value objects, rules | `Financial.*.Domain/{Entities,ValueObjects,Rules}/*.cs` | Unit | `artifacts/domain-entities-and-rules.md` |
| Application services | `Financial.*.Application/Services/*Service.cs` | Unit + Integration | `artifacts/application-services.md` |
| Parsers / validators | `Financial.*.Application/Validation/*.cs` | Unit | `artifacts/application-parsers-validators.md` |
| JSON persistence & repositories | `*/Persistence/**`, `*/Repositories/**`, `Financial.Shared.Infrastructure/**` | Integration (+ Unit converters) | `artifacts/json-persistence-and-repositories.md` |
| External HTTP services & fetchers | `Integrations/Frankfurter`, `Financial.Investment.Infrastructure/Services`, `Shared.Abstractions/Currencies` | Unit (fake transport) | `artifacts/external-http-services.md` |
| Web scraping parsers | `Integrations/WebPageParser/*.cs` | Unit + Live | `artifacts/web-scraping-parsers.md` |
| Google SDK wrappers | `Integrations/Google{Core,Drive,Sheets,Calendar}/*.cs` | Unit + Integration (DI) | `artifacts/google-sdk-wrappers.md` |
| Observability | `Integrations/Observability`, `Shared.Abstractions/Observability` | Unit + Integration (host) | `artifacts/observability-integration.md` |
| DI modules | `*/DependencyInjection/*ServiceCollectionExtensions.cs`, `Program.cs` | Integration | `artifacts/dependency-injection-modules.md` |
| Controllers, middleware, helpers | `Financial.Api/{Controllers,Middleware,Helpers}/*.cs` | Integration (host) | `artifacts/api-controllers-and-middleware.md` |
| API contract & generated types | `Contract/openapi-v1.snapshot.json`, `src/api/generated/openapi.ts` | Integration | `artifacts/api-contract-snapshot.md` |
| Architecture rules | `Tests/Financial.Architecture.Tests/*Tests.cs` | Unit | `artifacts/architecture-rule-tests.md` |
| Example & seed data | `data/*.example.json`, `Tests/**/TestData/*.test.json` | Integration | `artifacts/example-and-seed-data.md` |
| Console tools | `Tools/{CashFlowSpreadsheetImport,InvestmentSpreadsheetImport,InvestmentDataQualityReport}` | Unit + Integration | `artifacts/console-tools.md` |
| React hooks | `Financial.Web/src/hooks/use*.ts`, `map*ErrorToField.ts` | Unit | `artifacts/react-hooks.md` |
| React components | `Financial.Web/src/components/**/*.tsx` | Unit | `artifacts/react-components.md` |
| React pages | `Financial.Web/src/pages/*.tsx`, `src/App.tsx` | Page (frontend Integration) | `artifacts/react-pages.md` |
| Web API client | `Financial.Web/src/api/{financialApiClient,apiError,config}.ts` | Unit | `artifacts/web-api-client.md` |
| Web utils, context, navigation | `Financial.Web/src/{utils,context,navigation}/*` | Unit | `artifacts/web-utils-context-navigation.md` |
| Future types | — | — | `artifacts/future-types.md` |

## 5. Anti-patterns — Do NOT Do This

- ❌ **Unit test a controller's behaviour** — controllers delegate; prove them through `ApiEndpointTests` (`artifacts/api-controllers-and-middleware.md`)
- ❌ **Fake something we own above Unit** — repositories, `LocalJsonStorage`, serializers, services and the container stay real; only third parties are faked (`references/mock-health-rules.md`)
- ❌ **Mock a configured library** (`HttpClient`, the OTLP exporter, `IOptions<T>`) — use the real one with test config; a mock hides a wrong base address or binding (§1)
- ❌ **Skip the temp-file Integration test for a repository or storage change** — a stub repository proves nothing about serialization (`artifacts/json-persistence-and-repositories.md`)
- ❌ **Skip the DI resolution test for a new `Add*` method or provider branch** (`artifacts/dependency-injection-modules.md`)
- ❌ **Assert only `Throw<T>()`** — add `ShouldHaveFailedSpan<T>`; assert logged failures by exception type, never by message or amount (`references/negative-path-testing.md`)
- ❌ **Test only the happy path** of a branch, provider call or write (`references/negative-path-testing.md`)
- ❌ **Skip an AC test because an endpoint test touches the same path**, or write one without its `AC` tag (`references/feature-traceability.md`)
- ❌ **Write mirror tests** — recomputing the expected value with the code under test, or asserting a DTO echoes its input (§2)
- ❌ **Add a test to move a count or a coverage number** — it must pass the test-value questions in §2
- ❌ **Repeat a Unit assertion through the host or Playwright** without a new thing to prove (§2)
- ❌ **Add an E2E spec for a risk a lower layer can catch** — answer the four E2E questions first (`references/e2e-environment.md`)
- ❌ **Write per-class constructor null-guard tests** — `ConstructorGuardTests` owns them
- ❌ **Construct `ApiTestFactory` as a field, or copy a stub locally** — derive from `ApiEndpointTests`; shared doubles go in `Tests/Financial.TestUtilities`
- ❌ **Read the wall clock or wait on it** (`DateTime.Now`, `Task.Delay`, `waitForTimeout`) — CI's hygiene scan rejects it (`references/gotchas.md`)
- ❌ **Mock `fetch` per call in a hook test, or add a second Fluent/ResizeObserver shim** — mock `financialApiClient`; `setupTests.ts` already shims (`references/gotchas.md`)
- ❌ **Change a DTO without regenerating the snapshot and `openapi.ts`** (`artifacts/api-contract-snapshot.md`)
- ❌ **Point a test, tool or smoke run at `data/*.json` or port 8080** (`references/e2e-environment.md`)

## 6. Layer Terminology Note

Many guides call an in-process test through the real HTTP stack (`WebApplicationFactory`) "E2E".
**Here it is Integration** — `Category=Integration`, one process, run by the `backend` CI job.
**E2E** means the Playwright suite in `Financial.Web/tests/e2e`: the published `Financial.Api.dll`
serving the built SPA on seeded JSON, driven by Chromium in the `web-e2e` CI job. The HTTP-contract
questions other guides put in "E2E" (status codes, validation wiring, response format) are answered
here by host-based Integration tests. AC-tracing tests are an explicitly tagged subset of
Integration, not a layer of their own.

## 7. References

| Topic | File |
|---|---|
| External systems — real vs fake, setup, teardown | `references/external-systems.md` |
| Mock health rules & the third-party-only boundary | `references/mock-health-rules.md` |
| File naming, directory structure, commands, coverage targets | `references/file-conventions.md` |
| Stack-specific gotchas & pitfalls | `references/gotchas.md` |
| AC-tracing tests: ids, tags, audit commands | `references/feature-traceability.md` |
| Failure-case patterns per layer | `references/negative-path-testing.md` |
| The Playwright E2E environment and local recipe | `references/e2e-environment.md` |
| Test-count baseline per project, to compare against later | `references/test-baseline.md` |

## 8. How to Use This Guide

This guide is organized as a multi-file skill:
- **This file (SKILL.md)** — always loaded. Core rules, quick reference, anti-patterns.
- **`artifacts/`** — one file per artifact type. Read the relevant file when creating or modifying that type.
- **`references/`** — supporting content. Read when you need mock strategies, conventions, gotchas,
  AC tracing, negative paths or the E2E environment.

When working on a feature:
1. Check §3 (Feature Implementation Checklist) to identify which artifacts need tests
2. Read the corresponding `artifacts/*.md` file for the complete testing recipe
3. Consult `references/` files as needed for mock strategies, conventions, or pitfalls
