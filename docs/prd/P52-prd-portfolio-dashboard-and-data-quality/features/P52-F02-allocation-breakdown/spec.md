# P52-F02 — Allocation Breakdown — Technical Spec

## 1. Overview

F02 adds a single computed, read-only Application-layer surface — `AllocationBreakdownDTO` — that
groups every priced, Active-scope holding into four independent breakdowns (asset class, currency,
country, broker), each sized by the exact market-value weighting the app already uses for per-asset
portfolio weight. It is computed on demand from the already-loaded in-memory `Investments` graph on
every request — nothing is persisted, and nothing in this feature changes the JSON document shape.

The feature is a pure re-grouping of a weight basis the backend already knows how to compute per
asset. Research confirms `AssetAmountBases.For(InvestmentScope.Active, AssetTotals.For(asset),
valuation.MarketValue).WeightBasis` is the literal expression `PortfolioAssetSummaryBuilder` already
evaluates for its own per-asset `PortfolioWeight` field (`ComputeAssetData` → `bases.WeightBasis`),
and that struct's own branch (`weightBasis = scope == Historic ? totals.TotalBought : marketValue`)
is exactly why an Active-scope, unpriced holding's basis is `null`: `PortfolioAssetSummaryBuilder`
already excludes such a holding from both its weight numerator (`CalculateWeight` returns `null`
when `weightBasis is null`) and denominator (`portfolioWeightBasis` sums only `computed.Where(c =>
c.WeightBasis.HasValue)`). F02 reuses this identical basis and identical null-exclusion rule, just
grouped four different ways across the whole Active-scope portfolio instead of ranked within one
broker/portfolio's asset list. Confirmed by research: no Domain change and no Infrastructure change
are needed. Every input is already reachable through `IInvestmentRepository.GetInvestments()`
(`Investments.ActiveBrokers`) and the existing `IHoldingValuationService` Application interface,
already registered in `AddFinancialApplication()`. This feature adds one new Application DTO family
(one container + four dimension-entry types), one new Application service (plus one small internal
builder class), and one new Api controller endpoint.

### Decisions / Assumptions

Recorded here because the PRD does not spell these out and no user was available to ask.

1. **Route path.** Following the identical convention F01 and F03 already recorded (every
   controller in `Financial.Api/Controllers` uses a flat kebab-case segment directly under the
   `api/v{version}/financial` group — `[Route("dashboard")]`, `[Route("data-quality-report")]`,
   never nested under `investment/`), this spec uses `[Route("allocation-breakdown")]` on a new
   `AllocationBreakdownController`, giving `GET /api/v1/financial/allocation-breakdown`.
2. **Service name and shape.** New `IAllocationBreakdownService` / `AllocationBreakdownService`
   (mirrors `IPortfolioAssetSummaryService`/`PortfolioAssetSummaryService` and
   `IBrokerBreakdownService`/`BrokerBreakdownService` naming), registered as `AddSingleton` in
   `AddFinancialApplication()`. One method, `AllocationBreakdownDTO GetAllocationBreakdown()`, no
   parameters, **synchronous** — unlike `IPortfolioDashboardService.GetDashboardAsync()` (F01),
   which is `async` only because it conditionally awaits `IExchangeRateProvider` for reporting-
   currency conversion (Decision 3 below removes that need entirely here), this feature has no
   async dependency, exactly like `PortfolioAssetSummaryService.GetPortfolioAssetsSummary` and
   `BrokerBreakdownService.GetBrokerBreakdown`, both synchronous today.
3. **No reporting-currency conversion.** F01's PRD Capabilities text explicitly calls out a
   `Converted*` counterpart "whenever the reporting-currency setting (P49-F03) is enabled"; F02's
   own Capabilities text never mentions this at all. Every `MarketValue` this feature sums is the
   holding's own native-currency value, potentially spanning more than one broker currency (this is
   a BR+UK app) with no conversion applied — the same behaviour `AggregatedSummaryDTO.MarketValue`
   (F01's own *non-converted* total field) already has by default, so mixing native currencies in a
   raw sum is an existing, accepted precedent in this codebase, not a new risk this feature
   introduces. Adding a reporting-currency-aware variant of the allocation breakdown is out of scope
   here; nothing in the PRD asks for it, and the Experience section's legend table shows a plain
   market value and percentage, not a `Converted*` secondary line the way F01's KPI tiles do.
4. **DTO shape: one container, four dimension-specific entry types, not one generic
   `{Label, Value, Percent}` shape shared across dimensions.** `AllocationBreakdownDTO` has four
   `IReadOnlyList<T>` properties (`ByClass`, `ByCurrency`, `ByCountry`, `ByBroker`); each entry type
   keeps its dimension's real domain type for the grouping key rather than collapsing everything to
   a string label:
   - `AssetClassAllocationEntryDTO.Class` is `GlobalAssetClass`, `[JsonConverter(typeof
     (JsonStringEnumConverter))]` — matching `PortfolioAssetSummaryItemDTO.Class`'s existing,
     identical convention for the same enum.
   - `CountryAllocationEntryDTO.Country` is `CountryCode`, same converter attribute — matching
     `AssetDetailsDTO.Country`'s existing, identical convention for the same enum.
   - `CurrencyAllocationEntryDTO.Currency` is a plain `string` (the parsed `Currency` enum's
     `.ToString()`, e.g. `"GBP"`), **not** the enum type with a converter — matching
     `PortfolioDashboardDTO.ReportingCurrency`'s own existing precedent of exposing `Currency` as a
     wire string rather than a converted enum.
   - `BrokerAllocationEntryDTO.BrokerName` is a plain `string` — no enum exists for broker identity
     anywhere in this codebase.
   A single generic shape was considered and rejected: it would erase the enum fidelity already
   established for Class/Country elsewhere in this codebase for no benefit, since F05/F06 already
   render each dimension on its own tab regardless of DTO shape (PRD Experience: "Four selectable
   views ... as tabs").
5. **Weight basis and null-exclusion reuse `AssetAmountBases`/`PortfolioAssetSummaryBuilder`
   verbatim, not a re-derivation.** Per-asset `WeightBasis` is computed the identical way
   `PortfolioAssetSummaryBuilder.ComputeAssetData` computes it for its own `PortfolioWeight` field:
   `AssetAmountBases.For(InvestmentScope.Active, AssetTotals.For(asset),
   valuation.MarketValue).WeightBasis`, which collapses to `valuation.MarketValue` for Active scope
   (`AssetAmountBases.For`'s own `weightBasis = scope == Historic ? totals.TotalBought :
   marketValue` branch). An asset whose valuation has no market value therefore has a `null`
   `WeightBasis` and is dropped **before** grouping into any of the four dimensions — never grouped
   with a zero contribution, never appearing as a zero-value slice, satisfying the PRD's explicit
   "excluded from both the numerator and the denominator... never shown as a zero-value slice" rule
   for every dimension at once, because exclusion happens once, upstream of the four groupings.
6. **Percentage formula matches `PortfolioAssetSummaryBuilder.CalculateWeight` exactly**, including
   its divide-by-zero guard: `entryPercentage = dimensionMarketValueTotal == 0m ? 0m :
   groupMarketValueTotal / dimensionMarketValueTotal * 100m`, left as full unrounded `decimal`
   precision (matching every other percentage this codebase computes; rounding for display is a
   frontend/formatter concern, e.g. `formatPercent1`). The guard is required, not merely defensive:
   `WeightBasis` is `null` only for an *unpriced* holding (excluded upstream, per Decision 5); a
   zero-quantity, closed-but-still-Active holding instead gets `MarketValue = 0m` from
   `HoldingValuationCalculator` (a real, non-null value), so its `WeightBasis` is `0m`, not `null` —
   it survives the exclusion and lands in a group. A dimension whose every surviving holding is like
   this has a non-empty entry list with a `dimensionMarketValueTotal` of exactly `0`, which the
   formula above must not divide by. This is a distinct case from the "unpriced holding, never a
   zero-value slice" rule (Decision 5): a flat/closed holding is priced (at `0`), so showing it as a
   `MarketValue: 0, Percentage: 0` entry is correct, not a violation of that rule.
7. **Entries within each dimension are sorted descending by `MarketValue`, tie-broken ascending by
   label (ordinal, case-insensitive).** This is this feature's own choice — `PortfolioAssetSummaryBuilder`
   sorts a potentially long per-broker asset list alphabetically, a different use case (finding a
   named row), whereas a bounded handful of allocation buckets reads naturally largest-slice-first,
   matching how a pie chart's legend is conventionally ordered. No existing backend convention
   dictates this either way, so it is recorded here rather than left to implementation-time
   improvisation.
8. **Currency grouping key reuses the identical `Broker.Currency` string→enum parse F01 already
   established**, `EnumParser.TryParseEnum<Currency>(broker.Currency, ...)` (the same logic
   `PortfolioDashboardService.ParseCurrency` applies), duplicated locally in this feature's own
   holding-collection step rather than extracted into a shared helper (that method is `private
   static` on `PortfolioDashboardService`, and this feature's brief every other new type in this
   PRD wave stays additive-only against sibling features). An unrecognized currency string throws
   `ArgumentException`, the same fail-fast behaviour F01 already has for this exact parse — a
   symptom of a malformed `data-investment.json`, never a normal runtime path.
9. **Active scope only, with no `IsPartial`/unvalued-count field on this DTO.** Unlike F01,
   which surfaces `UnvaluedHoldingCount`/`IsPartial` because its KPI tiles need an inline notice,
   F02's own PRD Experience text has no equivalent notice for the allocation view — an excluded
   unpriced holding is instead counted in F03's `UnpricedOpenHoldings` warning (PRD: "instead
   counted in F03's missing-price warning"), which is that feature's responsibility, not this one's.
   This spec therefore adds no partial-data field to `AllocationBreakdownDTO`.

## 2. Scope

### Included
- New `AllocationBreakdownDTO` (Application DTO) plus four entry-list types
  (`AssetClassAllocationEntryDTO`, `CurrencyAllocationEntryDTO`, `CountryAllocationEntryDTO`,
  `BrokerAllocationEntryDTO`), computed on demand.
- New `IAllocationBreakdownService` / `AllocationBreakdownService` (Application), assembling the DTO
  from `IInvestmentRepository.GetInvestments()` and `IHoldingValuationService`.
- New internal builder: `AllocationBreakdownBuilder` (groups priced Active holdings into the four
  dimensions and computes each entry's `MarketValue`/`Percentage`).
- New `AllocationBreakdownController` (`Financial.Api/Controllers`), one
  `GET /api/v1/financial/allocation-breakdown` endpoint returning `AllocationBreakdownDTO`.
- DI registration of `IAllocationBreakdownService` in `AddFinancialApplication()`.
- OpenAPI snapshot + generated `Financial.Web/src/api/generated/openapi.ts` regeneration (the new
  DTO family and endpoint are a wire-format change).
- Unit tests for the new service/builder; one Integration `ApiEndpointTests`-derived suite covering
  the endpoint and every F02 §9 acceptance criterion.

### Deferred (to their own PRD features, already scheduled)
- Dashboard KPIs — **F01** (merged).
- Data-quality warnings panel — **F03** (merged).
- Upcoming-income projection — **F04**.
- Any UI: the four tabs, the pie/donut chart, the legend table, the Class-default-on-load behaviour
  — **F05** (React) and **F06** (WPF). This spec is backend-only.
- Click-through navigation from an allocation slice to its holdings — explicitly out of scope for
  this wave per the PRD's own F02 Experience section ("This view is read-only in this wave —
  selecting a slice does not navigate elsewhere").

### Out of scope (per PRD §7, applies to this feature too)
- Computing tax due.
- Bond maturity/coupon-schedule modelling.
- CSV or any other export.
- A configurable staleness threshold.
- Automatic remediation of any data-quality finding.
- Click-through navigation from the allocation breakdown (explicit PRD callout for this feature).
- Changes to `AggregatedSummaryTab`/`PortfolioSummaryTab`/`AssetSummaryTab`/`BrokerBreakdownCharts`
  or their backing services/endpoints — F02 adds a new, additive surface only.

## 3. Architecture / Component Overview

### Domain — no changes
No new entity, value object, or `Rules/` calculator. Every input this feature reads (`Asset.Class`,
`Asset.Country`, `Broker.Currency`, `Broker.Name`, `Investments.ActiveBrokers`) and every calculation
primitive it relies on (`HoldingValuationCalculator.Calculate` via `IHoldingValuationService`) already
exists and is already unit-tested at the Domain layer.

### Application

| File | Change |
|---|---|
| `Financial.Investment.Application/DTOs/AllocationBreakdownDTO.cs` | **New.** `AllocationBreakdownDTO` plus the four entry-list record/DTO types (§5). One file, mirroring `DataQualityReportDTOs.cs`'s existing convention of grouping a container DTO with its own small finding/entry types in one file. |
| `Financial.Investment.Application/Interfaces/IAllocationBreakdownService.cs` | **New.** `AllocationBreakdownDTO GetAllocationBreakdown();` |
| `Financial.Investment.Application/Services/AllocationBreakdownService.cs` | **New.** Orchestrates repository read, per-asset valuation/weight-basis, and delegates grouping to the builder; owns span/log wrapper per `docs/rules/implementation.md`. |
| `Financial.Investment.Application/Services/AllocationBreakdownBuilder.cs` | **New**, `internal static`. Groups priced Active holdings into the four dimensions and computes each entry's `MarketValue`/`Percentage` (Decisions 5–7). |
| `Financial.Investment.Application/DependencyInjection/InvestmentApplicationServiceCollectionExtensions.cs` | **Changed.** One new line: `services.AddSingleton<IAllocationBreakdownService, AllocationBreakdownService>();` |

### Infrastructure — no changes
This feature is computed on demand from the already-loaded `IInvestmentRepository` graph; no new
persistence, no new external call, no new repository method.

### Presentation

| File | Change |
|---|---|
| `Financial.Api/Controllers/AllocationBreakdownController.cs` | **New.** `[ApiController] [Route("allocation-breakdown")]`, one `[HttpGet]` action, `GetAllocationBreakdown()`, injecting `IAllocationBreakdownService`, returning `Ok(AllocationBreakdownDTO)`. No route/query parameters — mirrors `DashboardController`'s/`DataQualityReportController`'s shape exactly. |

No `Financial.App` or `Financial.Web` file changes in this feature (F05/F06 consume the endpoint
later).

## 4. API Contracts

### `GET /api/v1/financial/allocation-breakdown`

No request body, no query string, no route parameters.

**200 OK** — `AllocationBreakdownDTO`:

```json
{
  "byClass": [
    { "class": "Equity", "marketValue": 512000.00, "percentage": 63.05 },
    { "class": "RealEstate", "marketValue": 180000.00, "percentage": 22.16 },
    { "class": "Bond", "marketValue": 88345.67, "percentage": 10.87 },
    { "class": "Unknown", "marketValue": 32000.00, "percentage": 3.94 }
  ],
  "byCurrency": [
    { "currency": "GBP", "marketValue": 430000.00, "percentage": 52.93 },
    { "currency": "BRL", "marketValue": 382345.67, "percentage": 47.07 }
  ],
  "byCountry": [
    { "country": "UK", "marketValue": 430000.00, "percentage": 52.93 },
    { "country": "BR", "marketValue": 350000.00, "percentage": 43.08 },
    { "country": "Unknown", "marketValue": 32345.67, "percentage": 3.98 }
  ],
  "byBroker": [
    { "brokerName": "Trading212", "marketValue": 430000.00, "percentage": 52.93 },
    { "brokerName": "XPI", "marketValue": 382345.67, "percentage": 47.07 }
  ]
}
```

**No priced Active holdings example** (all four lists empty — never an error):

```json
{
  "byClass": [],
  "byCurrency": [],
  "byCountry": [],
  "byBroker": []
}
```

There is no error/400 case: the endpoint takes no input to validate. An unexpected exception falls
through to `DomainExceptionMappingMiddleware`/the default ASP.NET Core problem-details handler,
exactly as every other read endpoint in this API (same precedent F01 §4 and F03 §4 already
recorded).

## 5. Data Model

**N/A — nothing persists.** `AllocationBreakdownDTO` is computed fresh from the in-memory
`Investments` graph on every call to `GetAllocationBreakdown()`; there is no new field on
`data-investment.json`, no schema/migration concern, and no caching between requests (consistent
with every other Summary-family endpoint in this codebase, and with F01 §5/F03 §5's identical
framing).

New Application shapes (not a database schema — listed here per this project's Data Model section
convention for computed DTOs):

**`AllocationBreakdownDTO`** (new)

| Field | Type | Description |
|---|---|---|
| `ByClass` | `IReadOnlyList<AssetClassAllocationEntryDTO>` | Class-dimension entries. |
| `ByCurrency` | `IReadOnlyList<CurrencyAllocationEntryDTO>` | Currency-dimension entries. |
| `ByCountry` | `IReadOnlyList<CountryAllocationEntryDTO>` | Country-dimension entries. |
| `ByBroker` | `IReadOnlyList<BrokerAllocationEntryDTO>` | Broker-dimension entries. |

**`AssetClassAllocationEntryDTO`** (new)

| Field | Type | Description |
|---|---|---|
| `Class` | `GlobalAssetClass` (`[JsonConverter(JsonStringEnumConverter)]`) | Grouping key; `Unknown` is a real, expected value. |
| `MarketValue` | `decimal` | Sum of `WeightBasis` (§1 Decision 5) over every priced Active holding of this class. |
| `Percentage` | `decimal` | `MarketValue / ByClass total * 100`, unrounded. |

**`CurrencyAllocationEntryDTO`** (new)

| Field | Type | Description |
|---|---|---|
| `Currency` | `string` | Owning broker's currency, e.g. `"GBP"`. Never `"Unknown"` — every asset has a real broker. |
| `MarketValue` | `decimal` | Same weight-basis sum, grouped by broker currency. |
| `Percentage` | `decimal` | `MarketValue / ByCurrency total * 100`, unrounded. |

**`CountryAllocationEntryDTO`** (new)

| Field | Type | Description |
|---|---|---|
| `Country` | `CountryCode` (`[JsonConverter(JsonStringEnumConverter)]`) | Grouping key; `Unknown` is a real, expected value. |
| `MarketValue` | `decimal` | Same weight-basis sum, grouped by `Asset.Country`. |
| `Percentage` | `decimal` | `MarketValue / ByCountry total * 100`, unrounded. |

**`BrokerAllocationEntryDTO`** (new)

| Field | Type | Description |
|---|---|---|
| `BrokerName` | `string` | Owning broker's name. Never `"Unknown"` — every asset has a real broker. |
| `MarketValue` | `decimal` | Same weight-basis sum, grouped by `Broker.Name`. |
| `Percentage` | `decimal` | `MarketValue / ByBroker total * 100`, unrounded. |

## 6. Requirements / Business Rules

All rules below operate over `activeAssets = investments.ActiveBrokers.SelectMany(b =>
b.Portfolios.SelectMany(p => p.Assets))`, where each asset is paired with its owning broker's `Name`
and `Currency` while iterating (mirroring `PortfolioDashboardService.CollectHoldings`/`AddHoldings`'s
own broker-then-portfolio-then-asset walk, since `Asset` itself carries no back-reference to its
owning `Broker`).

1. **Weight basis and exclusion** — for each asset in `activeAssets`, call
   `holdingValuationService.GetValuation(asset, InvestmentScope.Active)`, then compute
   `weightBasis = AssetAmountBases.For(InvestmentScope.Active, AssetTotals.For(asset),
   valuation.MarketValue).WeightBasis` (§1 Decision 5). When `weightBasis is null` (no price), the
   asset is dropped before any of the four groupings below — it never contributes to any dimension's
   numerator or denominator, and never appears as a zero-value entry in any list.
2. **Grouping keys** — for every asset that survives rule 1:
   - Class dimension groups by `asset.Class` (`GlobalAssetClass`, `Unknown` included as a real
     value).
   - Currency dimension groups by the owning broker's `Currency` (parsed via
     `EnumParser.TryParseEnum<Currency>`, §1 Decision 8; throws `ArgumentException` on an
     unrecognized value, the same fail-fast behaviour F01 already has for this parse).
   - Country dimension groups by `asset.Country` (`CountryCode`, `Unknown` included as a real
     value).
   - Broker dimension groups by the owning broker's `Name`.
3. **Per-group `MarketValue`** — the sum of `weightBasis` (rule 1) over every asset in that group.
4. **Per-entry `Percentage`** — `dimensionTotalMarketValue == 0m ? 0m : groupMarketValue /
   dimensionTotalMarketValue * 100m`, where `dimensionTotalMarketValue` is the sum of every group's
   `MarketValue` within that same dimension (equivalently, the sum of `weightBasis` over every asset
   that survived rule 1, since every surviving asset belongs to exactly one group per dimension).
   Unrounded `decimal`. When `dimensionTotalMarketValue` is nonzero, each dimension's entries'
   percentages sum to exactly 100% of that dimension's own total by construction — guaranteed by
   every surviving asset landing in exactly one group.
5. **Sort order** — within each dimension's list, entries are ordered descending by `MarketValue`,
   ties broken ascending by the entry's label (`Class`/`Currency`/`Country`/`BrokerName`, ordinal,
   case-insensitive) — §1 Decision 7.
6. **Zero-total edge case** — a dimension's `dimensionTotalMarketValue` can be exactly `0` even with
   a non-empty entry list: `weightBasis` is `0m` (not excluded) for a zero-quantity, closed-but-
   still-Active holding, so a group made entirely of such holdings has `MarketValue = 0`. Rule 4's
   guard returns `Percentage = 0` for every entry in that case rather than dividing by zero. When no
   Active holding has a price at all (rule 1 excludes every asset), the list itself is empty instead.
7. **No cross-dimension coupling** — the four dimensions are computed independently from the same
   underlying `(asset, weightBasis)` pairs; a change to one dimension's grouping (e.g. a broker
   rename) never affects another dimension's totals or percentages.
8. All monetary sums use `decimal` throughout (never `double`), matching every existing DTO in this
   context; `Percentage` is non-nullable `decimal` (an entry only ever exists when its `MarketValue`
   is a real, positive-or-zero contribution — never a placeholder for a missing figure).

## 7. Testing Strategy

Per `testing-guide-Financial`: Unit for the new service/builder's branches + failed-span/log
assertions, Integration (`ApiEndpointTests`-derived) for the wired endpoint and the mandatory
AC-tracing subset. This PRD (P52) already has AC ids from F01 (`P52-F01-dashboard-aggregate-01..04`)
and F03 (`P52-F03-data-quality-warnings-01..04`); per `references/feature-traceability.md`, this
feature adds ids to §9's F02 group the first time an AC-tracing test is written for it — plan.md's
final phase includes this as an explicit step (`P52-F02-allocation-breakdown-01..03`, in the order
the three bullets already appear in §9).

### Unit — `Tests/Financial.Investment.Application.Tests/Services/`

- `AllocationBreakdownServiceTests.cs` (pattern: `PortfolioAssetSummaryServiceTests.cs`/
  `PortfolioDashboardServiceTests.cs` — `StubInvestmentRepository` with `Investments` set,
  `TestHoldingValuationService`, `RecordingTelemetryTracer`, `RecordingLogger<AllocationBreakdownService>`):
  - Constructor null-guard tests, one per dependency (`repository`, `holdingValuationService`,
    `tracer`, `logger`), mirroring `PortfolioAssetSummaryServiceTests`' pattern.
  - `GetAllocationBreakdown_ByClass_GroupsAssetsByGlobalAssetClass` — two Active, priced holdings of
    different `GlobalAssetClass` values land in two separate `ByClass` entries.
  - `GetAllocationBreakdown_ByClass_UnknownClassHolding_AppearsAsUnknownEntry` — the AC #3 proof for
    Class.
  - `GetAllocationBreakdown_ByCountry_UnknownCountryHolding_AppearsAsUnknownEntry` — the AC #3 proof
    for Country.
  - `GetAllocationBreakdown_ByCurrency_GroupsAssetsByOwningBrokerCurrency` — two brokers in
    different currencies produce two `ByCurrency` entries.
  - `GetAllocationBreakdown_ByBroker_GroupsAssetsByOwningBrokerName`.
  - `GetAllocationBreakdown_UnpricedActiveHolding_ExcludedFromEveryDimension` — the AC #2 proof: one
    unpriced Active holding among priced ones never appears (not even as a zero-value entry) in any
    of the four lists, and every dimension's percentages still sum to exactly 100 over the remaining
    priced holdings only.
  - `GetAllocationBreakdown_HistoricHolding_ExcludedFromEveryDimension` — Active-scope-only proof,
    mirroring F01's own `SumsMarketValueAcrossActiveBrokersOnly` test shape.
  - `GetAllocationBreakdown_EachDimension_PercentagesSumToExactlyOneHundred` — the AC #1 proof,
    asserted independently for all four dimensions with a fixture whose holdings span more than two
    groups per dimension (so the assertion isn't trivially true for a single-entry list).
  - `GetAllocationBreakdown_EntriesSortedDescendingByMarketValue` — three groups of decreasing size
    render in that exact order.
  - `GetAllocationBreakdown_NoActiveHoldingsPriced_ReturnsEmptyListsForAllFourDimensions`.
  - `GetAllocationBreakdown_UnrecognizedBrokerCurrency_Throws` — the Decision 8 fail-fast proof
    (mirrors `PortfolioDashboardServiceTests`' equivalent malformed-currency coverage, if present, or
    else a new fixture with a broker `Currency` value outside the `Currency` enum's members).
  - `Constructor_WithNullRepository_RecordsFailedSpanOnGetAllocationBreakdownFailure` (or
    equivalent negative-path test using `StubInvestmentRepository.ThrowOnGetInvestments` or the
    exact throwing member `AllocationBreakdownService` calls — whichever `StubInvestmentRepository`
    already exposes — asserting `RecordingTelemetryTracer`/`RecordingLogger<AllocationBreakdownService>`
    records the exception type, per `references/negative-path-testing.md`).

- `AllocationBreakdownBuilderTests.cs` (if the builder's branching is rich enough to warrant its own
  file separate from the service test above, following `PortfolioXirrBuilderTests.cs`'s precedent
  from F01 of giving an `internal static` builder its own focused test file; otherwise the service
  tests above are sufficient and this file is skipped — decide at implementation time based on
  whether `AllocationBreakdownBuilder`'s grouping/sorting/percentage logic is reachable only through
  the service, matching how `PortfolioAssetSummaryBuilder` itself has no dedicated test file today,
  only `PortfolioAssetSummaryServiceTests.cs`).

### Integration — `Tests/Financial.Api.Tests/`

- `Controllers/AllocationBreakdownControllerTests.cs` (pattern: `DashboardControllerTests`/
  `DataQualityReportControllerTests` guard-clause-only convention): `GetAllocationBreakdown_ReturnsOk`.
- `Acceptance/AllocationBreakdownAcceptanceTests.cs` — one `[Fact]` per F02 §9 bullet, each tagged
  `[Trait("AC", "P52-F02-allocation-breakdown-0N")]`:
  - `-01` — seed Active holdings spanning at least 3 distinct `GlobalAssetClass` values with known
    market values; assert each of the 4 dimensions' `Percentage` values sum to exactly 100 (within
    decimal precision) independent of the fixture's absolute totals.
  - `-02` — seed one Active holding with no price alongside priced ones; assert it appears in none
    of the 4 dimensions' entries, and that the priced holdings' percentages still sum to exactly 100
    over themselves.
  - `-03` — seed one Active holding with `GlobalAssetClass.Unknown` and one with
    `CountryCode.Unknown`; assert an `Unknown` entry is present in `ByClass` and in `ByCountry`
    respectively, with a nonzero `MarketValue`.
- `Contract/OpenApiContractTests.cs` — no new test method; the existing
  `OpenApiDocument_NumericProperties_...` and snapshot-diff tests automatically cover the new
  endpoint/DTO family once the snapshot is regenerated (plan.md's final phase).

### Not covered here (other features' responsibility)
- F01/F03/F04's own endpoints and DTOs.
- Any React/WPF rendering of these figures, including the four tabs, the chart, and the legend table
  (F05/F06).
