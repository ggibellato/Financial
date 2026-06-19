> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# External Systems Test Strategy

This project has **one external system**: the local JSON file that stores investment data. There are no databases, no Redis, no message queues, no external HTTP APIs in the backend.

## Local JSON File Storage (C#)

**Strategy: real temp file, real implementation.**

| Consideration | Decision |
|---|---|
| Complexity | Low — JSON read/write, no schema migration |
| Speed | Fast — file I/O < 10ms for test data size |
| Isolation | Achieved via `Guid`-named temp file per test |
| Cleanup | `File.Delete(tempFile)` in `finally` block |
| Why not a fake? | No mocking framework in this project; file I/O is simple enough that faking it adds no benefit |

### Setup

```csharp
var tempFile = Path.Combine(Path.GetTempPath(), $"data.test.{Guid.NewGuid():N}.json");
File.Copy(TestDataPaths.DataJsonFile, tempFile, true);

var storage = new LocalJsonStorage(tempFile);
var serializer = new InvestmentsSerializerAdapter();
var repository = new JSONRepository(
    InvestmentsLoader.LoadSync(storage, serializer),
    storage,
    serializer);
```

### Test Data

`TestData/data.test.json` is the seed file. It contains a minimal but representative dataset: at least one broker ("XPI"), one portfolio ("Default"), one asset ("BCIA11"), and existing transactions/credits.

**Do not modify `data.test.json` directly in tests** — always write to the temp copy. The seed file must remain stable so all tests start from the same known state.

### Cleanup

```csharp
try
{
    // act + assert
}
finally
{
    File.Delete(tempFile);  // runs whether the test passes or fails
}
```

## HTTP API Client (TypeScript)

The React frontend calls the .NET API. In tests, the API client is mocked at the module boundary:

```typescript
vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getAssetDetails: getAssetDetailsMock,
    // include only methods used by the component under test
  }),
}))
```

This eliminates all network dependency from frontend tests. There is no Docker setup or test server for frontend unit tests — the mock replaces the HTTP call entirely.
