> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Example and Seed Data Files (`data/*.example.json`, `Tests/**/TestData/*.test.json`)

Test-data contract tests: a committed JSON file that a real loader must keep parsing.

| File | Who loads it | Guard |
|---|---|---|
| `data/data-investment.example.json` | README first-run copy | `Tests/Financial.Investment.Infrastructure.Tests/Persistence/ExampleDataFileTests.cs` |
| `data/data-cashflow.example.json` | README first-run copy (empty shell by design) | `Tests/Financial.CashFlow.Infrastructure.Tests/Persistence/ExampleDataFileTests.cs` |
| `Tests/Financial.TestUtilities/TestData/data.test.json` (+ per-project copies) | `TestDataPaths.DataJsonFile`; `ApiTestFactory` temp copy; Investment repository tests | every Investment Infrastructure / Api test |
| `Tests/Financial.Api.Tests/TestData/data-cashflow.test.json` | CI `smoke` job seed | the smoke run; `ApiTestFactory.SeededBanksJson` mirrors its ids |

## What to test

- The file exists in the repository (csproj `<Content Include="..\..\data\data-cashflow.example.json" Link="ExampleData\...">`
  links the tracked file, so the test reads the original, not a copy).
- It deserializes through the **real** loader + serializer (`CashFlowLoader.LoadSync(new LocalJsonStorage(path), new CashFlowSerializerAdapter())`,
  `InvestmentLoader.LoadSync(...)`) without throwing.
- It yields a usable document (Investment: at least the seeded broker; CashFlow: non-null).
- Negative: a deliberately corrupted temp copy (e.g. `"Country": ""`, the real historical
  failure) throws at load — proves the guard is not vacuous.
- When a migration tool adds a field, both example files and both `*.test.json` files are
  updated in the same PR and these tests still pass.

## Layer assignment

- **Integration** — real file, real loader, real serializer (fundamentals' "test-data contract
  test" row). No Unit (nothing to isolate), no E2E of its own (the smoke job is the consumer).

## Setup pattern

```csharp
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;

public class ExampleDataFileTests
{
    private static string ExampleFilePath =>
        Path.Combine(AppContext.BaseDirectory, "ExampleData", "data-cashflow.example.json");

    [Fact]
    public void ExampleFile_DeserializesThroughTheRealSerializer()
    {
        var storage = new LocalJsonStorage(ExampleFilePath);
        var serializer = new CashFlowSerializerAdapter();

        var act = () => CashFlowLoader.LoadSync(storage, serializer);

        act.Should().NotThrow(
            "a fresh checkout seeds from this file, so anything it cannot deserialize stops the app at startup");
    }
}
```

(Verbatim from the CashFlow `ExampleDataFileTests.cs`.) Tests that **mutate** seed data copy it
first: `File.Copy(TestDataPaths.DataJsonFile, Path.Combine(Path.GetTempPath(), $"financial-api-{Guid.NewGuid():N}.json"), true)`
as `ApiTestFactory.CreateTempDataFile` does — never write to the tracked file, and never point
any test at `data/data-cashflow.json` or `data/data-investment.json` (live data,
`feedback_never_run_migrations_against_live_data`).

## When to skip

- Asserting every field of the example files — one presence + one load + one usability
  assertion per file.
- Duplicating the seed JSON as a string in a second test project; reference
  `Financial.TestUtilities` and `TestDataPaths` instead.

## Examples from project

- `Tests/Financial.CashFlow.Infrastructure.Tests/Persistence/ExampleDataFileTests.cs` — Integration.
- `Tests/Financial.Investment.Infrastructure.Tests/Persistence/ExampleDataFileTests.cs` — Integration; the one that caught the `"Country": ""` startup crash.
- `Tests/Financial.Api.Tests/ApiTestFactory.cs` — temp-copy pattern and the seeded CashFlow JSON.
