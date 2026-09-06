> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Mock Health Rules

## The boundary, in this project's terms

Above Unit, the only things that may be faked are the six external providers in
`external-providers.md` (Frankfurter, Yahoo Finance, scraped pages, Google Drive, Google
Sheets, the OTLP collector) plus the non-deterministic clock. Everything else —
`CashFlowJsonRepository`, `LocalJsonStorage`, `CashFlowSerializerAdapter`, every `*Service`,
`DomainExceptionMappingMiddleware`, the DI container — is real.

No mocking framework exists in the solution (no Moq / NSubstitute / FakeItEasy in any csproj,
no MSW in `package.json`). Every double is a hand-written class implementing the real
interface, which is deliberate: a hand-written stub cannot be "set up" to return something
the interface never promised, and a `NotImplementedException` on an unexpected member is a
louder failure than a silent default.

## The one thing each layer fakes above Unit

| Layer / place | What is faked | Mechanism |
|---|---|---|
| Infrastructure Integration (HTTP providers) | The provider's transport | `private sealed class FakeHttpMessageHandler : HttpMessageHandler` overriding `SendAsync` with a `Func<HttpRequestMessage, HttpResponseMessage>` (`FrankfurterExchangeRateProviderTests.cs:91`, `YahooFinanceServiceTests.cs:135`) |
| API host Integration | `IExchangeRateProvider`, `TimeProvider` | `ApiTestFactory.ConfigureWebHost` → `builder.ConfigureTestServices(services => { services.RemoveAll<IExchangeRateProvider>(); services.AddSingleton(_exchangeRateProviderOverride); })` — the only two overrides it accepts |
| Shared.Infrastructure Integration (remote storage) | `IRemoteFileClient` (Google Drive) | `RemoteJsonStorage`'s delegate constructor `(Func<string,string> download, Action<string,string> upload, string remotePath)` |
| Spreadsheet import | `IGoogleSheetsDataSource` | Local `StubDataSource` in `GoogleSheetsAssetReaderTests` |
| Observability | The collector | Not started; `Observability:Enabled=false`, or an unreachable port for the negative path |
| WPF Integration (AC-tracing) | `IDialogService` / `confirm` delegate | `StubDialogService` — WPF shell mechanics, not the feature; services stay real |

## What Unit tests fake vs what Integration/E2E leave real

- **Unit (Application)**: `StubCashFlowRepository` / `StubInvestmentRepository` (in-memory,
  `Tests/Financial.TestUtilities`), `RecordingTelemetryTracer`, `RecordingLogger<T>` or
  `NullLogger<T>`, `FakeTimeProvider`, a stub `IExchangeRateProvider`. Domain entities stay real
  (sociable Unit tests).
- **Unit (WPF ViewModels)**: `Stub*Service` classes in `Tests/Financial.Presentation.Tests/ViewModels/**/TestStubs.cs`,
  `StubDialogService`, `Func<string,bool> confirm`, `Func<Task> refresh`.
- **Unit (Infrastructure fetchers)**: `StubFinanceService` / `FakeFinanceService` delegates.
- **Unit (React hooks/components)**: `vi.mock` of `financialApiClient` or of the hook module;
  `vi.fn()` callbacks; `vi.useFakeTimers`.
- **Integration**: none of the above except the provider fakes in the table.
- **E2E (smoke)**: nothing faked at all; real published API on seeded JSON, real browser.

## The sanctioned exception — the SPA faking its own backend

`Financial.Web` and `Financial.Api` live in one repo and one Docker image, but at test time
the SPA is a single process and the API is a separate deployable reached only over HTTP. So
`vi.mock('../../api/financialApiClient', () => ({ apiClient: { getBanks: getBanksMock, … } as Partial<FinancialApiClient> }))`
in a page test is **not** a "mock something owned" violation — it is the same
single-process / no-cross-service-deployment boundary the layer table uses. The backend has its
own Integration tests against real storage (`Tests/Financial.Api.Tests`), and the smoke job
proves the two agree at runtime. Rules that follow from this:

- Mock at the module boundary (`financialApiClient`) or at `fetch` (client tests), never at a
  deeper owned module (a hook mocking another hook's internals, a page mocking `useAsyncResource`).
  A component test may mock the hook module it consumes to drive its state matrix — the page
  test then covers the real hook.
- The exception applies only to the SPA. `Financial.App` composes the backend in-process, so a
  WPF Integration test wires real services from the real container.

## The "too many fakes" signal

- An Integration test that needs a stub repository, a stub service and a fake storage is a
  Unit test wearing the wrong label — move it to the Unit suite, or remove the stubs and use
  the host / a temp file.
- A Unit test that needs more than one `Stub*Service` plus the repository is testing a
  ViewModel or service that does too much (`docs/rules/implementation.md` SRL) — split the
  unit before adding the fourth stub.
- A new `Stub*` class appearing in a test project instead of `Tests/Financial.TestUtilities`
  (backend) or the shared `TestStubs.cs` (WPF) is a rule violation
  (`docs/rules/implementation.md` §Tests item 1) unless it is genuinely single-file.

## Recording doubles are for proving absence too

`RecordingLogger<T>.Entries` and `RecordingTelemetryTracer.Spans` exist "to prove what *is not*
logged as much as what is": every negative-path test at Unit asserts that the entry contains
the exception **type name** and does **not** contain the financial value or entity name from
the message (`DomainExceptionLoggingTests.RejectedWithdrawal_LogsTheExceptionType_WithoutTheFinancialValuesInItsMessage`).
Keep recorders as instance fields — a `static readonly` tracer accumulates spans across tests.
