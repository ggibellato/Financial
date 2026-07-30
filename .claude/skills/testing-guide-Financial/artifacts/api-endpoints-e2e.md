> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# API Endpoints — E2E (`*EndpointsTests.cs` via `ApiTestFactory`)

Examples: `DividendEndpointsTests`, `AssetPriceEndpointsTests`, `DiagnosticsEndpointsTests`, `ControleMaeEndpointsTests`.

## What to test

- HTTP status codes for success and every documented error path (400 for bad input, 404 for not-found, etc.)
- Response body shape and values, deserialized into the real DTO type
- **camelCase JSON property names the frontend depends on** — a rename on the C# side without updating the test should fail immediately (see `GetDividendSummary_JsonContainsAverageDividendLastFiveYears`)
- Error-to-HTTP mapping: when a service throws, does the endpoint return the right status with a friendly `detail` message?
- Input validation wiring: one test per endpoint proving invalid input (blank/whitespace path or query param) is rejected with 400 — don't re-prove the validator's own logic here, that belongs in `artifacts/application-parsers.md`

## Layer assignment

E2E only — this is the one layer that answers "is the HTTP contract correct?" Do not duplicate business logic assertions already covered by unit tests on the underlying service; do not duplicate the parser's branch coverage here.

## Setup pattern

`ApiTestFactory : WebApplicationFactory<Program>` boots the **real** app: real routing, real middleware, real JSON serialization, temp JSON data files for both domains. Override only the specific collaborator the test needs to control:

```csharp
public class DividendEndpointsTests
{
    [Fact]
    public async Task GetDividendSummary_ReturnsOk()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/dividends/BCIA11/summary?exchange=BVMF");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<DividendSummaryDTO>();
        summary!.Ticker.Should().Be("BCIA11");
    }

    [Fact]
    public async Task GetDividendSummary_WhenServiceThrows_ReturnsNotFoundWithFriendlyDetail()
    {
        await using var factory = CreateFactory(throwOnLookup: true);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/dividends/ASDF/summary?exchange=BVMF");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static WebApplicationFactory<Program> CreateFactory(bool throwOnLookup = false) =>
        new ApiTestFactory().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDividendService>();
                services.AddSingleton<IDividendService>(new DividendServiceStub(throwOnLookup));
            }));
}
```

Key points:
- `await using var factory` — the factory owns temp data files it must delete on dispose; a plain `using` on a synchronous `Dispose` also works, but `await using` matches `ApiTestFactory`'s async-friendly shape
- Override collaborators via `RemoveAll<T>()` + `AddSingleton<T>()` in `ConfigureTestServices`/`ConfigureServices` — same DI-swap technique the module tests use, applied at the whole-app level
- Seed only the data the test needs; `ApiTestFactory` writes a real temp copy of `data.json` and a seeded `data-cashflow.json` so both domains resolve correctly even for tests that don't touch CashFlow

## When to skip

- Don't add an E2E test for a validation rule already fully covered by a `[Theory]` on the parser (`artifacts/application-parsers.md`) — one wiring test per endpoint proving the validator fires is enough
- Don't re-test business calculation correctness here if the calculator/service already has unit coverage — E2E proves the endpoint *uses* the result correctly (status code, shape), not that the math is right

## Examples from project

- `DividendEndpointsTests` — full status-code + JSON-contract + error-mapping matrix for one endpoint pair; use as the template for new endpoints
- `ApiTestFactory` — the shared factory; extend its `ConfigureWebHost` if a new endpoint needs a new overridable collaborator, following the existing `IExchangeRateProvider` override pattern
