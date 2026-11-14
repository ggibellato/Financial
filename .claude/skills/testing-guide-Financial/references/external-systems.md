> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# External Systems — what is real, what is faked

Confirmed 2026-11-14 (first decided 2026-09-06). Decision rule from the fundamentals: if it runs
locally in under five seconds with no cost, credentials or flakiness, use it for real; otherwise
fake it behind the narrowest seam we own.

## Owned infrastructure — always real above Unit

| System | Why real | How it stays real in tests | Setup / teardown |
|---|---|---|---|
| JSON data files (`data-investment.json`, `data-cashflow.json`, `data-fx-rates.json`) through `LocalJsonStorage`, loaders, serializers | The project's only persistence; costs nothing to run | `Guid`-named temp file under `Path.GetTempPath()`, copied from `TestDataPaths.DataJsonFile` or written from `ApiTestFactory.SeededCashFlowJson` | Delete in `finally` / `Dispose`; `ApiTestFactory.Dispose` deletes its own files. Never `data/*.json` |
| Repositories, services, middleware, serializers of both contexts | Our code | Real inside the host or over a temp file. `StubCashFlowRepository` / `StubInvestmentRepository` are for **Unit** only | — |
| DI container (`Program.cs`, `Add*` extensions) | Our composition | Real `ServiceCollection` / `WebApplicationFactory<Program>` | Disposed with the provider/factory |
| The published API + built SPA | Our single deployable | Started for real by the CI `web-e2e` job (`e2e-environment.md`) | Temp data copies; runner teardown |
| Time (`TimeProvider`) | Non-determinism, not a system | Controlled: `TestClock.At(...)`, `FakeTimeProvider`, `ObservableFakeClock` | None |

`ControllableJsonStorage` / `RecordingJsonStorage` (in-memory `IJsonStorage`) are allowed only when
the assertion is about *when* a write happens or whether it happened — they are our own code, not a
provider fake.

## Third-party systems — always faked in the default run

| System | Reached by | Fake | Failure modes to cover |
|---|---|---|---|
| Frankfurter FX API (`https://api.frankfurter.dev/v2/`) | `FrankfurterExchangeRateProvider` (`Integrations/Frankfurter`), typed `HttpClient` | Transport: `FakeHttpMessageHandler` (TestUtilities) behind a real `HttpClient` with `BaseAddress`. Host: `ApiTestFactory` registers `StubExchangeRateProvider` by default; pass your own or `useRealExchangeRates: true` to exercise the real FX composition | Non-2xx, malformed JSON, missing currency, thrown `HttpRequestException`, a handler that never answers (the per-call budget is a constructor argument) |
| Yahoo Finance quotes (`https://query1.finance.yahoo.com/`) | `YahooFinanceService` | Same fake-handler pattern; above it `StubFinanceService` / `FakeFinanceService` | Non-2xx, malformed body, unsupported exchange (no HTTP call) |
| Google Finance, StatusInvest, Redentia, Tesouro Transparente, DadosMercado pages | `Integrations/WebPageParser` (HtmlAgilityPack) | Parsing tested on literal fragments/CSV; adapters take a delegate | Changed page structure → documented failure, never a silent `0m`. Live selectors: `Category=Live`, nightly |
| Google Drive (remote storage) | `GoogleDriveClient` / `GoogleDriveFileClient` behind `IRemoteFileClient(Factory)` | Above: `RemoteJsonStorage(download, upload, remotePath)` delegates, `StubRemoteFileClient`. Contract: real `DriveService` over `FakeDriveHandler` | 404/403 untouched, 429/5xx → `TransientStorageException`, failed media download throws |
| Google Sheets | `GoogleSheetsClient` / `GoogleSheetsDataSource` behind `IGoogleSheetsDataSource` | Stub data source (`GoogleSheetsAssetReaderTests`) | Malformed row, empty range. Raw client: accepted gap |
| Google Calendar + OAuth | `GoogleCalendarOAuthClient` behind CashFlow's `ICalendarProvider` | Transport: fake handler. Host: `FakeCalendarProvider`, `FakeCalendarConnectionStore`, `FakeCalendarIntegrationService` (TestUtilities) | Revoked token, unknown calendar, non-2xx, missing email |
| OTLP collector (Jaeger / Langfuse) | `Integrations/Observability` exporter | Never started. Disabled by default; the enabled-but-unreachable path points at `http://localhost:4319` | Endpoints still 200, spans never throw |

None of these can run locally without credentials, cost or flakiness, so all are faked. Tests and CI
run with `Repository:Provider=LocalJson` for every context and never read a credentials file.

## The SPA's view of the backend

From `Financial.Web`'s Vitest tests the API is a separate deployable reached over HTTP, so it is
faked at `financialApiClient` (module-level `vi.mock`) or at `fetch` (client tests). That is the
single-process boundary of the SPA's tests, not the backend being "external" — see
`mock-health-rules.md`. Only the Playwright suite runs both for real.
