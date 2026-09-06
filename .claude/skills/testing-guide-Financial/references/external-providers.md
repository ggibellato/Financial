> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# External Providers — what is faked, what stays real

Decided in Phase 3 (2026-09-06). "External" means outside this codebase's own deployable
boundary. Everything not listed as external stays real at Integration and E2E.

## True external providers (faked above Unit)

| Provider | Reached by | Strategy | Setup / teardown |
|---|---|---|---|
| Frankfurter exchange-rate API (`https://api.frankfurter.app/`) | `FrankfurterExchangeRateProvider` via typed `HttpClient` (`AddHttpClient<IExchangeRateProvider, FrankfurterExchangeRateProvider>`) | Fake at the transport: hand-written `FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage>)` behind a real `HttpClient` with `BaseAddress`. At host level: `ApiEndpointTests(new StubExchangeRateProvider(rate))` replaces `IExchangeRateProvider` through `ConfigureTestServices` + `RemoveAll<IExchangeRateProvider>()`. | No teardown; handler is per test. Failure modes: non-2xx, malformed JSON, missing currency key, thrown `HttpRequestException`. |
| Yahoo Finance quote API (`https://query1.finance.yahoo.com/`) | `YahooFinanceService` (typed `HttpClient`) | Same fake-handler pattern (`YahooFinanceServiceTests`). Above it, `IFinanceService` is stubbed with `StubFinanceService(AssetValueSnapshot?)`. | Per test. |
| Google Finance / StatusInvest / DadosMercado web pages | `Integrations/WebPageParser` (`GoogleFinance`, `StatusInvest`, `DadosMercadoDividend`) via HtmlAgilityPack | Never fetched in the default run. Parsing functions tested on strings/fragments; `AssetSnapshotSourceAdapter` / `DividendDataSourceAdapter` take a delegate so the lookup is faked. Live selector checks are `[Fact(Skip = "Manual verification test - requires internet connection")]`. | Run the skipped tests by hand after a selector change. |
| Google Drive API | `GoogleDriveFileClient` / `GoogleDriveClient` (`Google.Apis`) behind `IRemoteFileClient` / `IRemoteFileClientFactory` | Fake at the abstraction: `RemoteJsonStorage(download, upload, remotePath)` delegate constructor in `RemoteJsonStorageTests`; DI tests assert only registration. Raw client untested (accepted gap, `../artifacts/google-sdk-wrappers.md`). | Tests and CI run with `Repository:Provider=LocalJson`; no credentials file ever in tests. |
| Google Sheets API | `GoogleSheetsClient` / `GoogleSheetsDataSource` behind `IGoogleSheetsDataSource` | Stub `IGoogleSheetsDataSource` (`GoogleSheetsAssetReaderTests.StubDataSource` returns rows and records the requested range). `[Financial.Integrations.GoogleSheets]*` is excluded from the coverage gate. | Per test. |
| OTLP collector (Jaeger / Langfuse) | `Integrations/Observability` OpenTelemetry exporter | Never started. Disabled by default in tests (`Observability:Enabled=false` → no-op tracer). The enabled-but-unreachable path is tested by pointing at `http://localhost:4319` (`ObservabilityBackendUnreachableTests`). Service-level spans use `RecordingTelemetryTracer`. | Nothing to tear down; exporter retries in the background and is disposed with the host. |

Decision rule applied: none of these can run locally in Docker in under five seconds without
cost, credentials or flakiness, so all are faked (fundamentals' "External HTTP API (third-party)"
and "Third-party file storage" rows).

## Owned infrastructure (real at Integration and E2E — never in the mock column)

| Component | Why it is owned | How it stays real in tests |
|---|---|---|
| JSON data files (`data-investment.json`, `data-cashflow.json`) through `LocalJsonStorage`, `DebouncedJsonStorage`, `CashFlowLoader` / `InvestmentLoader` | The project's only persistence; deployed with the app (`docker-compose.yml` mounts `./data`) | `Guid`-named temp file under `Path.GetTempPath()`, copied from `TestDataPaths.DataJsonFile` or seeded from `ApiTestFactory.SeededBanksJson`; deleted in `finally` / `Dispose`. `RecordingJsonStorage` / `ControllableJsonStorage` are in-memory `IJsonStorage` implementations used only when the assertion is about *when* a write happens — still our own code, not a provider fake. |
| `CashFlowJsonRepository`, `InvestmentJsonRepository`, serializers, reference converters | Infrastructure we own | Real in every Infrastructure and Api test. `StubCashFlowRepository` / `StubInvestmentRepository` exist for the **Unit** layer only. |
| Application services of both contexts | Our code | Real inside the host and the WPF composition; stubbed (`Stub*Service` in `Financial.Presentation.Tests`) only in ViewModel **Unit** tests. |
| DI container (`Program.cs`, `App.xaml.cs`, `Add*` extensions) | Our composition | Real `ServiceCollection` / `WebApplicationFactory<Program>`. |
| The published `Financial.Api` process + built SPA | Our single deployable | Started for real by the CI `smoke` job (`../references/e2e-environment.md`). |
| Time (`TimeProvider`) | Not a provider — non-determinism | Controlled, not faked away: `FakeTimeProvider(DateTimeOffset)` (TestUtilities) or `Microsoft.Extensions.Time.Testing.FakeTimeProvider` wrapped by `ObservableFakeClock`. |

## The frontend's view of the backend

From `Financial.Web`'s tests, `Financial.Api` is a separate deployable reached over HTTP. Its
own Integration tests therefore fake the API at `financialApiClient` (module-level `vi.mock`)
or at `fetch` (client tests) — the sanctioned exception described in `mock-health-rules.md`.
This is not the backend being "external"; it is the single-process boundary of the SPA's test.
`Financial.App` (WPF) composes the backend in-process, so its Integration tests do **not** get
this exception — they wire the real services.

## Ambiguities resolved

- Google Drive as a *storage provider* — classified external (third-party file storage API),
  confirmed 2026-09-06. The `LocalJson` provider is the one exercised in every automated layer.
- `Tools/ImportGoogleSpreadSheets` — out of scope; not a provider question but recorded here
  because it is the only remaining code that talks to Google Sheets without tests.
