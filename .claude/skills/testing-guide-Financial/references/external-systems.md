> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# External System Test Strategies

This project touches more external systems than a typical CRUD app despite having no traditional database: two JSON-file storage backends (selectable per domain), an external currency-exchange HTTP API, Google Drive/Sheets APIs, two scraped web pages, and an Excel file format. Each has its own established strategy.

## Local JSON File Storage (both domains)

**Strategy: real, temp file copy.**

| Consideration | Decision |
|---|---|
| Complexity | Low — JSON read/write, no schema migration |
| Speed | Fast — file I/O < 10ms for test data size |
| Isolation | `Guid`-named temp file per test |
| Cleanup | `File.Delete(tempFile)` in `finally`, or the owning `IAsyncDisposable`'s `Dispose` |
| Why not a fake? | No mocking framework in this project; file I/O is simple enough that faking it adds no benefit |

Both Investment (`data.json`) and CashFlow (`data-cashflow.json`) follow this pattern independently — see `artifacts/infrastructure-persistence.md`.

## Google Drive (alternative storage backend)

**Strategy: real `GoogleDriveJsonStorage` class, fake read/write delegates.**

`GoogleDriveJsonStorage`'s constructor accepts `Func<string,string>`/`Action<string,string>` delegates instead of a concrete Google SDK client — tests inject in-memory delegates, proving the storage class's own path-threading and read/write contract without live credentials.

The layer beneath it — `GoogleDriveClient`, `GoogleFileClientFactory`, `GoogleService`, `GoogleCredentialFactory` — is an **accepted, untested gap** (see `artifacts/google-api-wrappers.md`). It wraps Google's own SDK and needs live OAuth credentials to exercise for real; not worth a testability seam for a single-user project per CLAUDE.md's "no over-engineering" guidance.

## Frankfurter Exchange Rate API (external HTTP)

**Strategy: real `HttpClient`, fake `HttpMessageHandler`.**

Never call the real API in tests (network flakiness, rate limits) and never mock the `IExchangeRateProvider` interface itself (that would hide the exact configuration/parsing bugs — wrong URL, wrong query params, wrong JSON path — this test exists to catch). See `artifacts/external-http-services.md` for the full 5-case matrix (success, non-2xx, malformed body, transport exception, missing field).

## Google Sheets (asset import, Investment domain)

**Strategy: parsing logic tested directly; API client untested (same accepted gap as Google Drive).**

`GoogleSheetValueParser` (pure string→value parsing) has direct unit tests. `GoogleSheetsClient`/`GoogleSheetsAssetReader` (actual Sheets API calls) fall under the same accepted-gap decision as the Drive wrappers.

## Web Scraping — Google Finance / StatusInvest

**Strategy: unit-test the pure parsing functions; verify CSS selectors manually.**

`GoogleFinanceParsing.ParsePriceValue`/`TryParseAsOf` and equivalents in `StatusInvest.cs` are unit tested against literal string inputs. Whether the CSS selectors still match the *live* page's current markup is verified manually per `Integrations/WebPageParser/HOW_TO_VERIFY_SELECTORS.md` — a fixture-based snapshot test would only prove the selector matches a frozen fixture, not that it still matches the live site, so it wouldn't catch the real risk (selector drift). See `artifacts/web-scraping-parsers.md`.

## Excel File (`Despesas.xlsx` import via ClosedXML)

**Strategy: real ClosedXML library, in-memory `XLWorkbook` built per test — no file I/O.**

Tests build a minimal worksheet programmatically (`new XLWorkbook()`, `AddWorksheet(...)`, `Cell(r,c).Value = ...`) rather than reading a static `.xlsx` fixture from disk. This exercises the real ClosedXML API surface without coupling tests to an evolving real-world file. See `artifacts/spreadsheet-import.md`.

## React → .NET API (HTTP, frontend side)

**Strategy: mock the API client factory at the module boundary.**

```typescript
vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getAssetDetails: getAssetDetailsMock,
  }),
}))
```

No Docker setup or test server for frontend component/hook tests — the mock replaces the HTTP call entirely. The one file allowed to mock `fetch` directly instead is `financialApiClient.ts` itself, since it *is* the boundary (see `artifacts/api-client.md`).

## API Endpoint E2E (backend HTTP pipeline)

**Strategy: real ASP.NET Core pipeline via `WebApplicationFactory<Program>`, real temp data files, DI-swap only true external boundaries.**

`ApiTestFactory` boots the actual app with real routing/middleware/serialization and temp copies of both domains' JSON files. Only true external-system boundaries or the one service under test get swapped via `RemoveAll<T>()` + `AddSingleton<T>()` — everything else runs for real. See `artifacts/api-endpoints-e2e.md`.

---

**Decision rule for anything new:** if it can run locally in-process (real library, in-memory data, fake `HttpMessageHandler`) in under a few seconds with no external cost or flakiness risk → make it real or fake-at-the-boundary. If it requires live third-party credentials with no in-process fake practical → document it as an accepted gap like the Google SDK wrappers, rather than skipping the decision silently.
