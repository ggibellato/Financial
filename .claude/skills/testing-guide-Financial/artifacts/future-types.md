> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Future Artifact Types (not present yet)

Guidance for common framework artifact types this codebase does not have today. Each entry
says where it would live, which layer proves it, and the setup to copy — so the first instance
arrives with the right test rather than a new convention.

## ASP.NET Core action filters / result filters (`Financial.Api/Filters/`)

- **Layer**: Integration through `ApiEndpointTests` — a filter only matters once MVC invokes
  it. Assert the observable effect (header, status, transformed body) on one endpoint, plus
  the negative case the filter rejects.
- **Not**: a Unit test that constructs `ActionExecutingContext` by hand.

## Custom model binders / value providers

- **Layer**: Integration via the host, one accepted and one rejected input per binder; the
  400 problem-details body is the assertion. Update the OpenAPI snapshot if the parameter
  schema changes (`api-contract-snapshot.md`).

## Additional hosted/background services (beyond `ShutdownFlushHostedService<T>`)

- **Layer**: Unit for the scheduling decision with `FakeTimeProvider` /
  `Microsoft.Extensions.Time.Testing.FakeTimeProvider` (`ITimer` fires on `Advance`), as
  `DebouncedJsonStorageTests` does; Integration for registration
  (`ShutdownFlushHostedServiceRegistrationTests` shape) and for the persisted outcome on a temp
  JSON file. Never `Task.Delay` in the test.

## A third bounded context (`Financial.<Name>.{Domain,Application,Infrastructure}`)

- Add `<Name>DependencyRuleTests` to `Tests/Financial.Architecture.Tests` (three assertions:
  Domain ↛ Application, Domain ↛ Infrastructure, Application ↛ Infrastructure), add the
  project references to `Financial.Architecture.Tests.csproj`, add the assembly to
  `SharedInfrastructureIsolationRuleTests.IsolatedProjects`, and mirror the three test
  projects (`Tests/Financial.<Name>.*.Tests`). Stubs for its repository go in
  `Tests/Financial.TestUtilities`.

## A second remote storage provider (beyond Google Drive)

- Implement `IRemoteFileClient` / `IRemoteFileClientFactory` in a new
  `Integrations/<Vendor>` project (`feedback_integration_sdk_isolation`). Tests: Unit for
  retry/translation logic, Integration for the DI registration and for `RemoteJsonStorage`
  over a delegate fake, one new `Repository:Provider` value in the DI tests. The vendor API is
  external — never called in tests.

## React error boundaries

- **Layer**: Unit (component) — render a child that throws, assert the fallback's
  `role="alert"` text and a retry/navigate action; `vi.spyOn(console, 'error')` to silence
  React's report. Page-level Integration: a hook rejection must not reach the boundary (the
  page's own error state handles it).

## MSW-style request mocking (`msw`)

- Not adopted. The module-level `vi.mock('../../api/financialApiClient')` is the sanctioned
  fake (`../references/mock-health-rules.md`); introducing MSW would need an ADR and a
  migration of every page test. If adopted, it remains a fake of the separate API deployable,
  so layer assignments do not change.

## Playwright test files beyond the smoke script

- `Financial.Web/scripts/smoke-test.mjs` is a plain Node script. If journeys multiply, move
  to `@playwright/test` with a `playwright.config.ts` whose `webServer` starts the published
  API on a non-8080 port (`feedback_never_smoke_test_against_live_port`). Each journey is E2E
  and needs one failure journey (`../references/negative-path-testing.md`).

## WPF UI automation (FlaUI / WinAppDriver)

- Would be the WPF E2E layer. Locate elements by `AutomationProperties.Name` (already required
  by `docs/ui/accessibility.md` and used 178 times in `Financial.App`), click via
  `GetClickablePoint()`, never by screenshot coordinates. Until it exists, the manual run in `docs/rules/ui.md` is the WPF E2E
  substitute.

## Database/EF Core migrations

- Not applicable while persistence is JSON. If a relational store ever arrives it is owned
  infrastructure: real database in Docker at Integration (Testcontainers), never faked; the
  example/seed-data contract tests (`example-and-seed-data.md`) become schema-migration tests.
