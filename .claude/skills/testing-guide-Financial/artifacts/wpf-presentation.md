> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# WPF Presentation (`*ViewModel.cs`, `*Converter.cs`, Helpers)

Located in `Financial.App` (WPF), tested from `Tests/Financial.Presentation.Tests/{ViewModels,Converters,Helpers}`. Presentation Layer per CLAUDE.md — must contain no business logic, but view models do coordinate calls to Application services and hold UI-facing state.

## What to test

**ViewModels:**
- State changes after a "Load*" method runs (e.g., `LoadBrokerSummary` sets `IsBrokerView` and aggregated totals)
- Branching over scope/mode parameters (e.g., `InvestmentScope.Active` vs other scopes)
- Correct use of injected Application services to populate view-facing properties

**Converters** (`IValueConverter` implementations):
- `Convert`/`ConvertBack` for each meaningfully different input, including null and out-of-range values

**Helpers:**
- Any branching or edge-case handling; skip pure pass-through helpers

## Layer assignment

Unit only — no mocking framework here either. ViewModel tests inject hand-written stub Application services, exactly like `artifacts/application-services.md`.

**Reminder — WPF binding gotcha (unrelated to what to test, but relevant when writing new bindings the ViewModel tests will exercise):** never bind `<Run Text="{Binding}"/>` — it defaults to `TwoWay` and crashes on a private setter; use `TextBlock` + `StringFormat` instead.

## Setup pattern

```csharp
public class AssetDetailsViewModelBrokerSummaryTests
{
    private static AssetDetailsViewModel BuildViewModel(
        IBrokerBreakdownService? brokerBreakdownService = null,
        InvestmentScope scope = InvestmentScope.Active) =>
        new(
            new StubTransactionService(),
            new StubCreditService(),
            new StubAssetPriceService(),
            brokerBreakdownService ?? new StubBrokerBreakdownService(),
            new StubTransactionQueryService(),
            new XirrCalculationService(),   // real: pure calculation, no boundary
            new ProfitCalculationService(), // real: pure calculation, no boundary
            scope);

    [Fact]
    public void LoadBrokerSummary_SetsIsBrokerViewTrue()
    {
        var vm = BuildViewModel();

        vm.LoadBrokerSummary("XPI", new AggregatedSummaryDTO(), []);

        vm.IsBrokerView.Should().BeTrue();
    }
}
```

Note the mix: services with pure computation and no dependencies (`XirrCalculationService`, `ProfitCalculationService`) are constructed for real; services that would otherwise reach a repository or external system are stubbed.

## When to skip

- Plain pass-through properties with no formatting/computation
- Converters with only one trivial branch (e.g., always returns `value.ToString()`)

## Examples from project

- `AssetDetailsViewModelBrokerSummaryTests`, `...CreditsChartTests`, `...PortfolioSummaryTests` — one test class per ViewModel concern, all using the same `BuildViewModel` stub-injection helper
