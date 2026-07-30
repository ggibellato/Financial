> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# API Controllers (`*Controller.cs`)

Examples: `AssetPricesController`, `AssetsController`, `NavigationController`, `XirrController`, `SummaryController`, `DividendsController`, and the rest of `Financial.Api/Controllers/`.

## What to test

**Only constructor null-guards** — nothing else. Controller business behavior (status codes, response bodies, validation, error mapping) is proven by `artifacts/api-endpoints-e2e.md` instead, which exercises the real HTTP pipeline.

## Layer assignment

Unit, but narrow in scope: `ControllerGuardClauseTests` exists specifically because these guard clauses are **unreachable via a real HTTP call** — DI never passes a null constructor argument, and `[ApiController]`'s automatic model validation short-circuits a null non-nullable `[FromBody]` parameter before the action method runs. The only way to exercise these lines at all is to construct the controller directly.

Everything else about the controller (routing, status codes, validation wiring, business behavior) belongs in `artifacts/api-endpoints-e2e.md` — do not write additional unit tests here.

## Setup pattern

```csharp
public class ControllerGuardClauseTests
{
    [Fact]
    public void AssetPricesController_NullService_Throws()
    {
        Action act = () => new AssetPricesController(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SummaryController_NullPortfolioAssetSummaryService_Throws()
    {
        Action act = () => new SummaryController(new StubSummaryService(), null!, new StubBrokerBreakdownService());

        act.Should().Throw<ArgumentNullException>().WithParameterName("portfolioAssetSummaryService");
    }
}
```

Group all controllers' guard-clause tests in one file (`ControllerGuardClauseTests`), one `[Fact]` per constructor parameter, rather than a separate test class per controller — this keeps the narrow scope obvious and avoids a proliferation of near-empty test classes.

## When to skip

- Any test that exercises an action method's logic, status code, or response shape — that's an E2E concern (`artifacts/api-endpoints-e2e.md`)
- A controller with no constructor dependencies (nothing to null-guard) needs no test file at all

## Examples from project

- `ControllerGuardClauseTests` — one `[Fact]` per controller constructor parameter across the whole `Financial.Api.Tests/Controllers` folder
