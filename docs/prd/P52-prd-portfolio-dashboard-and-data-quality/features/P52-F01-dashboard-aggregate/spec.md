# P52-F01 — Dashboard Aggregate — Technical Spec

## 1. Overview

F01 adds a single computed, read-only Application-layer surface — `PortfolioDashboardDTO` — that
aggregates whole-portfolio investment figures (market value, invested amount, unrealised and
lifetime realised gain, income year-to-date and lifetime, and a genuine money-weighted gross/net
portfolio XIRR) across every Active and Historic broker, plus a `Converted*` counterpart for every
money figure when the reporting-currency setting (P49-F03) is on. It is computed on demand from the
already-loaded in-memory `Investments` graph on every request — nothing is persisted, and nothing
in this feature changes the JSON document shape.

The feature is a pure aggregation of figures the backend already knows how to compute per
broker/asset (`HoldingValuationService`, `AssetAmountBases`, `AssetCashFlowBuilder`,
`XirrCalculator`, `DisposalRecord.GainLoss`, `Credit.NetAmount`) into one portfolio-wide answer.
Confirmed by research: no Domain change and no Infrastructure change are needed. Every input F01
needs is already reachable through `IInvestmentRepository.GetInvestments()` (the aggregate root,
exposing both `ActiveBrokers` and `HistoricBrokers`) and the existing `IHoldingValuationService` /
`IXirrCalculationService` / `IExchangeRateProvider` / `IReportingCurrencyProvider` Application
interfaces. This feature adds one new Application DTO, one new Application service (plus two small
internal builder classes), and one new Api controller endpoint.

### Decisions / Assumptions

Recorded here because the PRD does not spell these out and no user was available to ask.

1. **Route path.** Every existing controller in `Financial.Api/Controllers` uses a flat kebab-case
   segment directly under the `api/v{version}/financial` group set up in `Program.cs`
   (`[Route("summary")]`, `[Route("xirr")]`, `[Route("payments-due")]` — none nests under an
   `investment/` segment). The PRD's example URL for F03
   (`GET /api/v1/financial/investment/data-quality-report`) does not match this convention. For
   F01 this spec follows the observed, consistent convention: a new `DashboardController` at
   `[Route("dashboard")]`, giving `GET /api/v1/financial/dashboard`. (F03's own spec should make
   the same call for its endpoint; not this feature's concern to fix.)
2. **Service name and shape.** New `IPortfolioDashboardService` / `PortfolioDashboardService`
   (mirrors `ISummaryService`/`SummaryService` naming), registered as `AddSingleton` in
   `AddFinancialApplication()` (the actual registration method in
   `Financial.Investment.Application/DependencyInjection/InvestmentApplicationServiceCollectionExtensions.cs`
   — CLAUDE.md's `AddFinancial<Context>Application` naming is illustrative; the Investment
   context's real method is `AddFinancialApplication`). One method,
   `PortfolioDashboardDTO GetDashboardAsync()`, no parameters — F01 always spans the whole
   portfolio, so there is no broker/portfolio/scope route parameter the way `SummaryController`
   has.
3. **XIRR computed via `XirrCalculator.Calculate` directly, not `IXirrCalculationService`.**
   `IXirrCalculationService.Calculate(cashFlows, terminalValue, asOf)` appends exactly one
   synthetic terminal cash flow to the series. F01 needs one terminal cash flow *per priced Active
   holding* (each on `asOf` but at that holding's own market value) layered on top of the combined
   dated flows from every holding — a shape the existing interface cannot express. A new internal
   static builder (`PortfolioXirrBuilder`, Application layer) assembles the full
   `IReadOnlyList<(DateTime Date, decimal Amount)>` series (dated flows from
   `AssetCashFlowBuilder.BuildWithCredits`/`BuildNetOfTaxWithCredits` for every asset in both
   scopes, plus one `(asOf, MarketValue)` entry per priced Active holding) and calls
   `Financial.Investment.Domain.Rules.XirrCalculator.Calculate(series)` directly — the same
   Domain rule `XirrCalculationService` itself wraps, just with the multi-terminal-value series
   `IXirrCalculationService`'s two-argument shape can't build. `IXirrCalculationService` itself is
   left unchanged; no existing caller is touched.
4. **`RealisedGainLoss` reads `DisposalRecord.GainLoss` directly, not `Asset.RealizedGainLoss`.**
   Research finding: `Asset.RealizedGainLoss` (note American spelling on the entity, British on
   the PRD/DTO) is `DisposalRecords.Where(Active).Sum(GainLoss) + Credits.Sum(c => c.Value)` —
   it already folds in every credit's *gross* value. Using it for F01's `RealisedGainLoss` would
   double-count income against `IncomeYtd`/`IncomeLifetime`, which separately sum
   `Credit.NetAmount`. F01's `RealisedGainLoss` therefore sums
   `asset.DisposalRecords.Where(d => d.Status == DisposalRecordStatus.Active).Sum(d => d.GainLoss)`
   directly, across every asset in both `ActiveBrokers` and `HistoricBrokers` — never the entity
   property of the same near-name.
5. **`IncomeYtd`/`IncomeLifetime` span both scopes.** The PRD's Executive Summary says the whole
   DTO is "computed... from every Active and Historic broker's holdings"; income is a lifetime
   fact independent of whether a holding is still open, so both sums iterate
   `ActiveBrokers ∪ HistoricBrokers`'s credits — consistent with `RealisedGainLoss`'s same
   dual-scope treatment (Decision 4) and distinct from `MarketValue`/`Invested`/
   `UnrealisedGainLoss`, which the PRD explicitly scopes to Active only.
6. **Multi-currency conversion needs a new builder; `ConvertedSummaryBuilder` cannot be reused
   unmodified.** `ConvertedSummaryBuilder.BuildAsync` takes one `Currency brokerCurrency` — it
   assumes every asset it converts shares one native currency, true for a single broker's summary
   but false for a whole-portfolio aggregate spanning brokers in different currencies (this is a
   BR+UK app; brokers are not all the same currency). F01 needs a new
   `PortfolioDashboardConvertedBuilder` that groups assets by their owning broker's `Currency`,
   builds one currency-conversion cache per distinct source currency present in the portfolio (one
   `ConversionContext`-shaped cache per currency, reusing the existing per-date rate-caching
   pattern), converts each asset's own transactions/credits/terminal value using its own broker's
   currency as the source, then sums the converted totals across all currencies into the six
   converted money figures and the two converted XIRRs. `ConvertedSummaryBuilder`'s private
   `ConversionContext` nested class is extracted to a new internal, currency-parameterised
   `Financial.Investment.Application/Services/CurrencyConversionContext.cs` shared by both
   builders — `ConvertedSummaryBuilder`'s own behaviour is unchanged (it now constructs one shared
   context instead of a private nested one).
7. **Two distinct "partial" concepts get two distinct field names.** The PRD explicitly reuses
   the field name `IsPartial` for F01 but gives it a *different meaning* than
   `AggregatedSummaryDTO.IsPartial` ("some, but not all, contributing records converted
   successfully"): for F01, `IsPartial` means "at least one Active holding has no market value" —
   nothing to do with currency conversion. Because F01's DTO *also* carries a reporting-currency
   conversion (Decision 6) that can independently be partial, giving both conditions the same
   property name on the same DTO would be genuinely ambiguous to a consumer (unlike
   `AggregatedSummaryDTO`, which only ever has one partial-cause). This spec keeps `IsPartial`
   exactly as the PRD defines it (missing-price cause) and names the conversion-partial flag
   `IsReportingCurrencyPartial` instead of reusing `IsPartial` for it. `IsReportingCurrencyEnabled`
   and `IsReportingCurrencyUnavailable` keep their `AggregatedSummaryDTO` names and meanings
   unchanged.
8. **`UnvaluedHoldingCount` is added (not in the PRD's field list) to support the Experience
   section's inline notice ("explains which and how many holdings are excluded").** Mirrors
   `AggregatedSummaryDTO.UnvaluedHoldingCount` exactly — a plain Active-scope count, no holding
   names (the PRD reserves the actual list for F03's click-through, out of scope here).
9. **Historic holdings' cash flows use `BuildWithCredits`/`BuildNetOfTaxWithCredits` unmodified,
   with no terminal value appended** — the PRD calls this out explicitly (G14's existing
   zero-terminal convention for closed positions), so no new Domain/Application rule is needed:
   simply never add a `(asOf, MarketValue)` entry for a Historic-scope asset.

## 2. Scope

### Included
- New `PortfolioDashboardDTO` (Application DTO), computed on demand.
- New `IPortfolioDashboardService` / `PortfolioDashboardService` (Application), assembling the DTO
  from `IInvestmentRepository.GetInvestments()`, `IHoldingValuationService`,
  `IExchangeRateProvider`, `IReportingCurrencyProvider`, `TimeProvider`.
- New internal builders: `PortfolioXirrBuilder` (combined multi-holding XIRR series) and
  `PortfolioDashboardConvertedBuilder` (multi-currency reporting-currency conversion).
- Extraction of `ConvertedSummaryBuilder`'s private `ConversionContext` into a shared internal
  `CurrencyConversionContext` class, with `ConvertedSummaryBuilder` updated to use it (behaviour
  unchanged, verified by its existing test suite continuing to pass unmodified).
- New `DashboardController` (`Financial.Api/Controllers`), one `GET /api/v1/financial/dashboard`
  endpoint returning `PortfolioDashboardDTO`.
- DI registration of `IPortfolioDashboardService` in `AddFinancialApplication()`.
- OpenAPI snapshot + generated `Financial.Web/src/api/generated/openapi.ts` regeneration (the new
  DTO and endpoint are a wire-format change).
- Unit tests for the new service/builders; one Integration `ApiEndpointTests`-derived suite
  covering the endpoint and every F01 §9 acceptance criterion.

### Deferred (to their own PRD features, already scheduled)
- Allocation breakdown by class/currency/country/broker — **F02**.
- Data-quality warnings panel and the `data-quality-report` endpoint extension — **F03**.
- Upcoming-income projection — **F04**.
- Any UI: KPI tiles, loading skeletons, the reporting-currency secondary line, the `IsPartial`
  inline notice, the nav entry — **F05** (React) and **F06** (WPF). This spec is backend-only.

### Out of scope (per PRD §7, applies to this feature too)
- Computing tax due.
- Bond maturity/coupon-schedule modelling.
- CSV or any other export.
- A configurable staleness threshold.
- Automatic remediation of any data-quality finding.
- Changes to `AggregatedSummaryTab`/`PortfolioSummaryTab`/`AssetSummaryTab` or their backing
  `SummaryController`/`SummaryService` endpoints — F01 adds a new, additive surface only.

## 3. Architecture / Component Overview

### Domain — no changes
No new entity, value object, or `Rules/` calculator. Every input (`DisposalRecord.GainLoss`,
`Credit.NetAmount`, `Asset.Transactions`, `Asset.Quantity`/`AveragePrice`,
`Investments.ActiveBrokers`/`HistoricBrokers`) and every calculation primitive
(`XirrCalculator.Calculate`, `HoldingValuationCalculator.Calculate`) already exists and is already
unit-tested at the Domain layer.

### Application

| File | Change |
|---|---|
| `Financial.Investment.Application/DTOs/PortfolioDashboardDTO.cs` | **New.** The response shape (§4). |
| `Financial.Investment.Application/Interfaces/IPortfolioDashboardService.cs` | **New.** `PortfolioDashboardDTO GetDashboardAsync();` |
| `Financial.Investment.Application/Services/PortfolioDashboardService.cs` | **New.** Orchestrates repository read, per-asset valuation, XIRR, and (conditionally) currency conversion; owns span/log wrapper per `docs/rules/implementation.md`. |
| `Financial.Investment.Application/Services/PortfolioXirrBuilder.cs` | **New**, `internal static`. Builds the combined gross/net cash-flow series (Decision 3) and calls `XirrCalculator.Calculate` for each. |
| `Financial.Investment.Application/Services/PortfolioDashboardConvertedBuilder.cs` | **New**, `internal static`. Multi-currency `Converted*` figures (Decision 6). |
| `Financial.Investment.Application/Services/CurrencyConversionContext.cs` | **New**, `internal sealed`. Extracted from `ConvertedSummaryBuilder`'s private nested `ConversionContext` — same caching behaviour, now constructible with an explicit source `Currency` so more than one can coexist per request. |
| `Financial.Investment.Application/Services/ConvertedSummaryBuilder.cs` | **Changed.** Its private `ConversionContext` class is removed; it constructs a `CurrencyConversionContext` instead. No behavioural change. |
| `Financial.Investment.Application/DependencyInjection/InvestmentApplicationServiceCollectionExtensions.cs` | **Changed.** One new line: `services.AddSingleton<IPortfolioDashboardService, PortfolioDashboardService>();` |

### Infrastructure — no changes
This feature is computed on demand from the already-loaded `IInvestmentRepository` graph; no new
persistence, no new external call, no new repository method.

### Presentation

| File | Change |
|---|---|
| `Financial.Api/Controllers/DashboardController.cs` | **New.** `[ApiController] [Route("dashboard")]`, one `[HttpGet]` action, `GetPortfolioDashboardAsync()`, injecting `IPortfolioDashboardService`, returning `Ok(PortfolioDashboardDTO)`. No route parameters, no `[FromQuery] scope` (unlike `SummaryController`) — F01 is always whole-portfolio. |

No `Financial.App` or `Financial.Web` file changes in this feature (F05/F06 consume the endpoint
later).

## 4. API Contracts

### `GET /api/v1/financial/dashboard`

No request body, no query string, no route parameters.

**200 OK** — `PortfolioDashboardDTO`:

```json
{
  "marketValue": 812345.67,
  "invested": 705000.00,
  "unrealisedGainLoss": 107345.67,
  "realisedGainLoss": 42310.55,
  "incomeYtd": 3120.44,
  "incomeLifetime": 28904.10,
  "grossXirr": 0.0912,
  "netXirr": 0.0781,
  "unvaluedHoldingCount": 0,
  "isPartial": false,
  "reportingCurrency": "GBP",
  "isReportingCurrencyEnabled": true,
  "convertedMarketValue": 5123456.70,
  "convertedInvested": 4450000.00,
  "convertedUnrealisedGainLoss": 673456.70,
  "convertedRealisedGainLoss": 266850.00,
  "convertedIncomeYtd": 19680.00,
  "convertedIncomeLifetime": 182150.00,
  "convertedGrossXirr": 0.0905,
  "convertedNetXirr": 0.0774,
  "isReportingCurrencyPartial": false,
  "isReportingCurrencyUnavailable": false
}
```

**Partial-data example** (one Active holding unpriced, reporting currency off):

```json
{
  "marketValue": 780000.00,
  "invested": 705000.00,
  "unrealisedGainLoss": 75000.00,
  "realisedGainLoss": 42310.55,
  "incomeYtd": 3120.44,
  "incomeLifetime": 28904.10,
  "grossXirr": 0.0812,
  "netXirr": 0.0691,
  "unvaluedHoldingCount": 1,
  "isPartial": true,
  "reportingCurrency": "GBP",
  "isReportingCurrencyEnabled": false,
  "convertedMarketValue": null,
  "convertedInvested": null,
  "convertedUnrealisedGainLoss": null,
  "convertedRealisedGainLoss": null,
  "convertedIncomeYtd": null,
  "convertedIncomeLifetime": null,
  "convertedGrossXirr": null,
  "convertedNetXirr": null,
  "isReportingCurrencyPartial": false,
  "isReportingCurrencyUnavailable": false
}
```

There is no error/400 case: the endpoint takes no input to validate. An unexpected exception falls
through to `DomainExceptionMappingMiddleware`/the default ASP.NET Core problem-details handler,
exactly as every other read endpoint in this API.

## 5. Data Model

**N/A — nothing persists.** `PortfolioDashboardDTO` is computed fresh from the in-memory
`Investments` graph on every call to `GetDashboardAsync()`; there is no new field on
`data-investment.json`, no schema/migration concern, and no caching between requests (consistent
with every other Summary-family endpoint in this codebase).

## 6. Requirements / Business Rules

All rules below operate over `investments = repository.GetInvestments()`,
`activeAssets = investments.ActiveBrokers.SelectMany(b => b.Portfolios.SelectMany(p => p.Assets))`,
`historicAssets` likewise from `HistoricBrokers`, and `asOf = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)`
(mirrors `HoldingValuationService`'s own `TimeProvider` usage, for testability with `FakeTimeProvider`).

1. **MarketValue** — for each asset in `activeAssets`, call
   `holdingValuationService.GetValuation(asset, InvestmentScope.Active)`; sum `valuation.MarketValue`
   over every asset where it is non-null. An asset with `valuation.MarketValue is null` (no price
   snapshot, per `HoldingValuationCalculator.Calculate`) contributes 0 to this sum and increments
   `UnvaluedHoldingCount`.
2. **Invested** — for each asset in `activeAssets`, compute
   `AssetAmountBases.For(InvestmentScope.Active, AssetTotals.For(asset)).InvestedAmount` (the
   existing "cost basis of currently-held units" figure, `OpenPositionCost` under the covers) and
   sum unconditionally — unlike `MarketValue`, a holding with no price still contributes its cost
   basis.
3. **UnrealisedGainLoss** = `MarketValue - Σ(InvestedAmount over priced Active holdings only)`.
   Concretely: accumulate `valuation.UnrealisedGain` (already `MarketValue - CostOfUnitsHeld` per
   `HoldingValuationCalculator`) over the same priced-only subset used for `MarketValue`, so an
   unpriced holding's cost basis (already counted in `Invested`) is not left dangling against a
   `MarketValue` that excludes it.
4. **RealisedGainLoss** — over `activeAssets.Concat(historicAssets)`, sum
   `asset.DisposalRecords.Where(d => d.Status == DisposalRecordStatus.Active).Sum(d => d.GainLoss)`.
   Never read `Asset.RealizedGainLoss` (Decision 4 — that property also folds in gross credit
   value and would double-count against `IncomeYtd`/`IncomeLifetime`). A `Superseded`
   `DisposalRecord` (P50-F03) is excluded by the `Status == Active` filter — this is the AC that
   directly proves P50's supersede-never-rewrite policy holds here too.
5. **IncomeYtd** — over `activeAssets.Concat(historicAssets)`, sum `credit.NetAmount` for every
   `Credit` whose `Date >= new DateTime(asOf.Year, 1, 1)` (host-local year, matching `asOf`'s own
   host-local date — no UTC/local ambiguity beyond what `HoldingValuationService` already accepts
   for "today"). **IncomeLifetime** — same sum, no date floor.
6. **GrossXirr / NetXirr** — via `PortfolioXirrBuilder`:
   - `grossFlows` = `activeAssets.Concat(historicAssets).SelectMany(AssetCashFlowBuilder.BuildWithCredits)`
     plus one `(asOf.ToDateTime(TimeOnly.MinValue), valuation.MarketValue.Value)` entry for every
     `activeAssets` member with a non-null valuation (from step 1's already-computed valuations —
     do not recompute).
   - `netFlows` = the same shape, substituting
     `AssetCashFlowBuilder.BuildNetOfTaxWithCredits` for the dated flows (terminal entries are
     identical between gross and net, matching `HoldingValuationService`'s own per-holding
     precedent of using the same `valuation.MarketValue` for both).
   - `GrossXirr = XirrCalculator.Calculate(grossFlows)`, `NetXirr = XirrCalculator.Calculate(netFlows)` —
     called directly (Decision 3), not through `IXirrCalculationService`.
   - A Historic asset contributes only its dated flows, never a terminal entry (Decision 9).
7. **IsPartial** = `activeAssets.Any(a => holdingValuationService.GetValuation(a, InvestmentScope.Active).MarketValue is null)`
   — equivalently, `UnvaluedHoldingCount > 0`. When true, `MarketValue`, `UnrealisedGainLoss`,
   `GrossXirr` and `NetXirr` are all understated/excluding that holding's current contribution, per
   rules 1, 3 and 6 above — this DTO does not attempt to "fill the gap"; the caller (F05/F06) is
   responsible for the inline notice.
8. **Reporting-currency conversion** (only when `reportingCurrencyProvider.IsReportingCurrencyEnabled()`
   is true; otherwise every `Converted*` field is `null`, `IsReportingCurrencyEnabled = false`,
   `IsReportingCurrencyPartial = false`, `IsReportingCurrencyUnavailable = false` — identical
   short-circuit to `SummaryService.Aggregate`):
   - `PortfolioDashboardConvertedBuilder` groups `activeAssets`/`historicAssets` by their owning
     `Broker.Currency`, builds one `CurrencyConversionContext` per distinct source currency, and
     converts every transaction's `NetCash`, every credit's `Value`/`NetAmount`, and (for Active,
     priced holdings) the terminal `MarketValue`, each on its own record date — the same
     per-record, own-date rule `ConvertedSummaryBuilder` already applies for a single-currency
     broker.
   - The six converted money totals (`ConvertedMarketValue`, `ConvertedInvested`,
     `ConvertedUnrealisedGainLoss`, `ConvertedRealisedGainLoss`, `ConvertedIncomeYtd`,
     `ConvertedIncomeLifetime`) are the sums of each currency group's converted contributions.
   - `ConvertedGrossXirr`/`ConvertedNetXirr` re-run `XirrCalculator.Calculate` over the *converted*
     combined flow series (mirroring `ConvertedSummaryBuilder.ComputeConvertedReturns`'s existing
     precedent of converting `TotalReturn`/`TotalReturnNetOfTax` despite them being rates, not
     currency totals).
   - `IsReportingCurrencyUnavailable` = true only when **every** currency context failed every
     conversion attempt (no `Converted*` figure could be produced at all).
   - `IsReportingCurrencyPartial` = true when at least one conversion attempt failed **and** at
     least one succeeded, across all currency contexts combined (a broker whose currency matches
     the reporting currency contributes zero attempts for that leg, matching
     `CurrencyConversionContext`'s existing `from == to` short-circuit).
9. All monetary sums use `decimal` throughout (never `double`), matching every existing DTO in this
   context; `GrossXirr`/`NetXirr`/`Converted*Xirr` are `decimal?` (null when
   `XirrCalculator.Calculate` returns null — e.g., fewer than 2 dated points or no sign change,
   per its own documented contract).

## 7. Testing Strategy

Per `testing-guide-Financial`: Unit for the new service/builders' branches +
failed-span/log assertions, Integration (`ApiEndpointTests`-derived) for the wired endpoint and the
mandatory AC-tracing subset. This PRD (P52) has no AC ids yet; per
`references/feature-traceability.md`, add them to §9's F01 group the first time an AC-tracing test
is written — plan.md's final phase includes this as an explicit step
(`P52-F01-dashboard-aggregate-01..04`, in the order the four bullets already appear in §9).

### Unit — `Tests/Financial.Investment.Application.Tests/Services/`

- `PortfolioDashboardServiceTests.cs` (pattern: `SummaryServiceTests.cs`/`StubInvestmentRepository`
  with `Investments` set, `StubReportingCurrencyProvider`, `StubExchangeRateProvider`,
  `TestHoldingValuationService`, `RecordingTelemetryTracer`, `FakeTimeProvider`):
  - Constructor null-guard tests, one per dependency (mirrors `SummaryServiceTests`' six
    `Constructor_WithNull*_Throws` tests).
  - `GetDashboardAsync_SumsMarketValueAcrossActiveBrokersOnly` — a Historic broker's holding must
    not contribute to `MarketValue`.
  - `GetDashboardAsync_InvestedIncludesUnpricedActiveHoldings` — an unpriced Active holding still
    contributes its cost basis to `Invested` but not to `MarketValue`.
  - `GetDashboardAsync_UnrealisedGainLoss_ExcludesUnpricedHoldingsCostBasis`.
  - `GetDashboardAsync_RealisedGainLoss_SumsActiveDisposalRecordsAcrossBothScopes`.
  - `GetDashboardAsync_RealisedGainLoss_ExcludesSupersededDisposalRecords`.
  - `GetDashboardAsync_RealisedGainLoss_DoesNotDoubleCountCreditsViaAssetRealizedGainLoss` — the
    regression test for Decision 4: an asset whose `Asset.RealizedGainLoss` would differ from the
    sum of `DisposalRecords.GainLoss` alone (i.e., non-zero credits present) proves
    `RealisedGainLoss` matches the latter, not the former.
  - `GetDashboardAsync_IncomeYtd_SumsCreditsFromJanuaryFirstOfCurrentYear` (with `FakeTimeProvider`).
  - `GetDashboardAsync_IncomeYtd_ResetsAcrossNewCalendarYear` (two runs with a different
    `FakeTimeProvider` year, same fixture).
  - `GetDashboardAsync_IncomeLifetime_NeverAppliesADateFloor`.
  - `GetDashboardAsync_IsPartial_TrueWhenAnyActiveHoldingHasNoMarketValue`.
  - `GetDashboardAsync_IsPartial_FalseWhenEveryActiveHoldingIsPriced`.
  - `GetDashboardAsync_UnvaluedHoldingCount_MatchesUnpricedActiveHoldingCount`.
  - `GetDashboardAsync_WhenReportingCurrencyDisabled_EveryConvertedFieldIsNull`.
  - `Constructor_WithNullRepository_RecordsFailedSpanOnGetDashboardAsyncFailure` (or equivalent
    negative-path test using `StubInvestmentRepository.ThrowOnGetBrokerList`/`ThrowOnApplyAndSaveAsync`
    substitute — whichever repository member `PortfolioDashboardService` calls — asserting
    `RecordingTelemetryTracer`/`RecordingLogger<PortfolioDashboardService>` records the exception
    type, per the negative-path pattern in `SummaryServiceTests`/`references/negative-path-testing.md`).

- `PortfolioXirrBuilderTests.cs`:
  - `Build_AppendsOneTerminalEntryPerPricedActiveHolding`.
  - `Build_OmitsTerminalEntryForUnpricedActiveHolding`.
  - `Build_OmitsTerminalEntryForHistoricHoldings` (Decision 9 / G14).
  - `Build_NetSeriesUsesNetOfTaxCreditAmounts_GrossSeriesUsesGrossCreditValues`.

- `PortfolioDashboardConvertedBuilderTests.cs`:
  - `Build_ConvertsEachAssetUsingItsOwnBrokerCurrency` — two brokers in different native
    currencies, asserting each is converted with its own rate rather than one shared rate.
  - `Build_IsReportingCurrencyUnavailable_TrueOnlyWhenEveryCurrencyFails`.
  - `Build_IsReportingCurrencyPartial_TrueWhenOneCurrencyFailsAndAnotherSucceeds`.
  - `Build_SkipsConversionWhenBrokerCurrencyMatchesReportingCurrency` (the `from == to`
    short-circuit, ported from `CurrencyConversionContext`).

- `CurrencyConversionContextTests.cs` (new home for the coverage `ConvertedSummaryBuilderTests.cs`
  already has for the old private `ConversionContext` behaviour — moved, not duplicated, since the
  class itself moved):
  - Existing per-date caching, `IsPartial`/`IsUnavailable` computed-property tests carried over
    unchanged in behaviour, now against the standalone class.

- `ConvertedSummaryBuilderTests.cs` — no new tests required; existing suite must keep passing
  unmodified against the refactored (Decision 6) implementation, proving the extraction was
  behaviour-preserving.

### Integration — `Tests/Financial.Api.Tests/`

- `Controllers/DashboardControllerTests.cs` (pattern: `SummaryControllerTests`-equivalent, or the
  guard-clause-only convention if one doesn't yet exist for `SummaryController` — check at
  implementation time): `GetDashboard_ReturnsOk`.
- `Acceptance/PortfolioDashboardAggregateAcceptanceTests.cs` — one `[Fact]` per F01 §9 bullet, each
  tagged `[Trait("AC", "P52-F01-dashboard-aggregate-0N")]`:
  - `-01` — dashboard figures reconcile with the sum of per-broker `AggregatedSummaryDTO` figures:
    seed two Active brokers via the real host, call both `/api/v1/financial/summary/broker/{name}`
    per broker and `/api/v1/financial/dashboard`, assert `MarketValue`/`Invested`/
    `UnrealisedGainLoss`/`GrossXirr`≈`TotalReturn`/`NetXirr`≈`TotalReturnNetOfTax` sums match to
    the cent (XIRR reconciliation compares the *combined-series* XIRR against a manually-summed
    weighted expectation is not meaningful cent-for-cent the way money sums are — assert instead
    that the combined series used equals the concatenation of each broker's own series, which is
    the actual reconciliation contract F01 promises).
  - `-02` — `IsPartial` true + (endpoint-level) the notice's precondition: seed one unpriced Active
    holding, assert `IsPartial == true` and `UnvaluedHoldingCount == 1`.
  - `-03` — seed a `Superseded` `DisposalRecord` alongside an `Active` one for the same asset,
    assert `RealisedGainLoss` reflects only the Active record's `GainLoss`.
  - `-04` — seed one credit dated last calendar year and one dated this year; with two different
    `FakeTimeProvider` dates (via two test methods or a `[Theory]`), assert `IncomeYtd` excludes
    last year's credit while `IncomeLifetime` includes both.
- `Contract/OpenApiContractTests.cs` — no new test method; the existing
  `OpenApiDocument_NumericProperties_...` and snapshot-diff tests automatically cover the new
  endpoint/DTO once the snapshot is regenerated (plan.md's final phase).

### Not covered here (other features' responsibility)
- F02/F03/F04's own endpoints and DTOs.
- Any React/WPF rendering of these figures (F05/F06).
