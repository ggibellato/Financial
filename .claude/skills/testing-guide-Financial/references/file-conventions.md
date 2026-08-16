> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# File Naming and Directory Conventions

## C# (.NET)

### Project structure

Tests live in **separate projects** under `Tests/`, one per source project, mirroring the solution's DDD-domain split:

```
Tests/
├── Financial.Investment.Domain.Tests/
├── Financial.Investment.Application.Tests/
├── Financial.Investment.Infrastructure.Tests/     ← includes Integrations (GoogleFinancialSupport, WebPageParser)
├── Financial.CashFlow.Domain.Tests/
├── Financial.CashFlow.Application.Tests/
├── Financial.CashFlow.Infrastructure.Tests/
├── Financial.CashFlowSpreadsheetImport.Tests/      ← Tools/CashFlowSpreadsheetImport
├── Financial.Shared.Infrastructure.Tests/          ← LocalJsonStorage, GoogleDriveJsonStorage
├── Financial.Presentation.Tests/                   ← WPF ViewModels/Converters/Helpers
└── Financial.Api.Tests/                            ← Controllers (guard clauses) + E2E EndpointsTests via ApiTestFactory
```

Each test project references only its counterpart source project(s) via `<ProjectReference>`. Central package management is not used (no `Directory.Packages.props`) — each test `.csproj` declares its own package versions (currently `xunit` 2.9.3, `FluentAssertions` 6.12.0, targeting `net10.0`).

### Naming conventions

| Element | Convention | Example |
|---|---|---|
| Test file | `{SubjectName}Tests.cs` | `AssetTests.cs`, `CreditTypeParserTests.cs`, `DividendEndpointsTests.cs` |
| Test class | `{SubjectName}Tests` | `public class AssetTests` |
| Test method | `{MethodName}_{Condition}_{ExpectedResult}` | `AddTransaction_Buy_UpdatesAveragePriceAndQuantity` |
| Test doubles | `Stub{Interface}` in a `TestDoubles/` folder | `TestDoubles/StubRepository.cs`, `TestDoubles/StubFinanceService.cs` |
| E2E test factory | `{Area}TestFactory : WebApplicationFactory<Program>` | `ApiTestFactory` |

### Global usings

xUnit is available globally via `.csproj` `<Using Include="Xunit" />`. Do **not** add `using Xunit;` in individual test files. FluentAssertions must still be explicitly imported.

### Test data

```
Tests/Financial.Investment.Infrastructure.Tests/TestData/data.test.json
Tests/Financial.Api.Tests/TestData/data.test.json (+ a hardcoded seeded-banks JSON string in ApiTestFactory for CashFlow)
```

Access via a `TestDataPaths` helper:
```csharp
internal static class TestDataPaths
{
    public static string DataJsonFile => Path.Combine(AppContext.BaseDirectory, "TestData", "data.test.json");
}
```

**Never modify the seed file directly in a test** — always write to a `Guid`-named temp copy so the seed stays stable across the whole suite.

---

## TypeScript (React)

### Directory structure

Two patterns coexist:

```
Financial.Web/src/
├── api/
│   ├── financialApiClient.ts
│   └── financialApiClient.test.ts        ← colocated, no __tests__ folder
├── App.tsx
├── App.test.tsx                           ← colocated at src root
├── components/
│   ├── ErrorState.tsx
│   └── __tests__/
│       └── ErrorState.test.tsx            ← __tests__ subfolder
├── context/
│   └── __tests__/SelectedNodeContext.test.tsx
├── hooks/
│   ├── useAggregatedSummary.ts
│   └── useAggregatedSummary.test.ts       ← colocated, no __tests__ folder
└── test-utils/
    └── selectedNodeTestWrapper.tsx        ← shared test helpers (context wrappers, etc.)
```

`components/` and `context/` use a `__tests__/` subfolder; `api/`, `hooks/`, and `App.tsx` colocate the test file directly next to the source file. Follow whichever pattern the sibling files in that directory already use.

### Naming conventions

| Element | Convention | Example |
|---|---|---|
| Test file | `{Name}.test.tsx` or `{Name}.test.ts` | `BanksGrid.test.tsx`, `useAggregatedSummary.test.ts` |
| Describe block | Subject name | `describe('useAggregatedSummary', () => {` |
| Test name | User/behavior-centric description | `it('displays broker list after API resolves')` |
| Shared test helpers | `src/test-utils/` | `createSelectedNodeWrapper` |

### Test runner commands

```bash
npm test           # single run (CI) — vitest run
npm run test:watch # watch mode (development)
```

### Configuration

| File | Purpose |
|---|---|
| `vite.config.ts` | Vitest config lives inline (`test: { environment: 'jsdom', setupFiles: './src/setupTests.ts' }`) — no separate `vitest.config.ts` |
| `src/setupTests.ts` | Global setup: jest-dom matchers, `ResizeObserver` mock for Recharts |

Stack versions at time of writing: React 19.2.4, `@testing-library/react` 16.3.2, `@testing-library/user-event` 14.6.1, Vitest 4.1.4, TypeScript ~6.0.2.

### Coverage philosophy

**Pragmatic** — test business-critical paths, branching logic, and system boundaries; skip trivial or low-risk code. No specific coverage % target, consistent with CLAUDE.md's "does not over-engineer" guidance for this single-user personal project.
