> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# File Naming and Directory Conventions

## C# (.NET)

### Project structure

Tests live in **separate projects** under `Tests/` — not colocated with source files:

```
Tests/
├── Financial.Domain.Tests/        ← pure domain logic tests
├── Financial.Application.Tests/   ← parsers, validators, use case handlers
├── Financial.Infrastructure.Tests/ ← services, repositories, serialization
└── Financial.Api.Tests/           ← WebApplicationFactory endpoint tests (out of scope for unit guide)
```

Each test project references only its counterpart source project.

### Naming conventions

| Element | Convention | Example |
|---|---|---|
| Test file | `{SubjectName}Tests.cs` | `AssetTests.cs`, `CreditTypeParserTests.cs` |
| Test class | `{SubjectName}Tests` | `public class AssetTests` |
| Test method | `{MethodName}_{Condition}_{ExpectedResult}` | `AddTransaction_Buy_UpdatesAveragePriceAndQuantity` |
| Factory helper | `Create{SubjectName}()` | `private static (CreditService, string) CreateService()` |

### Global usings

xUnit is available globally via `.csproj` `<Using Include="Xunit" />`. Do **not** add `using Xunit;` in individual test files.

FluentAssertions and other namespaces must still be explicitly imported unless added to `GlobalUsings.cs`.

### Test data

```
Tests/Financial.Infrastructure.Tests/
└── TestData/
    └── data.test.json    (CopyToOutputDirectory: Always)

Tests/Financial.Api.Tests/
└── TestData/
    └── data.test.json    (CopyToOutputDirectory: PreserveNewest)
```

Access via:
```csharp
internal static class TestDataPaths
{
    public static string DataJsonFile =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "data.test.json");
}
```

---

## TypeScript (React)

### Directory structure

Tests live in `__tests__/` subdirectories next to the files they test:

```
Financial.Web/src/
├── pages/
│   ├── BrokersPage.tsx
│   └── __tests__/
│       └── BrokersPage.test.tsx
├── components/
│   ├── ErrorState.tsx
│   └── __tests__/               ← create when adding component tests
│       └── ErrorState.test.tsx
├── api/
│   ├── config.ts
│   └── __tests__/               ← create when adding utility tests
│       └── config.test.ts
├── App.tsx
└── App.test.tsx                  ← colocated at src root
```

### Naming conventions

| Element | Convention | Example |
|---|---|---|
| Test file | `{ComponentName}.test.tsx` or `{FileName}.test.ts` | `BrokersPage.test.tsx`, `config.test.ts` |
| Describe block | Component name | `describe('BrokersPage', () => {` |
| Test name | User-centric description | `it('displays broker list after API resolves')` |

### Test runner commands

```bash
npm test           # single run (CI)
npm run test:watch # watch mode (development)
```

### Configuration files

| File | Purpose |
|---|---|
| `vite.config.ts` | Vitest config (test environment: jsdom, setup file) |
| `src/setupTests.ts` | Global setup: jest-dom matchers + ResizeObserver mock |
| `tsconfig.app.json` | Includes `@testing-library/jest-dom` types |

### Coverage philosophy

Pragmatic — test business-critical paths and user-visible behavior. No specific coverage % target. Focus test effort on pages with complex interactions and domain-logic-adjacent code.
