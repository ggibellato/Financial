> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Infrastructure Persistence (`*Repository.cs`, `*Storage.cs`)

Examples: `JSONRepository`, `LocalJsonStorage`, `GoogleDriveJsonStorage` (`Financial.Shared.Infrastructure/Persistence`), CashFlow's equivalent repository/storage classes.

## What to test

- CRUD operations round-trip correctly against the real backing store
- File-not-found / malformed-content error paths
- Path resolution edge cases (null path → default filename, directory path → appends default filename, relative path → resolves against base directory) — see `LocalJsonStorageTests` for the full matrix
- `GoogleDriveJsonStorage`: that read/write delegate to the correct drive path with the correct content — without touching the real Google Drive API

## Layer assignment

Integration — this is a system boundary (the filesystem, or a storage abstraction over Drive), even though there's no branching logic in most CRUD methods. Two different real-vs-fake choices apply depending on which system:

| System | Strategy | Why |
|---|---|---|
| Local JSON file | **Real** — temp file copy of test seed data | Filesystem I/O in Docker/CI is fast (<10ms) and fully controllable; no reason to fake it |
| Google Drive | **Real class, fake delegates** — `GoogleDriveJsonStorage` takes `Func<string,string>`/`Action<string,string>` download/upload delegates in its constructor, so tests inject in-memory delegates instead of a real Drive client | Proves the storage class's own contract (path threading, no-download-on-write, etc.) without needing live Drive credentials — see `references/external-systems.md` |

## Setup pattern

**Local JSON (real temp file):**

```csharp
var tempFile = Path.Combine(Path.GetTempPath(), $"data.test.{Guid.NewGuid():N}.json");
File.Copy(TestDataPaths.DataJsonFile, tempFile, true);
try
{
    var storage = new LocalJsonStorage(tempFile);
    // act + assert
}
finally
{
    File.Delete(tempFile); // always in finally — never in an assert block
}
```

**Google Drive (fake delegates, no real API call):**

```csharp
string? capturedPath = null;
var storage = new GoogleDriveJsonStorage(
    path => { capturedPath = path; return "{\"data\":true}"; },
    (_, _) => throw new InvalidOperationException("upload should not be called"),
    "Pessoais/Gleison/Financeiros");

var result = await storage.ReadAsync();

result.Should().Be("{\"data\":true}");
capturedPath.Should().Be("Pessoais/Gleison/Financeiros");
```

## When to skip

- Don't unit-test the repository's business logic separately if it's already exercised end-to-end via `artifacts/api-endpoints-e2e.md` for the same code path — pick the layer whose question ("is the contract right?" vs "is the HTTP behavior right?") the test actually answers
- Don't write a test proving `File.Delete` was called — that's wiring, not behavior

## Examples from project

- `LocalJsonStorageTests` — 6 tests covering read/write plus 4 distinct path-resolution branches
- `GoogleDriveJsonStorageTests` — constructor null-checks + delegate-threading tests, no real network call anywhere
