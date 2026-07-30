---
name: testing-guide-Financial
description: >
  Testing guide for Financial. Reference this skill when planning features,
  implementing code, creating tests, or reviewing changes in Financial.
  Covers what to test, at which layer, and how to set up each test —
  organized by artifact type across the .NET backend (2 DDD domains + WPF
  presentation), 4 Integrations projects, and the React frontend.
  Triggers on: planning Financial features, implementing Financial features,
  writing tests for Financial, reviewing Financial code, reviewing Financial tests,
  what should I test in Financial, how to test Financial, Financial test guide.
---

## §0. Purpose

This guide helps you decide **what to test**, at **which layer**, and **how to set up tests** for each type of artifact in `Financial`. The solution spans two independent DDD domains (Investment, CashFlow), a WPF presentation layer, four standalone Integrations projects, an ASP.NET Core API, and a React frontend. When working on a specific artifact type, read the corresponding guide in `artifacts/` for the complete recipe. Supporting references (mock strategies, file conventions, gotchas) are in `references/`.

This guide supersedes the previous unit-test-only version: the codebase now has real E2E tests (`WebApplicationFactory`) and the CashFlow domain, so this revision covers **unit, integration, and E2E** layers and both domains.

---

## §1. Testability Foundations

**Domain layer has zero dependencies.** Dependency direction is Presentation → Application → Domain (CLAUDE.md). Domain entities, value objects, and rules/calculators (`Financial.*.Domain/{Entities,ValueObjects,Rules}`) need no mocks, no file I/O — pure logic tests via direct instantiation.

**No mocking framework is used anywhere in this solution** (no Moq/NSubstitute). This is a deliberate, project-wide choice, confirmed again in this revision:
- Application-layer tests use **hand-written stub classes** implementing the same interface (`StubRepository`, `StubFinanceService`, `DividendServiceStub`) — explicit, readable, and each stub only implements what the test needs (`NotImplementedException` on the rest).
- Infrastructure tests use **real implementations** against real temp files, real in-memory `XLWorkbook` objects, or a hand-rolled `FakeHttpMessageHandler`.
- **When a C# test reaches for a mocking framework, that's a signal** business logic leaked into Infrastructure, or the unit under test needs too many collaborators and should be split.

**Configured dependency contracts get real instances, not fakes.** `FrankfurterExchangeRateProviderTests` uses a real `HttpClient` with a fake `HttpMessageHandler` — this proves the request URL, header, and JSON-parsing contract are correct, which a mocked `IExchangeRateProvider` never would.

**Module/DI wiring is tested by resolving the real container.** `CashFlowInfrastructureServiceCollectionExtensionsTests` and `GoogleFinancialSupportServiceCollectionExtensionsTests` build a real `ServiceProvider` from an `IConfiguration` and call `GetRequiredService<T>()` — this is the only way to catch a missing registration or wrong default before runtime.

**E2E tests use the real ASP.NET Core pipeline.** `ApiTestFactory : WebApplicationFactory<Program>` boots the real app with temp JSON data files and overrides only true external-system boundaries (`IExchangeRateProvider`) or a single service under test (`IDividendService`) via `ConfigureTestServices` + `RemoveAll<T>()`. Everything else — routing, model binding, `[ApiController]` validation, JSON serialization — runs for real. This is why controllers themselves are **not** unit tested for behavior (see `artifacts/controllers.md`).

**Accepted, documented gap:** the raw Google API wrappers (`GoogleDriveClient`, `GoogleFileClientFactory`, `GoogleService`, `GoogleSheetsClient`, `GoogleCredentialFactory`) have no automated tests. They are thin wrappers around Google's own SDK requiring live OAuth credentials; per CLAUDE.md's "does not over-engineer" guidance for this single-user personal project, this is an accepted trade-off, not an oversight. See `artifacts/google-api-wrappers.md`. Similarly, web-scraping CSS selector correctness (`GoogleFinance.cs`, `StatusInvest.cs`) is verified **manually** against live pages (`HOW_TO_VERIFY_SELECTORS.md`) — only the pure string-parsing functions are automated. See `artifacts/web-scraping-parsers.md`.

**TypeScript: the API client is the one frontend system boundary.** All component/hook tests mock it via `vi.mock('.../financialApiClient')` at the module level — never mock individual `fetch` calls. React Testing Library tests are this project's unit-test equivalent for components: assert on user-visible behavior via `screen`, not internal state or DOM structure.

---

## §2. Testing Criteria

Worth testing in this codebase:
- Domain entities/value objects with invariants or state transitions (e.g., `Asset.AddTransaction` recalculating `AveragePrice`, `Transaction` guard clauses)
- Domain Rules/calculators with branching (`XirrCalculator`, `ReserveSplitCalculator`, `CategoryClassifier`)
- Application Services with branching logic that depends on stubbed collaborators (`ControleMaeService` currency conversion branches, `TitheService` calculation)
- Application parsers/validators (`CategoryParser`, `CurrencyParser`) — every branch and malformed-input case
- Infrastructure repositories/storage against a real temp file or real in-memory workbook
- The `FrankfurterExchangeRateProvider` HTTP contract (success, non-2xx, malformed body, missing currency, transport exception) — all 5 branches are already covered and should stay covered for any new HTTP-backed service
- DI module registration/resolution, including the "unsupported provider throws" and "default provider" branches
- API endpoint E2E: status codes, input validation (whitespace/blank ticker → 400), error-to-HTTP mapping, and JSON property name contracts the frontend depends on (`GetDividendSummary_JsonContainsAverageDividendLastFiveYears`)
- React hooks with branching over `SelectedNode` type discrimination (Broker vs Portfolio vs Asset) and loading/error states
- React pages/components: data rendering after mock API resolves, loading/error states, user interactions

NOT worth testing here:
- Controllers' business behavior (covered by E2E; see `artifacts/controllers.md`) — only constructor null-guards are unit tested, and only because DI/`[ApiController]` can't be relied on to prevent them in a direct-construction test
- The Google SDK wrapper classes (accepted gap, see §1)
- CSS selector correctness for scraped pages (verified manually, see §1)
- Trivial entity getters/setters with no logic
- Serializer property mapping with no custom transformation
- React rendering mechanics, Recharts internals, CSS class names
- Mirror tests (assertion copies the return value) on either stack

---

## §3. Feature Implementation Checklist

When implementing a new feature, walk each row for every artifact you touched.

**C# (.NET)**

| Artifact created/modified | Required tests | Guide |
|---|---|---|
| Domain entity / rule / calculator | `[Fact]`: state change, guard clause, branch | `artifacts/domain-entities.md` |
| Value object | `[Fact]`: equality, immutability, construction validation | `artifacts/value-objects.md` |
| Application service (branching + stubbed deps) | `[Fact]`: each branch with hand-written stub | `artifacts/application-services.md` |
| Application parser/validator | `[Theory]`+`[InlineData]`: all branches, null/empty | `artifacts/application-parsers.md` |
| Repository / JSON storage | Real temp file (Local) or injected fake delegate (Drive) | `artifacts/infrastructure-persistence.md` |
| Service calling external HTTP API | Fake `HttpMessageHandler`: success, error status, malformed body, exception | `artifacts/external-http-services.md` |
| `*ServiceCollectionExtensions.cs` | Real `ServiceProvider`: default + misconfigured provider | `artifacts/dependency-injection-modules.md` |
| Controller | Constructor null-guard only — no behavior tests | `artifacts/controllers.md` |
| New/changed API endpoint | E2E via `ApiTestFactory`: status codes, validation, JSON contract | `artifacts/api-endpoints-e2e.md` |
| Serializer/Adapter | Round-trip test | `artifacts/serialization.md` |
| WPF ViewModel/Converter/Helper | `[Fact]` with hand-written stub services | `artifacts/wpf-presentation.md` |
| Web-scraping parsing function | `[Theory]` on string inputs; selectors verified manually | `artifacts/web-scraping-parsers.md` |
| Spreadsheet importer/parser | Real in-memory `XLWorkbook` built in-test | `artifacts/spreadsheet-import.md` |

**TypeScript (React)**

| Artifact created/modified | Required tests | Guide |
|---|---|---|
| New page (`*Page.tsx`) | Render + API mock + user interactions | `artifacts/react-pages.md` |
| Shared component (`*.tsx`) | Render with variant props, loading/error states | `artifacts/react-components.md` |
| Custom hook (`use*.ts`) | `renderHook` + `waitFor`, all branches over input type | `artifacts/react-hooks.md` |
| API client / config / utility | Pure function test | `artifacts/api-client.md` |

**How to use:** after implementing a feature, walk through each row. For each artifact you created or modified, read the corresponding guide and verify the tests exist. Skip rows that don't apply.

---

## §4. Artifact Type Quick Reference

| Artifact Type | Pattern | Test Layer(s) | Guide |
|---|---|---|---|
| Domain Entities & Rules | `*.Domain/{Entities,Rules}/*.cs` | Unit | `artifacts/domain-entities.md` |
| Domain Value Objects | `*.Domain/ValueObjects/*.cs` | Unit | `artifacts/value-objects.md` |
| Application Services | `*.Application/Services/*.cs` | Unit (stub deps) | `artifacts/application-services.md` |
| Application Parsers/Validators | `*.Application/Validation/*.cs` | Unit | `artifacts/application-parsers.md` |
| Infrastructure Persistence | `*Repository.cs`, `*Storage.cs` | Integration (real temp file / fake delegate) | `artifacts/infrastructure-persistence.md` |
| External HTTP Services | `*Provider.cs` calling external API (e.g. `FrankfurterExchangeRateProvider`) | Integration (fake `HttpMessageHandler`) | `artifacts/external-http-services.md` |
| DI Modules | `*ServiceCollectionExtensions.cs` | Unit (real container resolution) | `artifacts/dependency-injection-modules.md` |
| API Controllers | `*Controller.cs` | Unit (guard clauses only) | `artifacts/controllers.md` |
| API Endpoints | `*EndpointsTests.cs` via `ApiTestFactory` | E2E | `artifacts/api-endpoints-e2e.md` |
| Serialization | `*Serializer.cs`, `*Adapter.cs` | Unit (round-trip) | `artifacts/serialization.md` |
| WPF Presentation | `*ViewModel.cs`, `*Converter.cs`, Helpers | Unit (stub deps) | `artifacts/wpf-presentation.md` |
| Web-scraping Parsers | `GoogleFinance.cs`, `StatusInvest.cs` parsing functions | Unit (string inputs) | `artifacts/web-scraping-parsers.md` |
| Google API Wrappers | `GoogleDriveClient`, `GoogleService`, `GoogleSheetsClient`, etc. | None (accepted gap) | `artifacts/google-api-wrappers.md` |
| Spreadsheet Import | `Integrations/CashFlowSpreadsheetImport/**` | Unit/Integration (real `XLWorkbook`) | `artifacts/spreadsheet-import.md` |
| React Pages | `*Page.tsx` in `Financial.Web/src/pages/` | Component (RTL) | `artifacts/react-pages.md` |
| React Components | `*.tsx` in `Financial.Web/src/components/` | Component (RTL) | `artifacts/react-components.md` |
| React Hooks | `use*.ts` in `Financial.Web/src/hooks/`, Context | Hook (`renderHook`) | `artifacts/react-hooks.md` |
| API Client / Config / Utilities | `*.ts` in `Financial.Web/src/api/`, `utils/` | Unit | `artifacts/api-client.md` |
| Future types | — | — | `artifacts/future-types.md` |

---

## §5. Anti-patterns — Do NOT Do This

- ❌ **Unit-test controller business behavior** — controllers are thin `[ApiController]` delegation; test via E2E, unit-test only constructor null-guards (`artifacts/controllers.md`)
- ❌ **Introduce a mocking framework** (Moq/NSubstitute) — this project deliberately uses hand-written stubs and real objects; a felt need for a mocking library signals a design problem, not a tooling gap (§1)
- ❌ **Mock `IExchangeRateProvider` or any configured HTTP dependency** — use a real `HttpClient` with a fake `HttpMessageHandler`; a mocked provider never catches a wrong URL, header, or JSON shape (`artifacts/external-http-services.md`)
- ❌ **Skip DI module resolution tests** — a missing registration or wrong default provider only fails at runtime; `GetRequiredService<T>()` against a real container catches it at test time (`artifacts/dependency-injection-modules.md`)
- ❌ **Write automated CSS-selector tests for scraped pages** — selector drift against live sites isn't caught by any fixture; this project verifies selectors manually per `HOW_TO_VERIFY_SELECTORS.md` and only unit-tests the pure parsing functions (§1, `artifacts/web-scraping-parsers.md`)
- ❌ **Chase coverage on the Google SDK wrappers** — accepted gap; wrapping Google's SDK for testability isn't worth it for a single-user project (§1, `artifacts/google-api-wrappers.md`)
- ❌ **Put `File.Delete(tempFile)` / `factory.Dispose()` cleanup in an assert block** — a failing assertion skips cleanup; always use `finally` or `await using` (`references/gotchas.md`)
- ❌ **Mock individual `fetch` calls in React tests** — mock the client factory at the module boundary: `vi.mock('.../financialApiClient')` (`references/mock-health-rules.md`)
- ❌ **Test hook/component internal state or CSS class names** — assert on `screen` queries and `result.current`, not implementation details
- ❌ **Forget `mockReset()` in `beforeEach`** for frontend mocks, or **forget `await using`/`Dispose()`** for `ApiTestFactory` — both leak state/files across tests (`references/gotchas.md`)
- ❌ **Write mirror tests** — an assertion that copies the return value proves nothing (§2)

---

## §6. E2E Terminology Note

"E2E" in this guide means HTTP-layer integration tests using `WebApplicationFactory<Program>` + `HttpClient` (the .NET equivalent of supertest) — real routing, model binding, and JSON serialization, in-process, no browser. It does not mean browser-based or multi-service end-to-end tests; there are none in this project.

---

## §7. References

| Topic | File |
|---|---|
| External system strategies (JSON file, Google Drive, Frankfurter API, web scraping, Excel import) | `references/external-systems.md` |
| Mock/stub health rules & the "no mocking framework" boundary principle | `references/mock-health-rules.md` |
| File naming, directory structure, coverage philosophy | `references/file-conventions.md` |
| Stack-specific gotchas & pitfalls | `references/gotchas.md` |

---

## §8. How to Use This Guide

- **This file (SKILL.md)** — always loaded. Contains core rules, quick reference, and anti-patterns.
- **`artifacts/`** — one file per artifact type. Read the relevant file when creating or modifying that type.
- **`references/`** — supporting content. Read when you need details on mock strategies, file conventions, or gotchas.

When working on a feature:
1. Check §3 (Feature Implementation Checklist) to identify which artifacts need tests
2. Read the corresponding `artifacts/*.md` file for the complete testing recipe
3. Consult `references/` files as needed for mock strategies, conventions, or pitfalls
