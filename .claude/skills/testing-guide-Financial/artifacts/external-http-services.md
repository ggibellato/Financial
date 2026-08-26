> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# External HTTP Services (e.g. `FrankfurterExchangeRateProvider`)

Any Infrastructure class whose job is to call an external HTTP API and parse the response.

## What to test

- Successful response → correctly parsed result
- Non-success HTTP status → the documented fallback (usually `null`, not an exception)
- Malformed/unexpected response body → same documented fallback
- Network-level failure (`HttpRequestException`) → same documented fallback
- Response missing an expected field (e.g., requested currency absent from the rates payload) → same documented fallback

This five-case matrix (success, bad status, malformed body, transport exception, missing-field) is the project's established standard for any HTTP-backed provider — apply it to every new one.

## Layer assignment

Integration — this crosses a real system boundary (an external HTTP API), even without complex branching. **Configured dependency contract**, not framework behavior: a wrong base URL, wrong query param, or wrong JSON path is a real bug this test catches that a mock never would.

**Strategy: real `HttpClient` + fake `HttpMessageHandler`.** Never hit the real external API in tests (rate limits, network flakiness, cost) and never mock `IExchangeRateProvider`/equivalent itself (that would hide the exact bugs this test exists to catch).

## Setup pattern

```csharp
private sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
        Task.FromResult(_responder(request));
}

[Fact]
public async Task GetHistoricalRateAsync_WithSuccessfulResponse_ParsesTheRate()
{
    var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("""{"amount":1,"base":"BRL","date":"2026-07-01","rates":{"GBP":0.146}}""")
    });
    var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app/") };
    var provider = new FrankfurterExchangeRateProvider(client, NullLogger<FrankfurterExchangeRateProvider>.Instance);

    var rate = await provider.GetHistoricalRateAsync(new DateOnly(2026, 7, 1), Currency.BRL, Currency.GBP);

    rate.Should().Be(0.146m);
}
```

Copy the `FakeHttpMessageHandler` shape for any new HTTP-backed provider rather than reaching for a mocking library — it's a ~10-line class and keeps the "no mocking framework" convention intact.

## When to skip

- Don't add a 6th test proving the request URL is well-formed if the parsing tests already exercise the real request path implicitly — only add a dedicated request-shape test if the URL/params have their own branching (e.g., optional query params)

## Examples from project

- `FrankfurterExchangeRateProviderTests` — the canonical 5-case example; use it as the template for any new external HTTP integration (e.g., if `GoogleFinancialSupport`'s HTTP calls are ever refactored behind a testable seam — see `artifacts/google-api-wrappers.md` for why they currently aren't)
