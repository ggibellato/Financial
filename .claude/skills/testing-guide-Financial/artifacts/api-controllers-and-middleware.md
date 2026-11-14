> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Controllers, Middleware and API Helpers (`Financial.Api/Controllers/*.cs`, `Financial.Api/Middleware/*.cs`, `Financial.Api/Helpers/*.cs`)

## What to test

Through the real host (`ApiEndpointTests`), per endpoint:
- **Status codes**: 200 with the DTO body on success; 400 for a malformed body / bad enum
  string / `[ApiController]` model validation; 404 when the id is unknown; 409 for
  `OverdraftConfirmationRequiredException`, `DuplicateNameException`, `EntityInUseException`,
  `ReserveMovementLinkedToIncomeException`, `InvestmentRuleViolationException`; 422 for
  `UnsupportedAssetClassException` — the map in
  `Financial.Api/Middleware/DomainExceptionMappingMiddleware.cs`.
- **Problem details body** carries the domain message (`RejectedWithdrawal_StillWritesTheFullMessageToTheCaller`)
  while the **log entry** carries only the exception type, method, path and status — never the
  bucket name or amount (`DomainExceptionLoggingTests`).
- **Wire property names the SPA depends on** for at least one endpoint per DTO family (the
  OpenAPI snapshot pins the rest — `api-contract-snapshot.md`).
- **Query-string vs body** per `docs/rules/implementation.md`: GETs take query parameters; a
  test that a GET with a body is not required.
- **Persistence through the host**: after a POST, a GET on the same host returns the item, and
  the temp JSON file contains it — the repository and storage are real.
- **Health / sync-status / diagnostics** endpoints with observability disabled.
- **An empty or `null` JSON body** on a `[FromBody]` endpoint → 400, not a 500
  (`EmptyBodyEndpointsTests`).
- Constructor null guards are not tested per controller: `Tests/Financial.Api.Tests/ConstructorGuardTests.cs`
  sweeps the assembly (`docs/rules/implementation.md` §Tests #7).

## Layer assignment

- **Integration** (default, `[Trait("Category", "Integration")]`): `WebApplicationFactory<Program>`
  on temp copies of `TestData/data.test.json`, the seeded CashFlow JSON, and temp paths for the FX-rate
  store and the calendar credentials; real DI, real middleware, real `System.Text.Json`. The base
  constructor is
  `ApiEndpointTests(IExchangeRateProvider? exchangeRateProvider = null, TimeProvider? timeProvider = null, ICalendarProvider? calendarProvider = null, bool useRealExchangeRates = false)`.
  FX is a `StubExchangeRateProvider` and time is `TestClock.At()` unless you pass your own — never
  the network, never the wall clock. This is also where every backend feature's AC-tracing tests go
  (`../references/feature-traceability.md`).
- **Unit** (`Category=Unit`) only for host-less `Api.Tests` classes: `ConstructorGuardTests` and
  `SyncStatusResolver` (`Financial.Api/Helpers`) if it grows branches.
- **E2E**: an endpoint is E2E-covered only when the smoke journey drives it from the SPA
  (`../references/e2e-environment.md`); do not label host tests E2E.
- `DomainExceptionMappingMiddleware` is additionally tested in isolation with a
  `RecordingLogger<DomainExceptionMappingMiddleware>` and a `DefaultHttpContext`
  (`DomainExceptionLoggingTests.InvokeWithAsync`) because the redaction rule is easier to assert
  without the host's Serilog pipeline — still Integration in shape (real `IProblemDetailsService`).

## Setup pattern

```csharp
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class ExpenseEndpointsTests : ApiEndpointTests
{
    private static readonly Guid BarclaysId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001");
    private static readonly Guid MercadoId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000008");

    [Fact]
    public async Task AddExpense_ValidRequest_ReturnsOk()
    {
        var request = new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "Weekly groceries",
            Value = 54.32m,
            CategoryId = MercadoId,
            PaymentSourceBankId = BarclaysId,
            CreditCardId = null
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

(Verbatim from `Tests/Financial.Api.Tests/ExpenseEndpointsTests.cs`.) The seeded ids
(`8f3b1c1a-…`) come from `ApiTestFactory.SeededCashFlowJson`; reuse them rather than inventing
entities per test. A class whose tests all need the same FX rate, "today" or calendar fake passes
it through the base constructor (`: base(timeProvider: TestClock.At(...))`). A single test that
needs something different may build its own factory inside the method —
`await using var factory = new ApiTestFactory(new StubExchangeRateProvider(0.146m)); using var client = factory.CreateClient();`
— never as a class-level field. To start the host on different CashFlow data, call
`ReplaceCashFlowDataBeforeStart(json)` before the first `Client` use; `SavedCashFlowData()` reads
what the host wrote. The data file loads once at startup, so editing it afterwards proves nothing.

## When to skip

- Re-testing a service branch already covered at Unit (`application-services.md`) through
  every endpoint — one negative case per endpoint proving the middleware mapping is enough,
  unless it is an AC.
- `ApiControllerBase`, `[ProducesResponseType]` attributes, routing — framework.
- `SwaggerEndpointsTests` beyond "the document and UI are served in Development and Production".

## Examples from project

- `Tests/Financial.Api.Tests/ExpenseEndpointsTests.cs`, `ReserveEndpointsTests.cs`, `BalanceAdjustmentsEndpointsTests.cs` — Integration; 200/400/404/409 through the host.
- `Tests/Financial.Api.Tests/EmptyBodyEndpointsTests.cs` — Integration; empty body → 400 on every `[FromBody]` endpoint.
- `Tests/Financial.Api.Tests/Acceptance/SettlementScreenAcceptanceTests.cs` — Integration with `StubExchangeRateProvider` (external) for the FX branches, including `rate == null`.
- `Tests/Financial.Api.Tests/PaymentsDueEndpointsTests.cs` — Integration with a `FakeTimeProvider` passed to the base.
- `Tests/Financial.Api.Tests/DomainExceptionLoggingTests.cs` — Integration; log redaction.
- `Tests/Financial.Api.Tests/ConstructorGuardTests.cs` — Unit; assembly-wide constructor null guards.
- `Tests/Financial.Api.Tests/SyncStatusEndpointsTests.cs`, `DiagnosticsEndpointsTests.cs` — Integration; helper + hosted-service state surfaced over HTTP.
