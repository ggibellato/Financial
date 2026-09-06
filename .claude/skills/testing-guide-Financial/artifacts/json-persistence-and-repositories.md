> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# JSON Persistence and Repositories (`*/Persistence/*.cs`, `*/Repositories/*.cs`, `Financial.Shared.Infrastructure/Persistence/*.cs`)

## What to test

- **Repository ↔ storage ↔ serializer round-trip**: `CashFlowJsonRepository.ApplyAndSaveAsync(Func<bool>)`
  writes serialized data through `LocalJsonStorage` and reads back a document with the entity in
  it; `saved == false` and zero writes when the apply callback reports no change.
- **Rejected write**: `LocalJsonStorage` on a path whose directory does not exist →
  `DirectoryNotFoundException` propagates; `ReadAsync` on a missing file → `FileNotFoundException`.
- **Serializer contract**: `CashFlowSerializerAdapter` / `InvestmentSerializerAdapter`
  round-trip every collection and preserve reference identity (an expense's `Bank` is the same
  instance as in `Banks`); a JSON id absent from the lookup → `JsonException` naming the id
  (`*ReferenceConverter` tests); `CashFlowTypeInfoResolver` enables private constructors and
  leaves computed properties unwired.
- **Loaders**: `CashFlowLoader.LoadSync(IJsonStorage storage, ICashFlowSerializer serializer)` /
  `InvestmentLoader.LoadSync` on a temp file and on the example files (see
  `example-and-seed-data.md`).
- **Debounce and retry timing** (`DebouncedJsonStorage(IJsonStorage inner, TimeSpan debounceWindow, TimeProvider? timeProvider = null, ITelemetryTracer? tracer = null, ILogger? logger = null)`):
  `WriteAsync` returns immediately with `SyncState.Pending`; only the latest JSON is uploaded
  after the window; a write during the window resets it; a write during an in-flight save starts
  a follow-up cycle; `TransientStorageException` is retried by `TransientRetryPolicy`;
  `GetStatus()` reports `LastError` after exhausting retries; `FlushAsync` on shutdown
  (`ShutdownFlushHostedService`).
- **Remote storage** (`RemoteJsonStorage`): delegates to `IRemoteFileClient` download/upload with
  the configured path; blank path rejected; transient Google errors translated.
- **Factories**: `JsonStorageFactory`, `CashFlowRepositoryFactory`, `InvestmentRepositoryFactory`
  choose local vs remote from `*RepositorySelectionOptions`.

## Layer assignment

- **Integration** for everything touching `LocalJsonStorage`, `DebouncedJsonStorage`, loaders
  and repositories: the file system is owned infrastructure and stays real (a `Guid`-named temp
  file under `Path.GetTempPath()`, deleted in `finally`). Time is the one non-determinism,
  controlled with `ObservableFakeClock` (Shared.Infrastructure.Tests) or
  `Microsoft.Extensions.Time.Testing.FakeTimeProvider`.
- **Unit** for `*ReferenceConverter`, `CashFlowTypeInfoResolver`, `ReferenceResolutionContext`
  and `PathResolution` — pure `System.Text.Json` plumbing with an in-memory lookup.
- `RemoteJsonStorage` is tested at **Integration with the Google client faked** via its
  delegate constructor (`new RemoteJsonStorage(download, upload, remotePath)`) — Google Drive is
  an external provider (`../references/external-providers.md`).
- No E2E of its own; the smoke job's seeded JSON files exercise the real path end to end.

## Setup pattern

```csharp
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;

[Fact]
public async Task ApplyAndSaveAsync_WritesSerializedDataThroughStorage()
{
    var path = Path.Combine(Path.GetTempPath(), $"cashflow-repo-{Guid.NewGuid()}.json");
    var storage = new LocalJsonStorage(path);
    var serializer = new CashFlowSerializerAdapter();
    var data = CashFlowData.Create();
    var repository = new CashFlowJsonRepository(data, storage, serializer);

    try
    {
        var bank = Bank.Create("Chase", roundUpEnabled: true);
        var category = Category.Create("Casa");
        data.AddBank(bank);
        data.AddCategory(category);
        repository.AddExpense(Expense.Create(new DateOnly(2026, 7, 1), "Test expense", 10m, category, bank, null));

        await repository.ApplyAndSaveAsync(() => true);

        var written = await storage.ReadAsync();
        serializer.Deserialize(written).Expenses.Should().ContainSingle();
    }
    finally
    {
        File.Delete(path);
    }
}
```

(Verbatim from `Tests/Financial.CashFlow.Infrastructure.Tests/Repositories/CashFlowJsonRepositoryTests.cs`.)
For timing, follow `DebouncedJsonStorageTests`: construct `ControllableJsonStorage` +
`RecordingTelemetryTracer` in the test constructor, then per test
`new DebouncedJsonStorage(_inner, TimeSpan.FromMilliseconds(150), clock)`, wait on a monotonic
counter (`clock.TimersArmed`, `_inner.WrittenJson.Count`) before `clock.Advance(...)`.

## When to skip

- `System.Text.Json` itself — only your converters/resolvers and the reference-identity rule.
- A repository query that is a straight `List` lookup with no branch — covered by the round-trip.
- Testing `LocalJsonStorage` again inside every repository test — one storage suite exists
  (`LocalJsonStorageTests`); repository tests may use `RecordingJsonStorage` (an in-memory
  `IJsonStorage` local to that test file) when the assertion is about *whether* a write happened.

## Examples from project

- `Tests/Financial.CashFlow.Infrastructure.Tests/Repositories/CashFlowJsonRepositoryTests.cs` — Integration; round-trip, rejected write, no-change short-circuit.
- `Tests/Financial.Investment.Infrastructure.Tests/Repositories/InvestmentJsonRepositoryTests.cs` — Integration over `TestDataPaths.DataJsonFile` (read-only) and temp copies for mutations.
- `Tests/Financial.Shared.Infrastructure.Tests/Persistence/DebouncedJsonStorageTests.cs` + `ObservableFakeClock.cs` + `ControllableJsonStorage.cs` — Integration; fake-clock debounce and retry.
- `Tests/Financial.Shared.Infrastructure.Tests/Persistence/LocalJsonStorageTests.cs` — Integration; read/write/missing-file.
- `Tests/Financial.Shared.Infrastructure.Tests/Persistence/RemoteJsonStorageTests.cs` — Integration with the Drive client faked by delegates.
- `Tests/Financial.CashFlow.Infrastructure.Tests/Persistence/BankReferenceConverterTests.cs`, `CashFlowTypeInfoResolverTests.cs`, `CashFlowSerializerAdapterTests.cs` — Unit / Integration serializer contracts.
- `Tests/Financial.Shared.Infrastructure.Tests/Hosting/ShutdownFlushHostedServiceTests.cs` — Unit; flush called only for `ISyncStatusProvider` repositories.
