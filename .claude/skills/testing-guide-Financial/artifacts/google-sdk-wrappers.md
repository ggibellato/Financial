> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Google SDK Wrappers (`Integrations/GoogleCore/*.cs`, `Integrations/GoogleDrive/*.cs`, `Integrations/GoogleSheets/*.cs`)

## What to test

- **Retry policy** (`GoogleRetryPolicy.ExecuteWithRetryAsync`): immediate success → one call;
  `GoogleApiException` with `HttpStatusCode.TooManyRequests` → retried, logger invoked, result
  returned; non-transient error → no retry, exception propagates; retries exhausted → last
  exception surfaces.
- **Transient translation** (`GoogleTransientErrorTranslator.ThrowIfTransient`): 429/5xx →
  `TransientStorageException` (so `TransientRetryPolicy` upstream retries); 404/403 → original
  exception untouched.
- **Value parsing** (`GoogleSheetValueParser`): numbers with Portuguese decimal commas, dates,
  blanks, `#N/A`-style cells — each rejected form.
- **DI**: `AddGoogleDriveFileClient()` registers `GoogleFileClientFactory` as
  `IRemoteFileClientFactory`.
- **Consumers of the abstractions**, tested with a stub `IGoogleSheetsDataSource` /
  `IGoogleDriveFileSource` / `IRemoteFileClient` — see `spreadsheet-import-tools.md` and
  `json-persistence-and-repositories.md` (`RemoteJsonStorage`).

## Layer assignment

- **Unit** for `GoogleRetryPolicy`, `GoogleTransientErrorTranslator`, `GoogleSheetValueParser`
  — pure policy code; a `GoogleApiException` is constructed directly, no SDK call.
- **Integration** for the DI registration (real `ServiceCollection`).
- **Accepted gap — no automated tests** for the raw SDK clients `GoogleCredentialFactory`,
  `GoogleDriveClient`, `GoogleDriveFileClient`, `GoogleFileClientFactory`, `GoogleSheetsClient`,
  `GoogleSheetsDataSource`: they need live OAuth credentials and `Google.Apis` network calls, and
  are thin pass-throughs (`GoogleDriveFileClient.DownloadFileContent` is `try { _driveClient.DownloadFileContent(remotePath); } catch (GoogleApiException ex) { GoogleTransientErrorTranslator.ThrowIfTransient(ex); throw; }`).
  `coverlet.runsettings` already excludes `[Financial.Integrations.GoogleSheets]*` from the
  coverage gate for this reason. Google Drive/Sheets are external providers; everything above
  them is tested with the abstraction faked (`../references/external-providers.md`).
- No E2E: the smoke job runs with `Repository:Provider=LocalJson`.

## Setup pattern

```csharp
using System.Net;
using Financial.Integrations.GoogleCore;
using FluentAssertions;
using Google;

namespace Financial.GoogleIntegrations.Tests;

public class GoogleRetryPolicyTests
{
    private static GoogleApiException RateLimitedException() =>
        new("sheets", "Rate limited") { HttpStatusCode = HttpStatusCode.TooManyRequests };

    [Fact]
    public async Task ExecuteWithRetryAsync_ActionSucceedsImmediately_ReturnsResultWithoutRetrying()
    {
        var callCount = 0;

        var result = await GoogleRetryPolicy.ExecuteWithRetryAsync(() =>
        {
            callCount++;
            return Task.FromResult(42);
        });

        result.Should().Be(42);
        callCount.Should().Be(1);
    }
}
```

(Verbatim from `Tests/Financial.GoogleIntegrations.Tests/GoogleRetryPolicyTests.cs`.) When the
policy sleeps between retries, prefer a constructor/parameter that accepts a `TimeProvider` or a
zero delay over real waits.

## When to skip

- The SDK clients listed above — do not write a test that needs `credentials.json`.
- `DTO/SheetDTO.cs`, `DTO/SpreadSheetDTO.cs` — data holders.

## Examples from project

- `Tests/Financial.GoogleIntegrations.Tests/GoogleRetryPolicyTests.cs` — Unit.
- `Tests/Financial.GoogleIntegrations.Tests/GoogleTransientErrorTranslatorTests.cs` — Unit.
- `Tests/Financial.GoogleIntegrations.Tests/GoogleSheetValueParserTests.cs` — Unit.
- `Tests/Financial.GoogleIntegrations.Tests/GoogleDriveServiceCollectionExtensionsTests.cs` — Integration (DI).
- `Tests/Financial.Architecture.Tests/SharedInfrastructureIsolationRuleTests.cs` — Integration; pins that `Financial.Integrations.GoogleCore/Drive/Sheets` reference only `Financial.Shared.Abstractions`.
