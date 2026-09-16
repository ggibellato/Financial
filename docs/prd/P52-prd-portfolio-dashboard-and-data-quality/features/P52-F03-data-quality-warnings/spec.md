# P52-F03 — Data-Quality Warnings — Technical Spec

## 1. Overview

F03 gives P46-F07's existing data-quality report — today reachable only by running the
`Tools/InvestmentDataQualityReport` console tool by hand — its first API surface, and extends it
with the 2 finding categories and 1 rollup count the dashboard's warnings panel needs. It extends
the existing `IDataQualityReportService.GenerateReport()` / `DataQualityReportDTO` in place rather
than introducing a parallel surface: the PRD's own Capabilities text for F03 says the new endpoint
returns "the existing `DataQualityReportDTO` plus 2 new finding lists," so the 3 pre-existing
finding lists (`SalesExceedPurchases`, `UnpricedOpenHoldings`, and the two P46-F07 fields not
mapped to any of the dashboard's 5 categories, `UnclassifiedHoldings`/
`UnclassifiedAndUnpricedOpenHoldings`/`HistoricHoldingsStillOpen`) keep working unchanged for the
console tool while the dashboard consumes the same DTO through a new endpoint.

Like F01, this is a pure aggregation of figures the backend already knows how to compute per
holding — `HoldingValuationService.GetValuation` (for market status and the market-value half of
the missing-cost-basis check), `AssetAmountBases`/`AssetTotals` (for the invested-amount half), and
`Asset.TaxClassifications` (already the exact collection `TaxWorkbookService` enumerates for the
tax workbook) — into 2 new per-holding finding lists plus 1 new portfolio-wide count. Confirmed by
research: no Domain change and no Infrastructure change are needed. Every input is already reachable
through `IInvestmentRepository.GetInvestments()` (the same aggregate `DataQualityReportService`
already reads) and the existing `IHoldingValuationService` Application interface, which is already
registered in `AddFinancialApplication()` — adding it as a new constructor dependency to
`DataQualityReportService` needs no new DI registration line, only the extra constructor parameter.
This feature adds 2 new Application DTOs (finding records), extends 1 existing Application DTO and
1 existing Application service, adds 1 new Api controller endpoint, and makes a 2-line compile-only
fix to the console tool that manually constructs `DataQualityReportService` outside the DI
container.

### Decisions / Assumptions

Recorded here because the PRD does not spell these out and no user was available to ask.

1. **Route path.** Following the same observed convention F01's spec already recorded (every
   controller in `Financial.Api/Controllers` uses a flat kebab-case segment directly under the
   `api/v{version}/financial` group — `[Route("dashboard")]`, `[Route("tax-workbook")]`,
   `[Route("tax-rules")]`, never nested under `investment/`), this spec uses
   `[Route("data-quality-report")]` on a new `DataQualityReportController`, giving
   `GET /api/v1/financial/data-quality-report` — not the PRD's own example URL
   (`GET /api/v1/financial/investment/data-quality-report`), which does not match the codebase's
   actual convention, exactly as F01 flagged this feature would need to do.
2. **Extend `DataQualityReportService`/`DataQualityReportDTO` directly; no new sibling service.**
   The PRD's own Capabilities wording ("Exposes `IDataQualityReportService.GenerateReport()` ...
   returning the existing `DataQualityReportDTO` plus 2 new finding lists") already answers this:
   the 2 new finding lists and the 1 new rollup count are new fields on the existing DTO, computed
   inside the existing service's existing `GenerateReport()` method, alongside the existing 5
   findings it already computes in one pass over the same flattened holding list. A new sibling
   service would duplicate the `Flatten` walk over `ActiveBrokers`/`HistoricBrokers` for no benefit.
3. **`IHoldingValuationService` is added as a new constructor dependency of
   `DataQualityReportService`.** Needed for both new capabilities: the stale-valuation rollup reads
   `HoldingValuation.MarketStatus`, and the missing-cost-basis finding reads
   `HoldingValuation.MarketValue`. It is already registered as
   `services.AddSingleton<IHoldingValuationService, HoldingValuationService>()` in
   `AddFinancialApplication()` (registered before `IDataQualityReportService` in that file), so the
   API's DI container resolves the new constructor parameter with no new registration line. The one
   place that is *not* DI-resolved is `Tools/InvestmentDataQualityReport/Program.cs`, which `new`s
   up `DataQualityReportService` by hand; it needs a 2-line addition
   (`new HoldingValuationService(new XirrCalculationService(), NoOpTelemetryTracer.Instance,
   NullLogger<HoldingValuationService>.Instance)`, mirroring how it already constructs
   `DataQualityReportService`'s other dependencies) to keep compiling. `XirrCalculationService` has
   a parameterless constructor (it is a pure, deterministic pass-through to `XirrCalculator`,
   explicitly exempt from the span/log wrapper per its own doc comment), so this adds no further
   transitive dependency.
4. **Stale valuation is a portfolio-wide count only (`StaleValuationCount`), not a finding list.**
   The PRD's own Capabilities text is explicit that this category maps to "a new rollup count of
   every Active holding whose `MarketStatus` (P48) is `Stale` ... just counted across the whole
   portfolio" — language it uses nowhere else among the 5 categories, all of which otherwise map to
   a named finding list. Research task 3 in this feature's brief independently describes the same
   thing as "a portfolio-wide rollup count," confirming the PRD's wording is deliberate, not a slip.
   Consequence: F03's Experience bullet "clicking a listed holding navigates to that holding's node"
   only applies to the 4 categories that carry per-holding identity in this backend response
   (`SalesExceedPurchases`, `UnpricedOpenHoldings`, `OpenHoldingsMissingCostBasis`,
   `UnresolvedTaxClassifications`); the stale-valuation tile in F05/F06 renders a count with no
   per-holding expand/click-through in this wave. If a future feature wants stale-valuation
   click-through, it is a new PRD decision (the count-only shape is this PRD's own explicit choice,
   not an oversight this spec should silently "fix" by inventing a list the PRD never asked for).
5. **`OpenHoldingsMissingCostBasis`'s "0 or null" collapses to `== 0m`.** `AssetAmountBases.
   InvestedAmount` (via `AssetTotals.OpenPositionCost`) is a non-nullable `decimal` at this
   codebase's Domain/Application layers — there is no domain representation of a "null" cost basis,
   only ever a computed decimal that may land on exactly `0m` (e.g., a fully broker-valued position
   with no purchase transactions recorded). The PRD's "0 or null" is therefore implemented as a
   single check, `InvestedAmount == 0m`, matching how every other decimal total in this codebase is
   modelled (Decision 9 of F01's own spec makes the identical point about `decimal` vs. `decimal?`
   for money totals).
6. **`OpenHoldingsMissingCostBasis` requires a nonzero, non-null market value, which already
   excludes every already-unpriced holding.** The PRD's own finding definition ("despite a nonzero
   market value") makes this exclusion explicit: an Active holding with `MarketValue is null` is
   already reported under `UnpricedOpenHoldings` and is never double-reported here, with no extra
   "linking" logic needed (unlike the existing `UnclassifiedAndUnpricedOpenHoldings` link, which
   this feature does not touch or extend).
7. **`UnresolvedTaxClassifications` is sourced from `Asset.TaxClassifications` directly, the same
   collection `TaxWorkbookService` already enumerates for the tax workbook** (`AllAssets(investments)
   .SelectMany(asset => asset.TaxClassifications)`), filtered to `Status == TaxClassificationStatus
   .Active` (a `Superseded` classification is never a live gap to resolve — same supersede-never-
   rewrite precedent F01 Decision 4 already established for `DisposalRecord`) and
   `CalculationStatus is CalculationStatus.Incomplete or CalculationStatus.RequiresReview`. One
   finding per matching `TaxClassification`, not one per asset — an asset can carry more than one
   unresolved classification across tax years or event categories, and the PRD's field list
   (`TaxYear`, `EventCategory`) only makes sense per-classification.
8. **DTO field order reflects the PRD's own severity order for the 5 dashboard categories, with
   the 3 pre-existing non-dashboard fields kept afterward.** The PRD's F03 Experience section
   orders categories "impossible cash-flow sequence, missing price, missing cost basis, stale
   valuation, unresolved tax classification." JSON object key order carries no semantic meaning to
   any client this codebase has, but keeping the DTO's declared property order consistent with the
   PRD's own stated order costs nothing and matches how a reader (or a future diff) would expect to
   scan it: `SalesExceedPurchases`, `UnpricedOpenHoldings`, `OpenHoldingsMissingCostBasis`,
   `StaleValuationCount`, `UnresolvedTaxClassifications`, followed unchanged by
   `UnclassifiedHoldings`, `HistoricHoldingsStillOpen`, `UnclassifiedAndUnpricedOpenHoldings` (the
   3 P46-F07 fields no dashboard category maps to).
9. **"A category with 0 findings is hidden" is a frontend (F05/F06) rendering rule, not a backend
   response-shaping rule.** The endpoint always returns all 5 dashboard-relevant fields
   unconditionally — empty lists (`[]`) where nothing is found, `StaleValuationCount = 0` when no
   holding is stale — so F05/F06 can each apply their own hide-if-zero rendering independently and
   so the endpoint's response shape never depends on the data it happens to contain (consistent
   with every other list-returning endpoint in this codebase, and with `DataQualityReportService`'s
   existing behaviour for its other 5 fields today).
10. **The console tool's text formatter is extended, not left silently stale.** CLAUDE.md's
    "vertical slices only" invariant (no disconnected infrastructure) argues against leaving
    `DataQualityReportFormatter.Format` printing a `DataQualityReportDTO` that has silently grown 3
    fields it never mentions. `DataQualityReportFormatter.cs` gets 2 more `Append*` sections (one
    per new finding list) plus one printed line for `StaleValuationCount`, following its own
    existing per-section format exactly. This is a small, mechanical, same-shape addition — not a
    redesign of the tool's output — so it stays in this feature's scope rather than being deferred.

## 2. Scope

### Included
- 2 new Application DTOs (finding records): `OpenHoldingMissingCostBasisFinding`,
  `UnresolvedTaxClassificationFinding`.
- Extension of the existing `DataQualityReportDTO` with 3 new fields:
  `OpenHoldingsMissingCostBasis`, `StaleValuationCount`, `UnresolvedTaxClassifications`.
- Extension of the existing `DataQualityReportService.GenerateReport()` to compute the 3 new
  fields, using the same already-flattened holding list it computes the existing 5 fields from.
- `IHoldingValuationService` added as a new constructor dependency of `DataQualityReportService`
  (no new DI registration needed — already registered in `AddFinancialApplication()`).
- New `DataQualityReportController` (`Financial.Api/Controllers`), one
  `GET /api/v1/financial/data-quality-report` endpoint returning `DataQualityReportDTO`.
- Compile-only fix to `Tools/InvestmentDataQualityReport/Program.cs` (construct and pass the new
  `IHoldingValuationService` dependency) and an output-format extension to
  `Tools/InvestmentDataQualityReport/DataQualityReportFormatter.cs` (print the 3 new fields).
- OpenAPI snapshot + generated `Financial.Web/src/api/generated/openapi.ts` regeneration (the DTO
  shape change and new endpoint are a wire-format change).
- Unit tests extending `DataQualityReportServiceTests.cs`; one Integration `ApiEndpointTests`-
  derived suite covering the new endpoint and every F03 §9 acceptance criterion (this feature's own
  bullets, plus this feature's share of the Cross-Feature Integration group is F05/F06's concern,
  not this backend feature's).

### Deferred (to their own PRD features, already scheduled)
- The warnings panel UI, expand/collapse control, and per-category ordering on screen — **F05**
  (React) and **F06** (WPF).
- Click-through navigation from a listed holding to its tree node — **F05**/**F06**; this spec only
  ensures the 4 click-through-eligible categories carry `BrokerName`/`PortfolioName`/`AssetName`
  (or the finding-specific equivalent) so a frontend can resolve a tree node later. No navigation
  logic exists in this feature.
- Dashboard KPIs (F01), allocation breakdown (F02), upcoming income (F04) — separate features in
  this PRD, untouched here.

### Out of scope (per PRD §7, applies to this feature too)
- Computing tax due — unresolved-classification counts point at the existing tax workbook; no tax
  figure is computed or displayed here.
- A configurable staleness threshold — `StaleValuationCount` reuses `MarketStatusCalculator`'s
  existing, unmodified rule.
- Automatic remediation of any finding (auto-price, auto-classify, auto-correct).
- Click-through navigation from the allocation breakdown (F02's own scope, untouched).
- Redesigning `Tools/InvestmentDataQualityReport`'s output format beyond appending the 3 new
  fields in the tool's own existing per-section style (Decision 10).

## 3. Architecture / Component Overview

### Domain — no changes
No new entity, value object, or `Rules/` calculator. Every input this feature reads
(`Asset.TaxClassifications`, `TaxClassification.Status`/`CalculationStatus`/`TaxYear`/
`EventCategory`, `HoldingValuation.MarketValue`/`MarketStatus`) and every calculation primitive it
relies on (`HoldingValuationCalculator.Calculate`, `MarketStatusCalculator.For`,
`OpenPositionCostCalculator.CostOfUnitsHeld`) already exists and is already unit-tested at the
Domain layer.

### Application

| File | Change |
|---|---|
| `Financial.Investment.Application/DTOs/DataQualityReportDTOs.cs` | **Changed.** Adds `OpenHoldingMissingCostBasisFinding`, `UnresolvedTaxClassificationFinding` records, and 3 new properties on `DataQualityReportDTO` (§5). |
| `Financial.Investment.Application/Services/DataQualityReportService.cs` | **Changed.** New constructor parameter `IHoldingValuationService`; `GenerateReport()` computes the 3 new fields from the same `allHoldings` list it already builds. |
| `Financial.Investment.Application/DependencyInjection/InvestmentApplicationServiceCollectionExtensions.cs` | **No change.** `IDataQualityReportService` and `IHoldingValuationService` are both already registered; only their construction order matters, and DI resolves that automatically. |

### Infrastructure — no changes
This feature is computed on demand from the already-loaded `IInvestmentRepository` graph, the same
way `DataQualityReportService` already operates today; no new persistence, no new external call, no
new repository method.

### Presentation

| File | Change |
|---|---|
| `Financial.Api/Controllers/DataQualityReportController.cs` | **New.** `[ApiController] [Route("data-quality-report")]`, one `[HttpGet]` action, injecting `IDataQualityReportService`, returning `Ok(DataQualityReportDTO)`. No route/query parameters — mirrors `DashboardController`'s shape exactly. |

### Tools (console tool, not a Presentation layer)

| File | Change |
|---|---|
| `Tools/InvestmentDataQualityReport/Program.cs` | **Changed.** Constructs `HoldingValuationService` (with a fresh `XirrCalculationService`, `NoOpTelemetryTracer.Instance`, `NullLogger<HoldingValuationService>.Instance`) and passes it as `DataQualityReportService`'s new 4th constructor argument. |
| `Tools/InvestmentDataQualityReport/DataQualityReportFormatter.cs` | **Changed.** 2 new `Append*` private methods (`AppendOpenHoldingsMissingCostBasis`, `AppendUnresolvedTaxClassifications`) following the file's own existing per-section format, plus 1 new printed line for `StaleValuationCount`; all 3 wired into `Format`. |

No `Financial.App` or `Financial.Web` file changes in this feature (F05/F06 consume the endpoint
later).

## 4. API Contracts

### `GET /api/v1/financial/data-quality-report`

No request body, no query string, no route parameters.

**200 OK** — `DataQualityReportDTO`:

```json
{
  "salesExceedPurchases": [
    {
      "brokerName": "XPI",
      "portfolioName": "Default",
      "assetName": "OVERSOLD",
      "offendingSaleDate": "2021-06-01T00:00:00",
      "quantityHeld": 5,
      "shortfall": 3
    }
  ],
  "unpricedOpenHoldings": [
    { "brokerName": "XPI", "portfolioName": "Default", "assetName": "UNPRICED" }
  ],
  "openHoldingsMissingCostBasis": [
    { "brokerName": "Avenue", "portfolioName": "Default", "assetName": "PROVIDERVALUED" }
  ],
  "staleValuationCount": 4,
  "unresolvedTaxClassifications": [
    {
      "brokerName": "XPI",
      "portfolioName": "Default",
      "assetName": "OVERSOLD",
      "taxYear": "2024",
      "eventCategory": "CapitalGain"
    }
  ],
  "unclassifiedHoldings": [],
  "historicHoldingsStillOpen": [],
  "unclassifiedAndUnpricedOpenHoldings": []
}
```

**Clean-portfolio example** (every category empty/zero — the endpoint still always returns the
full field set unconditionally; §6 Decision 9's hide-if-zero rule is a frontend concern, not
reflected here):

```json
{
  "salesExceedPurchases": [],
  "unpricedOpenHoldings": [],
  "openHoldingsMissingCostBasis": [],
  "staleValuationCount": 0,
  "unresolvedTaxClassifications": [],
  "unclassifiedHoldings": [],
  "historicHoldingsStillOpen": [],
  "unclassifiedAndUnpricedOpenHoldings": []
}
```

There is no error/400 case: the endpoint takes no input to validate. An unexpected exception falls
through to `DomainExceptionMappingMiddleware`/the default ASP.NET Core problem-details handler,
exactly as every other read endpoint in this API (same precedent F01 §4 already recorded for
`GET /api/v1/financial/dashboard`).

## 5. Data Model

**N/A — nothing persists.** `DataQualityReportDTO` is computed fresh from the in-memory
`Investments` graph on every call to `GenerateReport()`, exactly as it already is today; this
feature adds fields to that computation, not a new field on `data-investment.json`, no schema/
migration concern, and no caching between requests.

New/changed Application shapes (not a database schema — listed here per this project's Data Model
section convention for computed DTOs, matching F01 §5's own "nothing persists" framing):

**`OpenHoldingMissingCostBasisFinding`** (new record)

| Field | Type | Description |
|---|---|---|
| `BrokerName` | `string` | Owning broker's name. |
| `PortfolioName` | `string` | Owning portfolio's name. |
| `AssetName` | `string` | Affected asset's name. |

**`UnresolvedTaxClassificationFinding`** (new record)

| Field | Type | Description |
|---|---|---|
| `BrokerName` | `string` | Owning broker's name. |
| `PortfolioName` | `string` | Owning portfolio's name. |
| `AssetName` | `string` | Asset the classification belongs to. |
| `TaxYear` | `string` | `TaxClassification.TaxYear` verbatim (e.g. `"2024"`). |
| `EventCategory` | `EventCategory` (`Financial.Investment.Domain.Entities`) | `TaxClassification.EventCategory` verbatim — same enum `TaxWorkbookEntryDTO.EventCategory` already exposes. |

**`DataQualityReportDTO`** (extended — new members only; existing 5 members unchanged)

| Field | Type | Description |
|---|---|---|
| `OpenHoldingsMissingCostBasis` | `IReadOnlyList<OpenHoldingMissingCostBasisFinding>` | New. |
| `StaleValuationCount` | `int` | New. Count only, no per-holding identity (Decision 4). |
| `UnresolvedTaxClassifications` | `IReadOnlyList<UnresolvedTaxClassificationFinding>` | New. |

## 6. Requirements / Business Rules

All rules below operate over the same `allHoldings` list `DataQualityReportService.GenerateReport()`
already builds today (`Flatten(investments.ActiveBrokers, InvestmentScope.Active).Concat(Flatten
(investments.HistoricBrokers, InvestmentScope.Historic))`), plus `activeHoldings` (the
`InvestmentScope.Active`-only subset already computed as a local for the existing
`UnpricedOpenHoldings`/`UnclassifiedAndUnpricedOpenHoldings` rules).

1. **`OpenHoldingsMissingCostBasis`** — for each holding in `activeHoldings`, call
   `holdingValuationService.GetValuation(h.Asset, InvestmentScope.Active)`. Include the holding
   when `AssetAmountBases.For(InvestmentScope.Active, AssetTotals.For(h.Asset)).InvestedAmount ==
   0m` **and** `valuation.MarketValue is decimal marketValue && marketValue != 0m` (Decisions 5–6).
   Sort ascending by `BrokerName`, then `PortfolioName`, then `AssetName`, matching every other
   finding list's existing sort convention in this file.
2. **`StaleValuationCount`** — `activeHoldings.Count(h => holdingValuationService.GetValuation
   (h.Asset, InvestmentScope.Active).MarketStatus == MarketStatus.Stale)`. A Historic holding is
   never counted (`HoldingValuationCalculator.NotMarkedToMarket` always returns
   `MarketStatus.Current` for Historic holdings, so the exclusion is structural, not an extra
   filter this feature adds).
3. **`UnresolvedTaxClassifications`** — for each holding in `allHoldings` (both scopes — an
   unresolved classification on a Historic holding's past disposal or credit is still a real gap in
   the tax workbook, the same "lifetime fact independent of open/closed" reasoning F01 Decision 5
   already applied to income), for each `classification` in `h.Asset.TaxClassifications` where
   `classification.Status == TaxClassificationStatus.Active` and `classification.CalculationStatus
   is CalculationStatus.Incomplete or CalculationStatus.RequiresReview` (Decision 7), emit one
   `UnresolvedTaxClassificationFinding(h.BrokerName, h.PortfolioName, h.Asset.Name,
   classification.TaxYear, classification.EventCategory)`. Sort ascending by `BrokerName`, then
   `PortfolioName`, then `AssetName`, then `TaxYear` (ordinal).
4. **Field order matches the PRD's severity order for the 5 dashboard categories** (Decision 8):
   `SalesExceedPurchases`, `UnpricedOpenHoldings`, `OpenHoldingsMissingCostBasis`,
   `StaleValuationCount`, `UnresolvedTaxClassifications`, then the 3 unchanged non-dashboard fields.
5. **The response always includes all 8 fields unconditionally** — empty lists (never `null`) and
   `StaleValuationCount = 0` on a fully clean portfolio (Decision 9); hiding a zero-count category
   is F05/F06's own rendering rule, not applied here.
6. **No change to any existing field's computation.** `SalesExceedPurchases`,
   `UnpricedOpenHoldings`, `UnclassifiedHoldings`, `HistoricHoldingsStillOpen`,
   `UnclassifiedAndUnpricedOpenHoldings` keep their exact existing logic, inputs and sort order —
   this feature is additive only, verified by `DataQualityReportServiceTests`' existing test
   methods continuing to pass unmodified (§7).

## 7. Testing Strategy

Per `testing-guide-Financial`: Unit for the service's new branches + the new constructor
dependency's null guard, Integration (`ApiEndpointTests`-derived) for the wired endpoint and the
mandatory AC-tracing subset. This PRD (P52) already has AC ids from F01
(`P52-F01-dashboard-aggregate-01..04`); per `references/feature-traceability.md`, this feature adds
ids to §9's F03 group the first time an AC-tracing test is written for it — plan.md's final phase
includes this as an explicit step (`P52-F03-data-quality-warnings-01..04`, in the order the four
bullets already appear in §9).

### Unit — `Tests/Financial.Investment.Application.Tests/Services/`

- `DataQualityReportServiceTests.cs` (existing file, extended — pattern already established in the
  file: `StubInvestmentRepository`, `RecordingTelemetryTracer`, `RecordingLogger<DataQualityReportService>`,
  `SeedActive`/`SeedHistoric`/`SeedHistoricInto` helpers; `TestHoldingValuationService`
  from `Tests/Financial.TestUtilities` added as the new dependency):
  - `Constructor_WithNullHoldingValuationService_Throws` (mirrors the multi-dependency
    null-guard pattern `SummaryServiceTests`/`PortfolioDashboardServiceTests` already use for their
    own newer dependencies).
  - `GenerateReport_OpenHoldingWithZeroInvestedAmountAndNonzeroMarketValue_IsNamed` — a
    provider-valued holding with no purchase transaction, priced nonzero, appears in
    `OpenHoldingsMissingCostBasis`.
  - `GenerateReport_OpenHoldingWithCostBasisAndPrice_NotReportedAsMissingCostBasis` — the normal
    case (nonzero invested amount) is never reported.
  - `GenerateReport_UnpricedOpenHolding_NeverAlsoReportedAsMissingCostBasis` — Decision 6's
    exclusion: a holding already in `UnpricedOpenHoldings` (null market value) never also appears
    in `OpenHoldingsMissingCostBasis`.
  - `GenerateReport_HistoricHoldingMissingCostBasis_NeverReported` — the rule is Active-scope only.
  - `GenerateReport_StaleValuationCount_CountsOnlyActiveHoldingsWithStaleMarketStatus` (using
    `TestHoldingValuationService`'s configurable `MarketStatus` per asset).
  - `GenerateReport_StaleValuationCount_ExcludesHistoricHoldings`.
  - `GenerateReport_StaleValuationCount_ZeroWhenNoHoldingIsStale`.
  - `GenerateReport_UnresolvedTaxClassification_IncompleteStatus_IsNamed`.
  - `GenerateReport_UnresolvedTaxClassification_RequiresReviewStatus_IsNamed`.
  - `GenerateReport_TaxClassification_FinalOrEstimatedStatus_NotReported`.
  - `GenerateReport_SupersededTaxClassification_NeverReported` — the Decision 7 regression test,
    mirroring F01's own supersede-never-rewrite regression test for `DisposalRecord`.
  - `GenerateReport_UnresolvedTaxClassification_HistoricHolding_StillReported` — proves the
    dual-scope enumeration (Rule 3).
  - `GenerateReport_AssetWithTwoUnresolvedClassifications_EmitsTwoFindings` — one asset, two tax
    years, two separate list entries, not deduplicated per asset.
  - `GenerateReport_ExistingFiveFields_UnaffectedByNewDependency` — a fixture already covered by
    an existing test in this file, re-run to confirm `SalesExceedPurchases`/`UnpricedOpenHoldings`/
    `UnclassifiedHoldings`/`HistoricHoldingsStillOpen`/`UnclassifiedAndUnpricedOpenHoldings` are
    byte-for-byte unchanged after the constructor/method change (Rule 6's own regression proof).
  - Every existing test method in the file (10 tests, listed in the file today) continues to pass
    unmodified except for the `CreateService()` helper's signature, which gains the new
    `IHoldingValuationService` parameter with a `TestHoldingValuationService` default.

### Integration — `Tests/Financial.Api.Tests/`

- `Controllers/DataQualityReportControllerTests.cs` (pattern: `DashboardControllerTests`, or the
  guard-clause-only convention if that is how `SummaryController`/`DashboardController` are
  already tested — check at implementation time): `GetDataQualityReport_ReturnsOk`.
- `Acceptance/DataQualityWarningsAcceptanceTests.cs` — one `[Fact]` per F03 §9 bullet, each tagged
  `[Trait("AC", "P52-F03-data-quality-warnings-0N")]`:
  - Missing-price count matches `UnpricedOpenHoldings.Count`.
  - Missing-cost-basis count matches `OpenHoldingsMissingCostBasis.Count`.
  - Stale-valuation count matches the number of Active holdings whose `MarketStatus` is `Stale`
    (seed one current and one stale-priced Active holding, assert `StaleValuationCount == 1`).
  - Unresolved-tax-classification count matches `UnresolvedTaxClassifications.Count`.
  - Impossible-cash-flow-sequence count matches `SalesExceedPurchases.Count`.
  - A clean portfolio still returns all 5 category fields present and empty/zero (proving the
    endpoint itself never hides a zero category — Decision 9/Rule 5; the *hiding* behaviour itself
    is proven in F05/F06's own test suites, not here).
- `Contract/OpenApiContractTests.cs` — no new test method; the existing
  `OpenApiDocument_NumericProperties_...` and snapshot-diff tests automatically cover the extended
  DTO/new endpoint once the snapshot is regenerated (plan.md's final phase).

### Not covered here (other features' responsibility)
- The warnings panel's expand/collapse UI and per-category hide-if-zero rendering, and
  click-through navigation to the affected holding's tree node — F05/F06.
- F01/F02/F04's own endpoints and DTOs.
