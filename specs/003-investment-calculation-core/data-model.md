# Phase 1 Data Model: Investment Calculation Core

**Branch**: `003-investment-calculation-core` | **Date**: 2026-09-10 | **Research**: [research.md](./research.md)

**This feature widens no stored entity.** No field is added to any transaction, income record or
price. Two derived fields are *removed* from storage, and everything else here is either a new
in-memory type or an additive field on a wire DTO. Rationale for each choice is in `research.md`;
this document states the shapes and the rules that bind them.

---

## 1. Stored entities — changes

### `Asset` (Domain entity)

| Change | Detail |
|---|---|
| **+ method** | `GetPriceAsOf(DateOnly date)` — the most recent price on or before `date`. Additive; the two existing readers keep their meanings. See R3. |
| **+ methods** | Strict `RecordTransaction` / `ReviseTransaction` / `RetractTransaction`, which validate then delegate to the existing tolerant methods. The tolerant four are unchanged. See R12, R13. |
| **− storage** | `PositionType` stops being written to the data file. It is a computed property with no setter, so it was never read back. |

### `Portfolio` (Domain entity)

| Change | Detail |
|---|---|
| **− storage** | `IsEmpty` stops being written, for the same reason as `PositionType`. Both are named by FR-005. |

### `Transactions` (Domain collection)

| Change | Detail |
|---|---|
| **behaviour** | Replay is date-ordered, purchases before sales within a date. `_items` holds the replay order. `Add` keeps an in-order fast path; out-of-order arrivals trigger a recompute. See R11. |
| **behaviour** | The average-price division is guarded against a zero resulting quantity **in the Buy branch only** — generalising it would move ~128 historic holdings and break SC-002. See R14. |

**Storage impact.** Removing the two derived keys and re-ordering three holdings' transaction arrays
are the only changes the next write makes to `data-investment.json`. Both directions are safe:
`UnmappedMemberHandling` is left at its default `Skip`, so an existing file carrying the removed keys
loads cleanly, and an older build never read them either.

---

## 2. New Domain types

### `HoldingValuation` — record, in `Domain/Rules/HoldingValuationCalculator.cs`

| Field | Type | Rule |
|---|---|---|
| `MarketValue` | `decimal?` | Quantity valued at the applicable price. `null` when no price is available (FR-030) — never nought. |
| `CostOfUnitsHeld` | `decimal` | Always derivable. Never negative (FR-021). |
| `UnrealisedGain` | `decimal?` | `MarketValue − CostOfUnitsHeld`; `null` whenever `MarketValue` is (FR-037). **Not reported at all in Historic Investments** (FR-075). |
| `PriceAsOfDate` | `DateOnly?` | The date of the price used (FR-031). |
| `IsStale` | `bool` | The price predates the most recent weekday *strictly before* the valuation date (FR-032). |

**Invariants to assert in Domain tests**: `UnrealisedGain is null ⟺ MarketValue is null`, and
`MarketValue is null ⟺ PriceAsOfDate is null`.

Two entry points rather than a flag, because `InvestmentScope` is an Application enum and Domain
cannot take it:

- `Calculate(quantity, averagePrice, price, valuationDate)` — Active
- `NotMarkedToMarket(quantity, averagePrice)` — Historic (FR-034)

### `SaleCoverageRule.UncoveredSale` — record struct

| Field | Type | Purpose |
|---|---|---|
| `Sale` | `Transaction` | The sale left short — named in the refusal (FR-066). |
| `HeldQuantity` | `decimal` | Quantity held on that sale's date. **Formatted round-trip, not `N2`** (FR-012). |
| `ShortBy` | `decimal` | The shortfall. |

`FindFirstUncoveredSale(IEnumerable<Transaction>)` returns `UncoveredSale?`. Pure, non-throwing, and
quantity-only — so it cannot hit the FR-016 division and is unaffected by it. The same function
judges a proposed change (FR-015) and reports a stored breach (FR-054).

### `TransactionReplayOrder` — the FR-002 ordering

One owner, consumed by `Transactions`, `SaleCoverageRule` and the data-quality report, so the three
cannot drift apart. Date ascending, then purchases before sales; ties keep source order, which is
sufficient because weighted-average folding over purchases is commutative and sales do not move the
average.

### `OpenPositionCostCalculator`

`CostOfUnitsHeld(asset) => Math.Max(0m, quantity * averagePrice)`. Scope-free, so legal in Domain. The
clamp is FR-021's guarantee, sited at the one place the product can go negative.

---

## 3. New Application types

### `AssetTotals` — extended, and moved to its own file

Today it is declared inside `PortfolioAssetSummaryBuilder.cs`, which is why `BrokerBreakdownService`
cannot reach it and re-derives its own tuple.

| Field | Type | Note |
|---|---|---|
| `TotalBought` | `decimal` | unchanged |
| `TotalSold` | `decimal` | unchanged |
| `TotalCredits` | `decimal` | unchanged |
| `OpenPositionCost` | `decimal` | **new** |
| `MarketValue` | `decimal?` | **added by the valuation increment**, not the first one |

Static `For(asset)` factory gives all three services one entry point, so FR-018 and FR-022 hold by
construction rather than by three services agreeing to agree. It stays *measurements*, never policy.

### `AssetAmountBases` — replaces `AssetInvestedAmountSelector`

| Field | Active | Historic |
|---|---|---|
| `InvestedAmount` | cost of open units (FR-019) | total bought (FR-020) |
| `WeightBasis` | market value *after the weight increment*; the old formula before it | total bought — stays cost-based (FR-050) |
| `IncomeYieldBasis` | same as `InvestedAmount` (amended FR-049) | total bought |

Decided by **scope, never by whether quantity is nought** — four historic holdings still carry a
quantity. Three named fields make FR-025's "separately identified even where they currently hold the
same value" structural rather than a naming convention, and let the weight basis move later without
dragging the yield with it.

---

## 4. Wire DTOs — all additive except one

Application DTOs are the literal wire format, so each row below is an OpenAPI snapshot change
requiring `npm run generate-api-types` in the same PR.

### `AggregatedSummaryDTO` — five additive fields (R6)

| Field | Type | Rule |
|---|---|---|
| `MarketValue` | `decimal?` | Sum over holdings that could be valued. `null` when holdings exist and none could be valued; `0m` for an empty portfolio. |
| `HoldingCount` | `int` | Distinguishes empty from all-unvaluable (FR-041). |
| `UnvaluedHoldingCount` | `int` | Published, not derived — two subtractions are two chances to disagree (FR-061). |
| `PriceOnlyReturn` | `decimal?` | Fraction. `null` whenever `UnvaluedHoldingCount > 0` (FR-042). |
| `TotalReturn` | `decimal?` | Fraction. Same withholding rule. |

`TotalInvested` keeps its name and type while **changing meaning** (FR-019). The contract test is
blind to that — it compares shapes — so it needs its own verification and its own increment.

### `PortfolioAssetSummaryItemDTO` / `AssetDetailsDTO` — additive valuation fields

`MarketValue`, `CostOfUnitsHeld`, `UnrealisedGain`, `PriceAsOfDate` (`DateOnly?` — precedent already
on the wire at `AssetPriceDTO.AsOfDate`), `IsPriceStale`, `PriceOnlyReturn`, `TotalReturn`.

The existing `CashFlows` / `CashFlowsWithCredits` / `CashFlowsWithoutCredits` fields **stay for now**.
Removing them is a second wire break and should wait until both front ends have stopped calling
`POST /xirr/calculate` per row.

### `PortfolioAssetSummaryItemDTO.PortfolioWeight` — the one genuine break

`decimal` → `decimal?` (FR-047). This is a reshape, not an addition: OpenAPI `number` →
`["null","number"]`, TypeScript `number` → `number | null`. It ships in its own compatibility-boundary
PR **while the value is still always non-null**, which is the only ordering where `main` never renders
`0.0%` for an unvaluable holding.

The break is a **useful tripwire**: `tsc -b` and `csc` both fail at the two grid sites, forcing each to
declare its unknown-rendering rather than silently defaulting. The two detail views are already
null-safe and are gated to Historic scope, where the value can never be unknown.

---

## 5. State and lifecycle rules

**A holding's valuation state** is one of three, and they must stay distinguishable (FR-064):

| State | When | Renders as |
|---|---|---|
| Valued | a price exists on or before today | the figure |
| Valued but stale | that price predates the most recent weekday before today | the figure, marked |
| Unavailable | no price at all, on or before today | `—`, never `0` |

**A level's total** is one of four:

| State | `HoldingCount` | `UnvaluedHoldingCount` | Total | Returns |
|---|---|---|---|---|
| Empty portfolio | 0 | 0 | `0m` | `null` |
| Fully valued | M | 0 | sum | computed |
| Partially valued | M | 0 < N < M | sum, marked incomplete with N stated | **withheld** |
| Nothing valuable | M | M | `null` | `null` |

**A holding with quantity nought is valued at nought without needing a price** — it is not "unvalued".
The spec's own edge case says a sold-out position "costs nothing, is worth nothing and contributes no
share", and FR-055 scopes the missing-price report to *open* holdings. Otherwise one closed position
would withhold an entire portfolio's rate.

**Historic Investments** is never marked to market: market value is nought by rule, unrealised gain is
not reported (FR-075), the closing balance for both returns is nought, and the share basis stays on
cost. All of this keys off the **scope a holding is filed under**, never its quantity.
