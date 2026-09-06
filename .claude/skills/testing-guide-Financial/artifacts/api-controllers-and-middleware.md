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
- **Guard clauses** that HTTP cannot reach (constructor nulls, non-nullable `[FromBody]` null):
  `ControllerGuardClauseTests` calls the controller directly — the only Unit tests in this area.

## Layer assignment

- **Integration** (default): `WebApplicationFactory<Program>` on temp copies of
  `TestData/data.test.json` and the seeded CashFlow JSON, real DI, real middleware, real
  `System.Text.Json`. Fakes allowed: `IExchangeRateProvider` (external) and `TimeProvider`
  (`ApiEndpointTests(IExchangeRateProvider? exchangeRateProvider = null, TimeProvider? timeProvider = null)`).
  This is also where every backend feature's AC-tracing tests go
  (`../references/feature-traceability.md`).
- **Unit** only for `ControllerGuardClauseTests` and for `SyncStatusResolver`
  (`Financial.Api/Helpers`) if it grows branches.
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
(`8f3b1c1a-…`) come from `ApiTestFactory.SeededBanksJson`; reuse them rather than inventing
entities per test. A test that needs a different FX rate or "today" builds its own factory:
`await using var factory = new ApiTestFactory(new StubExchangeRateProvider(0.146m)); using var client = factory.CreateClient();`
— inside a test method is fine; never as a class-level field.

## When to skip

- Re-testing a service branch already covered at Unit (`application-services.md`) through
  every endpoint — one negative case per endpoint proving the middleware mapping is enough,
  unless it is an AC.
- `ApiControllerBase`, `[ProducesResponseType]` attributes, routing — framework.
- `SwaggerEndpointsTests` beyond "the document is served in Development".

## Examples from project

- `Tests/Financial.Api.Tests/ExpenseEndpointsTests.cs`, `ReserveEndpointsTests.cs`, `TransfersEndpointsTests.cs` — Integration; 200/400/404/409 through the host.
- `Tests/Financial.Api.Tests/ControleMaeEndpointsTests.cs` — Integration with `StubExchangeRateProvider` (external) for the FX branches, including `rate == null`.
- `Tests/Financial.Api.Tests/PaymentsDueEndpointsTests.cs` — Integration with a `FakeTimeProvider` passed to the base.
- `Tests/Financial.Api.Tests/DomainExceptionLoggingTests.cs` — Integration; log redaction.
- `Tests/Financial.Api.Tests/Controllers/ControllerGuardClauseTests.cs` — Unit; unreachable guards.
- `Tests/Financial.Api.Tests/SyncStatusEndpointsTests.cs`, `DiagnosticsEndpointsTests.cs` — Integration; helper + hosted-service state surfaced over HTTP.
