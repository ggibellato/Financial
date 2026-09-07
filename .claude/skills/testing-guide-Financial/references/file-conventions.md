> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# File Naming, Directory Structure and Coverage Philosophy

## .NET

### Projects (all under `Tests/`, listed in `Financial.slnx`)

```
Financial.Investment.{Domain,Application,Infrastructure}.Tests
Financial.CashFlow.{Domain,Application,Infrastructure}.Tests
Financial.Shared.Infrastructure.Tests            ← LocalJsonStorage, DebouncedJsonStorage, RemoteJsonStorage, retry, hosting
Financial.Api.Tests                              ← endpoint Integration tests, Contract/, Controllers/ guard clauses, TestData/
Financial.Presentation.Tests                     ← WPF (net10.0-windows, UseWPF); run by the `wpf` CI job
Financial.Architecture.Tests                     ← fitness functions (references every project incl. Financial.App)
Financial.GoogleIntegrations.Tests, Financial.Observability.Tests, Financial.WebPageParser.Tests
Financial.CashFlowSpreadsheetImport.Tests, Financial.InvestmentSpreadsheetImport.Tests
Financial.TestUtilities                          ← shared doubles + TestData/data.test.json (not a test project)
```

Every test csproj declares `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4,
`Microsoft.NET.Test.Sdk` 17.14.1, `FluentAssertions` 6.12.0, `coverlet.collector` 6.0.4 and
`<Using Include="Xunit" />` (so no `using Xunit;` in files; `FluentAssertions` is imported
explicitly). No central package management. Folders inside a test project mirror the source
project (`Services/`, `Persistence/`, `Repositories/`, `DependencyInjection/`, `Validation/`,
`ViewModels/CashFlow/`, …).

### Naming

| Element | Convention | Example |
|---|---|---|
| Test file / class | `{Subject}Tests.cs` / `{Subject}Tests`; split by concern when large | `ExpenseServiceTests`, `TransactionServiceMutationTests` + `TransactionServiceQueryTests`, `MonthlyViewModelBanksCardsTests` |
| Test method | `{Method}_{Scenario}_{Outcome}` | `GetHistoricalRateAsync_WhenHttpRequestThrows_ReturnsNull`; architecture tests use `Domain_Should_Not_Reference_Infrastructure` |
| AC-tracing class / file | `{FeatureName}AcceptanceTests` under `Acceptance/` - no `P{NN}F{NN}` prefix, the AC id already carries that in its `[Trait]`/title | `Tests/Financial.Api.Tests/Acceptance/GoogleCalendarAccountConnectionAcceptanceTests.cs` |
| AC-tracing method | `[Trait("AC", "<id>")]` + `{Scenario}_{Outcome}` | see `feature-traceability.md` |
| Shared doubles | `Stub{Interface}`, `Recording{Thing}`, `Fake{Thing}` in `Tests/Financial.TestUtilities` | `StubCashFlowRepository`, `RecordingLogger<T>`, `FakeTimeProvider` |
| Local doubles (single consumer) | same prefixes, `private sealed class` at the bottom of the test file, or `TestStubs.cs` / `TestDoubles/` per test project | `FakeHttpMessageHandler`, `Financial.Presentation.Tests/ViewModels/CashFlow/TestStubs.cs`, `Financial.Investment.Infrastructure.Tests/TestDoubles/StubFinanceService.cs` |
| Host factory | `ApiTestFactory : WebApplicationFactory<Program>`; tests derive from `ApiEndpointTests` | — |
| Temp data | `Path.Combine(Path.GetTempPath(), $"{area}-{Guid.NewGuid()}.json")`, deleted in `finally` | `cashflow-repo-{guid}.json`, `financial-api-{guid:N}.json` |

### Layer by location (no attribute needed)

- Unit: `*.Domain.Tests`, `*.Application.Tests`, `Presentation.Tests/{ViewModels,Converters,Behaviors,Helpers,Input,Navigation}`, `WebPageParser.Tests`, parsing/resolver folders of the import tools.
- Integration: `Api.Tests` (except `Controllers/`), `*.Infrastructure.Tests/{Repositories,Persistence,DependencyInjection}`, `Shared.Infrastructure.Tests`, `Architecture.Tests`, `Observability.Tests`, `Presentation.Tests/{DependencyInjection,Views,Acceptance}`, import-tool `SheetImporters/` and `Migrations/`.
- AC-tracing: `Acceptance/` folders + the `AC` trait.
- E2E: nothing in `Tests/` — `Financial.Web/scripts/smoke-test.mjs`.

### Commands

```
dotnet test                                                            # everything (Presentation.Tests needs Windows)
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
src/navigation/__tests__/routes.test.ts
src/pages/__tests__/*.test.tsx
src/utils/__tests__/*.test.ts
src/acceptance/{feature-slug}.test.tsx                                 ← AC-tracing (to be created)
src/test/renderWithFluent.tsx, src/test-utils/selectedNodeTestWrapper.tsx, src/setupTests.ts
```

Every folder uses a `__tests__/` subfolder (the old colocated pattern is gone). Config lives
in `vite.config.ts` (`test: { environment: 'jsdom', setupFiles: './src/setupTests.ts', coverage: { provider: 'v8', all: true, reporter: ['text','json-summary'], exclude: [... 'src/api/generated/**', 'src/main.tsx', 'src/setupTests.ts'] } }`);
there is no `vitest.config.ts` and no `playwright.config.ts`.

### Naming

| Element | Convention | Example |
|---|---|---|
| File | `{Name}.test.tsx` / `.test.ts` | `PaymentDueBanner.test.tsx`, `useBanks.test.ts` |
| `describe` | subject name | `describe('usePaymentsDue', …)` |
| `it` | either `snake_case_scenario` or a behaviour sentence — both exist; pick the file's own style | `it('no_banner_when_payments_is_empty_array')`, `it('shows an error state with retry on load failure')` |
| AC-tracing `it` | `'[AC <id>] <sentence>'` | `it('[AC P42-F02-payment-due-banner-web-03] renders nothing when the list is empty')` |
| Mocks | `{method}Mock` from `vi.hoisted`, typed `vi.fn<FinancialApiClient['method']>()` | `getBanksMock` |

### Commands

```
npm test                  # vitest run
npm run test:watch
npm run test:coverage     # what the web CI job runs
npm run build             # tsc -b && vite build — the type-check gate (run after every change)
npm run lint
npx vitest list -t "\[AC P42-F02"    # AC audit
npm run smoke-test        # Playwright, needs the published API up (see e2e-environment.md)
```

## Coverage philosophy — thorough, CI bands as targets

Decided 2026-09-06. The CI coverage gate (`Check coverage threshold` step in each of the
`backend`, `wpf`, `web` jobs, `continue-on-error: true`) bands line coverage:

| Band | Line coverage | Meaning |
|---|---|---|
| 🟢 green | 100% | target for every new artifact's own tests |
| 🟡 yellow | 95–99.99% | acceptable; note what is uncovered in the PR |
| 🟠 amber | 90–94.99% | fix before the next feature on that area |
| 🔴 red | < 90% | the step fails visibly; treat as a defect to fix in the same PR |

Measured whole-repo per job: `backend` = every .NET assembly except `Financial.Presentation.App`
(`-assemblyfilters:+*;-Financial.Presentation.App`), `wpf` = `Financial.Presentation.App` only,
`web` = `coverage/coverage-summary.json` `total.lines.pct`. `coverlet.runsettings` excludes
generated `obj/**` code and the assemblies `Financial.TestUtilities`,
`Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport`,
`Financial.Investment.SpreadsheetImport`, `Financial.Integrations.GoogleSheets`.

Concrete rules that follow:

- A new class ships with tests that cover 100% of its own lines except documented
  exclusions (Google SDK clients, `Tools/ImportGoogleSpreadSheets`, WPF code-behind).
- Every branch has both directions tested (`negative-path-testing.md`); coverage alone does
  not satisfy this — a 100%-covered branch with only its success assertion is still incomplete.
- Every §9 acceptance criterion has a tagged Integration test (`feature-traceability.md`).
- Local reproduction: `dotnet test --settings coverlet.runsettings --results-directory TestResults`
  then `reportgenerator` as in `.github/workflows/build.yml`; `npm run test:coverage` for the web.
