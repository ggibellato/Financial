> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# External HTTP Services and Price Fetchers (`Integrations/Frankfurter/*.cs`, `Financial.Investment.Infrastructure/Services/*.cs`, `Financial.Shared.Abstractions/Currencies/**`)

Three sub-types:

1. **HTTP-backed providers** that call a genuine external API through an injected `HttpClient`:
   `FrankfurterExchangeRateProvider` (`Integrations/Frankfurter`, `https://api.frankfurter.dev/v2/`)
   and `YahooFinanceService` (`https://query1.finance.yahoo.com/`).
2. **FX composition** in `Financial.Shared.Abstractions/Currencies`: `UsdBasedExchangeRateProvider`
   (cross rates through USD, backed by `IFxRateStore` + `IUsdRateFetcher`) and
   `InMemoryCachedExchangeRateProvider` — pure logic over fakes, Unit.
3. **Fetchers, adapters and fallbacks** that orchestrate providers behind `IFinanceService` /
   `IAssetPriceFetcher`: `AssetPriceService`, `StandardAssetPriceFetcher`,
   `BondAssetPriceFetcher`, `CryptocurrencyAssetPriceFetcher`, `FallbackFinanceService`,
   `CachingFinanceService`, `GoogleFinanceService`, `StatusInvestFinanceService`,
   `TesouroTransparenteFinanceService`, `RedentiaFinanceService`, `AssetSnapshotSourceAdapter`,
   `DividendDataSourceAdapter`, `AssetPriceLookupService`, `AssetPriceHistoryService`,
   `WebPageParserMappers`.

## What to test

**Providers (the HTTP contract):**
- Success: the JSON body parses to the expected rate/snapshot.
- Non-2xx status → the documented outcome (`FrankfurterExchangeRateProvider` returns `null`;
  `YahooFinanceService` throws `InvalidOperationException`).
- Malformed body, body missing the requested key → `null` / exception.
- A handler that never answers → the provider's per-call budget (10 s in production, a constructor argument in tests) cancels it and the documented empty result comes back; a 5xx or thrown transport error makes exactly one request.
- Transport failure (`HttpRequestException` thrown by the handler) → swallowed **and logged by
  exception type** (`RecordingLogger<T>.Entries` contains `nameof(HttpRequestException)` at
  `Warning`), or propagated — whichever the class documents.
- Guard clauses before any HTTP call: blank ticker, missing exchange, unsupported exchange
  (`YahooFinanceServiceTests.GetAssetValue_UnsupportedExchange_ThrowsInvalidOperationException_WithoutCallingHttp`).
- The request itself when the URL/query has branching: capture `HttpRequestMessage` inside the
  responder delegate and assert path/query.

**Fetchers / adapters:**
- Dispatch by asset class; `UnsupportedAssetClassException` for the unpriceable classes (known
  noise, deliberately handled — see project memory `project_unsupported_asset_class_noise`).
- `FallbackFinanceService`: primary succeeds → fallback never called; primary throws → fallback
  used and the primary's exception **type** logged; both fail → exception surfaces.
- `AssetSnapshotSourceAdapter`: parameterless constructor must not perform a network call.

## Layer assignment

- **Provider contract tests, faked at the transport** — real `HttpClient` +
  `FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage>)` from
  `Tests/Financial.TestUtilities` + `BaseAddress`. The provider is external
  (`../references/external-systems.md`); everything on our side (URL building, JSON parsing,
  logging) is real. They run in process with no I/O, so the class is tagged `Category=Unit`
  (`Financial.Frankfurter.Tests`); `YahooFinanceServiceTests` sits in the Investment Infrastructure
  project and carries that project's tag. Either way this is the system-boundary contract test the
  fundamentals require for a service calling an external HTTP API — it is not optional because the
  trait says Unit.
- **Unit** for fetchers/adapters/fallbacks and FX composition: inject
  `StubFinanceService(AssetValueSnapshot?)`, a `FakeFinanceService(Func<...>)` delegate,
  `StubExchangeRateProvider` / `FakeExchangeRateProvider`; nothing touches the network.
- No E2E: the smoke run does not reach Yahoo/Frankfurter (the seeded data avoids live prices).
  Live-selector checks are manual (`web-scraping-parsers.md`).

## Setup pattern

From `Tests/Financial.Frankfurter.Tests/FrankfurterExchangeRateProviderTests.cs`:

```csharp
private static FrankfurterExchangeRateProvider CreateProvider(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
    new(CreateClient(new FakeHttpMessageHandler(respond)), NullLogger<FrankfurterExchangeRateProvider>.Instance);

private static HttpClient CreateClient(HttpMessageHandler handler) =>
    new(handler) { BaseAddress = new Uri("https://api.frankfurter.dev/v2/") };

[Fact]
public async Task GetHistoricalRateAsync_WhenHttpRequestThrows_LogsTheExceptionType()
{
    var logger = new RecordingLogger<FrankfurterExchangeRateProvider>();
    var provider = new FrankfurterExchangeRateProvider(
        CreateClient(new FakeHttpMessageHandler(_ => throw new HttpRequestException("network down"))), logger);

    await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 1), Currency.BRL, Currency.GBP);

    logger.Entries.Should().ContainSingle(e =>
        e.Level == LogLevel.Warning && e.Message.Contains(nameof(HttpRequestException)));
}
```

`FakeHttpMessageHandler` lives in `Tests/Financial.TestUtilities` and answers both `Send` and
`SendAsync`. Use it; do not add a local copy (`GoogleCalendarOAuthClientTests` still has a private
async-responder variant — move it to TestUtilities rather than copy it again). It ignores the URL,
so when the URL or query has branching, capture the `HttpRequestMessage` in the responder and assert
on it.

## Google SDK clients over a fake handler

`GoogleDriveClient` (`Integrations/GoogleDrive`) is tested through a real `DriveService` whose
`HttpClientFactory` hands the SDK a fake `HttpMessageHandler`, so the SDK's own request building
and response parsing run and only the network is faked. The client's `internal` constructor takes
the service factory and the retry delay; never fake `DriveService` members.

- `Tests/Financial.GoogleIntegrations.Tests/FakeDriveHandler.cs` records each request (method,
  URI, body) and answers from a `Func<RecordedRequest, HttpResponseMessage>`; `Error(status)`
  builds the JSON error body the SDK needs to raise a `GoogleApiException` with that status.
- Pass a recording delay function so the 2 s / 4 s backoff is asserted, never waited.
- A failed media download does not throw from the SDK; it is reported on the download's progress
  result. `GoogleDriveClient.DownloadFileContent` checks it and throws, so a 404 or 503 never
  reaches storage as empty content. A new SDK call that streams content needs the same check.
- Resumable uploads are two requests: the first answers with a `Location` header, the second
  (a `PUT` to that location) carries the content and decides the result.
- `GoogleDriveFileClient` has an `internal` constructor over a `GoogleDriveClient` so the
  429/5xx to `TransientStorageException` translation is tested through the real wrapper.

## When to skip

- `WebPageParserMappers` beyond its own branching — it is a DTO mapper; one test per branch.
- Retrying the same JSON fixture with cosmetic variations (variant repetition).
- Anything requiring a live network call — those belong in the `Category=Live` verification
  tests, never in the default run.

## Examples from project

- `Tests/Financial.Frankfurter.Tests/FrankfurterExchangeRateProviderTests.cs` — transport faked; all five failure modes covered.
- `Tests/Financial.Investment.Infrastructure.Tests/Services/YahooFinanceServiceTests.cs` — transport faked + guard clauses that must not call HTTP.
- `Tests/Financial.Shared.Abstractions.Tests/Currencies/FxRates/UsdBasedExchangeRateProviderTests.cs`, `Currencies/InMemoryCachedExchangeRateProviderTests.cs` — Unit; FX composition and caching.
- `Tests/Financial.Investment.Infrastructure.Tests/Services/FallbackFinanceServiceTests.cs` — Unit; `RecordingLogger<FallbackFinanceService>` proves the swallowed exception type is logged.
- `Tests/Financial.Investment.Infrastructure.Tests/Services/AssetPriceServiceTests.cs` — Unit; dispatch and guard clauses over `IAssetPriceFetcher` list.
- `Tests/Financial.Investment.Infrastructure.Tests/Services/AssetSnapshotSourceAdapterTests.cs` — Unit; no network on construction.
- `Tests/Financial.Investment.Infrastructure.Tests/TestDoubles/StubFinanceService.cs` — the local stub these tests share.
