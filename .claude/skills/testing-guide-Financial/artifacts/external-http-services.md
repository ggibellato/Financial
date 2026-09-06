> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# External HTTP Services and Price Fetchers (`Financial.*.Infrastructure/Services/*.cs`)

Two sub-types live in these folders:

1. **HTTP-backed providers** that call a genuine external API through an injected `HttpClient`:
   `FrankfurterExchangeRateProvider` (`https://api.frankfurter.app/`) and `YahooFinanceService`
   (`https://query1.finance.yahoo.com/`).
2. **Fetchers, adapters and fallbacks** that orchestrate providers behind `IFinanceService` /
   `IAssetPriceFetcher`: `AssetPriceService`, `StandardAssetPriceFetcher`,
   `BondAssetPriceFetcher`, `CryptocurrencyAssetPriceFetcher`, `FallbackFinanceService`,
   `GoogleFinanceService`, `StatusInvestFinanceService`, `AssetSnapshotSourceAdapter`,
   `DividendDataSourceAdapter`, `AssetPriceLookupService`, `AssetPriceHistoryService`,
   `WebPageParserMappers`.

## What to test

**Providers (the HTTP contract):**
- Success: the JSON body parses to the expected rate/snapshot.
- Non-2xx status → the documented outcome (`FrankfurterExchangeRateProvider` returns `null`;
  `YahooFinanceService` throws `InvalidOperationException`).
- Malformed body, body missing the requested key → `null` / exception.
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

- **Integration (provider faked at the transport layer)** for providers: real `HttpClient` +
  hand-written `FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage>)` +
  `BaseAddress`. The provider is external (`../references/external-providers.md`); everything
  on our side (URL building, JSON parsing, logging) is real. This is the fundamentals' "service
  calling a genuine external HTTP API" row.
- **Unit** for fetchers/adapters/fallbacks: inject `StubFinanceService(AssetValueSnapshot?)` or a
  `FakeFinanceService(Func<...>)` delegate; nothing touches the network.
- No E2E: the smoke run does not reach Yahoo/Frankfurter (the seeded data avoids live prices).
  Live-selector checks are manual (`web-scraping-parsers.md`).

## Setup pattern

From `Tests/Financial.CashFlow.Infrastructure.Tests/Services/FrankfurterExchangeRateProviderTests.cs`:

```csharp
private static FrankfurterExchangeRateProvider CreateProvider(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
    new(CreateClient(new FakeHttpMessageHandler(respond)), NullLogger<FrankfurterExchangeRateProvider>.Instance);

private static HttpClient CreateClient(HttpMessageHandler handler) =>
    new(handler) { BaseAddress = new Uri("https://api.frankfurter.app/") };

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

`FakeHttpMessageHandler` is currently a `private sealed class` duplicated in that file (line 91)
and in `YahooFinanceServiceTests.cs` (line 135). `docs/rules/implementation.md` §Tests puts
shared doubles in `Tests/Financial.TestUtilities`; the next HTTP-backed provider should move it
there rather than add a third copy.

## When to skip

- `WebPageParserMappers` beyond its own branching — it is a DTO mapper; one test per branch.
- Retrying the same JSON fixture with cosmetic variations (variant repetition).
- Anything requiring a live network call — those belong in the `Skip = "Manual"` verification
  tests, never in the default run.

## Examples from project

- `Tests/Financial.CashFlow.Infrastructure.Tests/Services/FrankfurterExchangeRateProviderTests.cs` — Integration (faked transport); all five failure modes covered.
- `Tests/Financial.Investment.Infrastructure.Tests/Services/YahooFinanceServiceTests.cs` — Integration (faked transport) + Unit guard clauses that must not call HTTP.
- `Tests/Financial.Investment.Infrastructure.Tests/Services/FallbackFinanceServiceTests.cs` — Unit; `RecordingLogger<FallbackFinanceService>` proves the swallowed exception type is logged.
- `Tests/Financial.Investment.Infrastructure.Tests/Services/AssetPriceServiceTests.cs` — Unit; dispatch and guard clauses over `IAssetPriceFetcher` list.
- `Tests/Financial.Investment.Infrastructure.Tests/Services/AssetSnapshotSourceAdapterTests.cs` — Unit; no network on construction.
- `Tests/Financial.Investment.Infrastructure.Tests/TestDoubles/StubFinanceService.cs` — the local stub these tests share.
