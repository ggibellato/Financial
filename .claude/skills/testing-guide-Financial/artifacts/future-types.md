> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Future Types — Not Yet Present

Artifact types this stack could plausibly grow into, with proactive guidance so a first instance follows house conventions instead of importing patterns (mocking frameworks, snapshot tests) that conflict with this project's established style. (Custom hooks and E2E API tests were in this section in the previous revision of this guide — both are now real, first-class artifact types with their own guides: `artifacts/react-hooks.md` and `artifacts/api-endpoints-e2e.md`.)

## Domain Events

CLAUDE.md lists Domain Events as a Domain-layer concern, but none exist yet. If added: test them like `artifacts/value-objects.md` (construction, equality) plus, for any handler, like `artifacts/application-services.md` (branching logic, stub collaborators). No event-bus mocking framework — if a real in-process dispatcher is introduced, prefer testing its resolution the way `artifacts/dependency-injection-modules.md` tests DI resolution.

## ASP.NET Core Middleware / Exception Filters

None exist beyond default `[ApiController]` behavior. If custom middleware or a global exception filter is added, test it via `artifacts/api-endpoints-e2e.md`'s `ApiTestFactory` — status code and response shape for the middleware's effect — not a standalone unit test, since middleware's whole job is to sit in the real HTTP pipeline.

## FluentValidation-style Validators

Current validation is hand-rolled parsers (`artifacts/application-parsers.md`) plus `[ApiController]`'s automatic model validation. If FluentValidation (or similar) is introduced, keep the existing pattern: one wiring test per endpoint in the E2E suite proving the validator fires, plus `[Theory]` coverage of the validator's own rules at the unit layer — don't duplicate rule coverage into the E2E suite.

## Background/Hosted Services

None exist yet (no `IHostedService`/`BackgroundService`). If one is added (e.g., periodic price refresh): unit-test its branching logic with a stub collaborator (`artifacts/application-services.md` pattern); integration-test actual scheduling/execution only if the scheduling logic itself has bugs worth catching — don't test `IHostedService`'s own lifecycle, that's framework behavior.

## React Suspense / `use()` Hook

Not used yet — the project is on React 19.2 / Testing Library 16.3, which support it, but current data-fetching hooks use plain `useState`/`useEffect`. If adopted: `renderHook`/component tests will need to wrap renders in a `<Suspense>` boundary and assert on the fallback separately from resolved content — otherwise follow `artifacts/react-hooks.md` unchanged.

## New Integrations Projects

If a new sibling to `WebPageParser`/`GoogleFinancialSupport`/`CashFlowSpreadsheetImport` is added, classify it up front using §1's questions in `../SKILL.md`: does it wrap a live third-party SDK needing real credentials (→ `artifacts/google-api-wrappers.md`'s accepted-gap pattern), call a plain external HTTP API (→ `artifacts/external-http-services.md`'s fake-`HttpMessageHandler` pattern), or parse a real file format in-memory (→ `artifacts/spreadsheet-import.md`'s in-memory-document pattern)?

## C# Application Commands/Queries (CQRS)

Not used — Application Services are plain classes, not MediatR handlers. If introduced, treat exactly like `artifacts/application-services.md`: unit test with a hand-written stub repository, no mocking framework.
