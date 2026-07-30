> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Application Services (`*.Application/Services/*.cs`)

Examples: `ControleMaeService`, `BankService`, `CardStatementService`, `ExpenseService`, `IncomeService`, `InvestmentSnapshotService`, `MensaisService`, `ReserveService`, `TitheService`, `AnnualSummaryService` (CashFlow); transaction/summary/xirr/profit services (Investment).

## What to test

- Branching logic: currency conversion decisions, date-scoping rules, conditional aggregation
- Correct delegation to injected collaborators (repository, `IExchangeRateProvider`, etc.) — but only as a side effect of testing an actual behavior, never as a bare "was called" assertion
- Error handling: what the service does when a collaborator returns null/empty/throws

## Layer assignment

| Characteristic | Layer |
|---|---|
| Branching logic only, collaborators are interfaces | Unit — hand-written stub implementing the interface |
| Also crosses a system boundary itself (rare at this layer — usually pushed to Infrastructure) | Add an integration test at the Infrastructure layer instead; don't duplicate in Application |

This project has **no mocking framework**. Every Application service test builds a small stub class implementing the dependency interface, matching the existing `StubRepository`/`StubFinanceService`/`DividendServiceStub` pattern.

## Setup pattern

```csharp
internal sealed class StubExchangeRateProvider : IExchangeRateProvider
{
    private readonly decimal? _rate;
    public StubExchangeRateProvider(decimal? rate) => _rate = rate;

    public Task<decimal?> GetHistoricalRateAsync(DateOnly date, Currency from, Currency to)
        => Task.FromResult(_rate);
}

public class ControleMaeServiceTests
{
    [Fact]
    public async Task ConvertToGbp_WhenRateAvailable_AppliesRate()
    {
        var service = new ControleMaeService(new StubExchangeRateProvider(0.146m), new StubCashFlowRepository());

        var result = await service.ConvertAsync(1000m, Currency.BRL, Currency.GBP, new DateOnly(2026, 7, 1));

        result.Should().Be(146m);
    }

    [Fact]
    public async Task ConvertToGbp_WhenRateUnavailable_ReturnsNull()
    {
        var service = new ControleMaeService(new StubExchangeRateProvider(null), new StubCashFlowRepository());

        var result = await service.ConvertAsync(1000m, Currency.BRL, Currency.GBP, new DateOnly(2026, 7, 1));

        result.Should().BeNull();
    }
}
```

Only implement the interface members the test actually exercises; throw `NotImplementedException` for the rest, matching `StubRepository`.

## When to skip

- A service that only delegates to one collaborator with zero branching (e.g., a pure pass-through `GetAll()`) — that's wiring, not logic; if it's worth verifying, do it via the E2E test that exercises the endpoint calling it
- Duplicate coverage already proven by a DI module resolution test (`artifacts/dependency-injection-modules.md`) — that test proves wiring, not service logic

## Examples from project

- `ControleMaeService` — currency-conversion branching depending on `IExchangeRateProvider` → unit, stub provider
- `TitheService` — 10%-of-net-income calculation, no I/O → unit, no stub needed beyond a repository stub for input data
- `ReserveService` — branches across multiple reserve buckets → unit, `StubCashFlowRepository`
