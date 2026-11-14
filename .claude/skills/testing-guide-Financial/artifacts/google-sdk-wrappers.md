> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Google SDK Wrappers (`Integrations/GoogleCore/*.cs`, `Integrations/GoogleDrive/*.cs`, `Integrations/GoogleSheets/*.cs`, `Integrations/GoogleCalendar/*.cs`)

## What to test

- **Calendar OAuth client** (`GoogleCalendarOAuthClient`): token exchange/refresh, revoke, account
  email, and the calendar/event calls, over a fake transport — the request it sends (endpoint, form
  fields, bearer header) and each failure: non-2xx → throws, missing email →
  `InvalidOperationException`, a rejected code or refresh token → `GoogleTokenRevokedException`, an
  unknown calendar → `GoogleCalendarNotFoundException`. Above it, CashFlow Infrastructure's
  `GoogleCalendarProviderAdapter` is tested against the client interface, and the API host fakes
  `ICalendarProvider` with `FakeCalendarProvider`.
- **Drive client contract** (`GoogleDriveClient`): a real `DriveService` whose `HttpClientFactory`
  hands the SDK `FakeDriveHandler`, so the SDK builds and parses for real and only the network is
  fake (details in `external-http-services.md`, "Google SDK clients over a fake handler").
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
  `IRemoteFileClientFactory`; the GoogleCalendar extension registers the typed OAuth client.
- **Consumers of the abstractions**, tested with a stub `IGoogleSheetsDataSource` /
  `IGoogleDriveFileSource` / `IRemoteFileClient` — see `console-tools.md` and
  `json-persistence-and-repositories.md` (`RemoteJsonStorage`).

## Layer assignment

- **Unit** (`Financial.GoogleIntegrations.Tests` is all `Category=Unit`) for `GoogleRetryPolicy`,
  `GoogleTransientErrorTranslator`, `GoogleSheetValueParser` — pure policy code; a
  `GoogleApiException` is constructed directly — and for the fake-transport contract tests of
  `GoogleCalendarOAuthClient` and `GoogleDriveClient`, plus the DI registration tests (an in-memory
  `ServiceCollection`, no I/O).
- **Accepted gap — no automated tests** for `GoogleCredentialFactory`, `GoogleFileClientFactory`,
  `GoogleSheetsClient` and `GoogleSheetsDataSource`: they need live OAuth credentials or a credentials
  file, and are thin pass-throughs. `coverlet.runsettings` excludes
  `[Financial.Integrations.GoogleSheets]*` from the coverage gate for this reason. Google Drive/Sheets are external providers; everything above
  them is tested with the abstraction faked (`../references/external-systems.md`).
- No E2E: the web-e2e job runs with `Repository:Provider=LocalJson`.

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
- `Tests/Financial.GoogleIntegrations.Tests/GoogleDriveServiceCollectionExtensionsTests.cs`, `GoogleCalendarServiceCollectionExtensionsTests.cs` — DI registration.
- `Tests/Financial.GoogleIntegrations.Tests/GoogleCalendarOAuthClientTests.cs` — fake transport; request shape and every failure.
- `Tests/Financial.GoogleIntegrations.Tests/GoogleDriveClientContractTests.cs` + `FakeDriveHandler.cs` — real SDK over a fake handler; retry delays recorded, not waited.
- `Tests/Financial.CashFlow.Infrastructure.Tests/Services/GoogleCalendarProviderAdapterTests.cs` — Unit; adapter over the OAuth client interface.
- `Tests/Financial.Architecture.Tests/SharedInfrastructureIsolationRuleTests.cs` — Integration; pins that `Financial.Integrations.GoogleCore/Drive/Sheets` reference only `Financial.Shared.Abstractions`.
