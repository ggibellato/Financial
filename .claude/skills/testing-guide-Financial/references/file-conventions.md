> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# File Naming, Directory Structure and Coverage Philosophy

## .NET

### Projects (all under `Tests/`, listed in `Financial.slnx`)

```
Financial.Investment.{Domain,Application,Infrastructure}.Tests
Financial.CashFlow.{Domain,Application,Infrastructure}.Tests
Financial.Shared.Abstractions.Tests              ← FX composition/caching, RetryPolicy, repository settings, TimeProvider extensions (Unit)
Financial.Shared.Infrastructure.Tests            ← LocalJsonStorage, DebouncedJsonStorage, RemoteJsonStorage, FX rate store, retry, hosting
Financial.Api.Tests                              ← host tests, Acceptance/ (AC-tracing), Contract/, ConstructorGuardTests, TestData/
Financial.Architecture.Tests                     ← fitness functions (references every project)
Financial.GoogleIntegrations.Tests, Financial.Observability.Tests, Financial.WebPageParser.Tests, Financial.Frankfurter.Tests
Financial.CashFlowSpreadsheetImport.Tests, Financial.InvestmentSpreadsheetImport.Tests, Financial.InvestmentDataQualityReport.Tests
Financial.TestUtilities                          ← shared doubles + TestData/data.test.json (not a test project)
```

Every test csproj declares `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4,
`Microsoft.NET.Test.Sdk` 17.14.1, `FluentAssertions` 6.12.0, `coverlet.collector` 6.0.4 and
`<Using Include="Xunit" />` (so no `using Xunit;` in files; `FluentAssertions` is imported
explicitly). No central package management. `Tests/Directory.Build.targets` adds
`TestCultureInitializer` (honours `TEST_CULTURE`) and `[UseCulture]` to every project. Folders inside a test project mirror the source
project (`Services/`, `Persistence/`, `Repositories/`, `DependencyInjection/`, `Validation/`, …).

### Naming

| Element | Convention | Example |
|---|---|---|
| Test file / class | `{Subject}Tests.cs` / `{Subject}Tests`; split by concern when large | `ExpenseServiceTests`, `TransactionServiceMutationTests` + `TransactionServiceQueryTests` |
| Test method | `{Method}_{Scenario}_{Outcome}` | `GetHistoricalRateAsync_WhenHttpRequestThrows_ReturnsNull`; architecture tests use `Domain_Should_Not_Reference_Infrastructure` |
| AC-tracing class / file | `{FeatureName}AcceptanceTests` under `Acceptance/` - no `P{NN}F{NN}` prefix, the AC id already carries that in its `[Trait]`/title | `Tests/Financial.Api.Tests/Acceptance/GoogleCalendarAccountConnectionAcceptanceTests.cs` |
| AC-tracing method | `[Trait("AC", "<id>")]` + `{Scenario}_{Outcome}` | see `feature-traceability.md` |
| Shared doubles | `Stub{Interface}`, `Recording{Thing}`, `Fake{Thing}` in `Tests/Financial.TestUtilities` | `StubCashFlowRepository`, `RecordingLogger<T>`, `FakeTimeProvider` |
| Local doubles (single consumer) | same prefixes, `private sealed class` at the bottom of the test file, or a `TestDoubles/` folder per test project; move it to TestUtilities the moment a second project needs it | `Financial.Investment.Infrastructure.Tests/TestDoubles/StubFinanceService.cs`, `Financial.GoogleIntegrations.Tests/FakeDriveHandler.cs` |
| Host factory | `ApiTestFactory : WebApplicationFactory<Program>`; tests derive from `ApiEndpointTests` | — |
| Temp data | `Path.Combine(Path.GetTempPath(), $"{area}-{Guid.NewGuid()}.json")`, deleted in `finally` | `cashflow-repo-{guid}.json`, `financial-api-{guid:N}.json` |

### Layer by location (a guide only - the `Category` trait is the authority)

The `Category` trait on each class is what CI and `--filter` use (`docs/rules/implementation.md` §Tests #9); the list below says which level is normal where.

The rule (P58-F07 D2): **Unit** = in process, no host, no real file or network; **Integration** =
real temp file, real host (`ApiEndpointTests` / `ApiTestFactory`), or loading full real assemblies;
**Live** = real third-party sites.

- Unit: `*.Domain.Tests`, `*.Application.Tests`, `Architecture.Tests` (except
  `AccountTemplateReadRuleTests`), `Observability.Tests`, `Frankfurter.Tests`,
  `GoogleIntegrations.Tests`, `Shared.Abstractions.Tests`, host-less `Api.Tests` classes
  (`ConstructorGuardTests`), serializer/converter/resolver classes in the Infrastructure projects,
  the import tools' parsing, argument and report classes.
- Integration: host-based `Api.Tests` classes, Infrastructure repository/persistence/DI/real-JSON
  service classes, `Shared.Infrastructure.Tests` storage classes, import-tool command/backup classes.
- Live: `WebPageParser.Tests/*VerificationTests` (nightly `live` job only).
- AC-tracing: `Acceptance/` folders + the `AC` trait (any level, normally Integration).
- E2E: nothing in `Tests/` — `Financial.Web/tests/e2e/*.spec.ts` (`@playwright/test`).

### Commands

```
dotnet test                                                            # everything
dotnet test Tests/Financial.CashFlow.Domain.Tests                      # one project
dotnet test --filter "FullyQualifiedName~ExpenseTests.Should_Reject_Negative_Value"
dotnet test --filter "AC~P42-F01" --list-tests                         # AC audit (see feature-traceability.md)
dotnet test --settings coverlet.runsettings --results-directory TestResults   # with coverage, as CI
```

## Financial.Web (Vitest 4.1, RTL 16.3, jsdom 29, user-event 14.6)

### Layout

```
src/__tests__/App.test.tsx
src/api/__tests__/{config,financialApiClient}.test.ts
src/api/generated/__tests__/openapiFreshness.test.ts
src/components/__tests__/*.test.tsx,  src/components/grid/__tests__/*.test.tsx
src/context/__tests__/*.test.tsx
src/hooks/__tests__/*.test.ts
src/navigation/__tests__/routes.test.tsx
src/pages/__tests__/*.test.tsx
src/utils/__tests__/*.test.ts
src/acceptance/{feature-slug}.test.tsx                                 ← AC-tracing (most web AC tests carry the [AC …] title in place, in the component/page test)
src/test/renderWithFluent.tsx, src/test/{account,recurringBill,…}.ts   ← builders
src/test-utils/{selectedNodeTestWrapper,renderWithSelectedNode}.tsx, confirmDelete.ts, pinDate.ts, …
src/setupTests.ts
tests/e2e/*.spec.ts, tests/e2e/{fixtures,global-setup}.ts                ← Playwright (E2E)
```

Every folder uses a `__tests__/` subfolder. Vitest config lives in `vite.config.ts`
(`test: { environment: 'jsdom', exclude: [..., 'tests/e2e/**'], setupFiles: './src/setupTests.ts', coverage: { provider: 'v8', all: true, reporter: ['text','json-summary','lcov'], exclude: [... 'src/api/generated/**', 'src/main.tsx', 'src/setupTests.ts', 'src/test/**', 'src/test-utils/**'] } }`).
Playwright: `playwright.config.ts` (`testDir: './tests/e2e'`, `globalSetup`, `baseURL` from
`SMOKE_APP_URL`, `en-GB` / `Europe/London`, CI retries 2). Mutation: `stryker.config.json` +
`vitest.mutation.config.ts` (nightly).

### Naming

| Element | Convention | Example |
|---|---|---|
| File | `{Name}.test.tsx` / `.test.ts` | `PaymentDueBanner.test.tsx`, `useCategories.test.ts` |
| `describe` | subject name | `describe('usePaymentsDue', …)` |
| `it` | either `snake_case_scenario` or a behaviour sentence — both exist; pick the file's own style | `it('no_banner_when_payments_is_empty_array')`, `it('shows an error state with retry on load failure')` |
| AC-tracing `it` | `'[AC <id>] <sentence>'` | `it('[AC P42-F02-payment-due-banner-web-03] renders nothing when the list is empty')` |
| Mocks | `{method}Mock` from `vi.hoisted`, typed `vi.fn<FinancialApiClient['method']>()` | `getCategoriesMock` |

### Commands

```
npm test                  # vitest run
npm run test:watch
npm run test:coverage     # what the web CI job runs
npm run build             # tsc -b && vite build — the type-check gate (run after every change)
npm run lint
npx vitest list -t "\[AC P42-F02"    # AC audit
npm run smoke-test        # Playwright @smoke specs, needs the published API on test data (see e2e-environment.md)
```

## Coverage philosophy — thorough, and coverage is a tripwire, not a target

Confirmed 2026-11-14. "Thorough" means every behaviour that can break is tested, at the lowest
layer that gives confidence, with its failure side. It does **not** mean adding tests to raise a
number: a test written only to cover lines fails the test-value review in `../SKILL.md` §2 and is
rejected even when it turns a red figure green. A low figure tells you where to look for an untested
rule; the fix is a test of that rule, or deleting dead code.

Each CI job runs the shared `.github/actions/coverage-gate` over a `reportgenerator`
`Summary.json`. It shows line and branch coverage behind the icon below and fails when line coverage
is below 90%, or when line **or** branch coverage drops more than 0.5 points below the job's entry in
`coverage-baseline.json` (refreshed only from a green `main` run by
`.github/scripts/coverage-baseline.ps1`). The PR comment adds the coverage of changed lines
(advisory) and warns when a PR adds coverage exclusions.

| Icon | Coverage (line or branch) | Meaning |
|---|---|---|
| 🟢 green | ≥ 90% | healthy |
| 🟡 yellow | 85–89.99% | slipping; note what is uncovered in the PR |
| 🟠 amber | 80–84.99% | fix before the next feature on that area |
| 🔴 red | < 80% | treat as a defect to fix in the same PR |

Measured whole-repo per job: `backend` = every .NET assembly (`-assemblyfilters:+*`), `web` =
`reportgenerator` over vitest's `lcov`. `coverlet.runsettings` excludes generated `obj/**` code and
the assemblies `Financial.TestUtilities`,
`Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport`,
`Financial.Investment.SpreadsheetImport`, `Financial.Investment.DataQualityReport`,
`Financial.Integrations.GoogleSheets`, plus `GoogleFinanceVerifier`. Mutation testing (Stryker.NET on
both Domain projects, StrykerJS on the web) runs nightly and is the better signal of whether the
assertions would notice a change.

Concrete rules that follow:

- A new class ships with tests for every behaviour that can break, which normally covers its
  lines; a line left uncovered is either a missed rule (test it) or dead code (delete it), never
  "covered" with an assertion-free call.
- Every branch has both directions tested (`negative-path-testing.md`); coverage alone does
  not satisfy this — a 100%-covered branch with only its success assertion is still incomplete.
- Every §9 acceptance criterion has a tagged Integration test (`feature-traceability.md`).
- Local reproduction: `dotnet test --settings coverlet.runsettings --results-directory TestResults`
  then `reportgenerator` as in `.github/workflows/build.yml`; `npm run test:coverage` for the web.
