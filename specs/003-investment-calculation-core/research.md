# Phase 0 Research: Investment Calculation Core

**Branch**: `003-investment-calculation-core` | **Date**: 2026-09-10 | **Plan**: [plan.md](./plan.md)

Every decision below was verified against the code at `main @ 24bbf41d` and, where a number is
quoted, against `data/data-investment.json` by replaying the same arithmetic `Transactions.Add`
performs. Three of these questions produced spec amendments; those are listed at the end.

---

## R1 — Where the single owner of valuation lives

**Decision.** Split across two layers, copying `CreditsAnalysisCalculator` exactly:

- **Domain** — `Financial.Investment.Domain/Rules/HoldingValuationCalculator.cs`, containing a
  `public sealed record HoldingValuation(…)` and a `public static class HoldingValuationCalculator`.
  Pure arithmetic over scalars plus an `AssetPriceSnapshot?`.
- **Application** — `IHoldingValuationService` / `HoldingValuationService`, which performs the price
  lookup, makes the Active/Historic decision, and composes the two return rates.

Two named entry points, `Calculate(quantity, averagePrice, price, valuationDate)` and
`NotMarkedToMarket(quantity, averagePrice)`, rather than a `bool markToMarket` flag.

**Rationale.** It satisfies *both* extraction criteria in `docs/rules/implementation.md` §Domain
rules, not just one: three Application call sites need it (`NavigationService`,
`PortfolioAssetSummaryBuilder`, `SummaryService`), and it is a stateless algorithm earning its own
Domain test class beside `ProfitCalculatorTests` and `XirrCalculatorTests`. The
`*Service`/`*Policy`/`*Specification` ban is respected by naming — `HoldingValuationCalculator` joins
`AssetTotalsCalculator`, `CreditsAnalysisCalculator`, `TransactionFeeCalculator`, `ProfitCalculator`
and `XirrCalculator`, introducing no new class kind.

`CreditsAnalysisCalculator` is the precise precedent: a record of seven related figures, several
nullable-for-unavailable, one static entry point, `today` passed in rather than read.

The two entry points exist because **`InvestmentScope` is an Application enum**
(`Financial.Investment.Application/Enums/InvestmentScope.cs`), so Domain cannot take it. The
scope→behaviour mapping stays in Application — the same split `AssetTotalsCalculator` (Domain) and
`AssetInvestedAmountSelector` (Application) already use.

**Alternatives rejected.** A method on `Asset` (the entity would own an algorithm meeting both
extraction criteria, and the scope decision would have nowhere sensible to live). A
`HoldingValuationPolicy` in Domain (explicitly banned). Application-only with no Domain rule (fails
the extraction test in reverse). One rule owning the XIRR rates too (would drag
`AssetCashFlowBuilder` and its Application DTO into Domain, and fuse two independent algorithms into
one test class).

---

## R2 — How "unavailable" is represented

**Decision.** Nullable value types on an immutable record. No `Result`/`Either`, no sentinel.
`MarketValue`, `UnrealisedGain` and `PriceAsOfDate` are nullable; `CostOfUnitsHeld` is not.

**Rationale.** "Unavailable" is not failure, so `implementation.md`'s "failure is signalled by
exceptions, never a `Result`/`Either`" does not govern — but the positive precedent for *absence* is
uniform. `XirrCalculator.Calculate` returns `decimal?` and its XML doc states the semantics the spec
needs almost verbatim: "A null result therefore means the series admits no rate in that interval —
never that the solver gave up on a rate that exists." The idiom repeats in
`ProfitCalculator.CalculateProfitPercent`, `Transactions.AverageSellPrice`, `Asset.GetPriceForDate`,
`CreditsAnalysis`'s four nullable figures, and `AssetPriceDTO.AsOfDate` (already `DateOnly?` on the
wire). A result type would be the first in the solution and would still encode as `null` in JSON.

**Invariant for the Domain tests**: `UnrealisedGain is null ⟺ MarketValue is null`, and
`MarketValue is null ⟺ PriceAsOfDate is null`. That is FR-037 as a testable property.

**Alternatives rejected.** `Maybe<T>`/`ValuationResult<T>` (no precedent, wire friction).
`decimal.MinValue` or `-1` sentinels (FR-064 forbids anything readable as a number). Throwing when no
price exists (FR-030 makes this a normal state for 7 of 28 positions, and WPF reads these from
data-binding getters where an exception is unrecoverable).

---

## R3 — The as-of price lookup

**Decision.** Add exactly one method to `Asset` and change nothing else:

```csharp
public AssetPriceSnapshot? GetPriceAsOf(DateOnly date) =>
    _priceHistory.Where(entry => entry.Date <= date).MaxBy(entry => entry.Date);
```

**Rationale.** Neither existing reader satisfies FR-028. `GetPriceForDate` is exact-match;
`GetMostRecentPrice` does not exclude future dates — and the stored file can contain one, because
loading bypasses `AssetPriceSnapshot.Create`'s validation. `UpsertInto` enforces one entry per date,
so `MaxBy` has no tie to resolve, and `n` is a handful per asset.

Adding rather than redefining matters: `GetPriceForDate` is used by
`AssetPriceLookupService.FindManualPriceForToday` and the upsert check, both of which genuinely want
an exact date; `GetMostRecentPrice` backs the *fetch fallback*, a different question from a valuation.

**Do not sort `_priceHistory`.** `UpsertPriceEntry` carries an explicit concurrency contract —
history is replaced wholesale because "an in-place Add or index assignment breaks an enumeration that
is already running, which is what produced 'Collection was modified' during a save". An ordered
insert re-opens a bug that was fixed deliberately, and would change the order `AssetDetailsDTO.
PriceHistory` is produced in.

**Alternatives rejected.** Redefining `GetMostRecentPrice()` in terms of today (puts a clock read in
Domain and silently changes the fetch fallback).

---

## R4 — Staleness, and where "today" comes from

**Decision — the rule.** `stale ⟺ priceDate < PreviousWeekday(valuationDate)`, where
`PreviousWeekday` steps back one day then keeps stepping while Saturday or Sunday.
`PreviousWeekday` is a `private static` member of `HoldingValuationCalculator` — one call site, and
§Domain rules is explicit that "premature extraction is its own defect".

**This corrected a defect in the spec.** FR-032 originally read "dated earlier than the most recent
weekday **on or before** the valuation date", which on a Monday resolves to Monday and marks a Friday
price stale — contradicting the same requirement's next sentence, the Clarifications answer ("current
if dated today or the most recent *preceding* weekday") and the Assumptions. FR-032 now reads
*strictly before*. A plan written from the original clause would have produced the opposite behaviour
on Mondays, and no test would have caught it unless it ran on a Monday.

**Decision — the clock.** There is **no clock abstraction in the Investment context**.
`TimeProvider` is registered only by CashFlow
(`CashFlowApplicationServiceCollectionExtensions.cs:11`), so an Investment service resolving it today
would do so *by accident, through the other bounded context* — a soft breach of bounded-context
isolation. Three moves, no refactor:

1. Domain never reads the clock — `Calculate` takes `DateOnly valuationDate`, as
   `CreditsAnalysisCalculator.Calculate(…, DateTime today)` already does.
2. The Application service takes `TimeProvider? timeProvider = null`, defaulting to
   `TimeProvider.System` — copied verbatim from `HistoricAverageService`, `IncomeSummaryService`,
   `InvestmentAnnualResultService` and `PaymentsDueService`. Optional and trailing, so no existing
   call site, test or registration changes.
3. Register `services.TryAddSingleton(TimeProvider.System)` in Investment's own DI extension, so the
   accidental cross-context dependency does not become load-bearing. `TryAdd` avoids a duplicate with
   CashFlow's unconditional `AddSingleton`, and `ApiTestFactory`'s `RemoveAll<TimeProvider>()` already
   clears every registration before installing an override.

Use `GetUtcNow()`, matching every service in the solution bar one; `FakeTimeProvider` overrides only
`GetUtcNow()`, so `GetLocalNow()` would make tests depend on the agent's time zone.

**Accepted trade-off.** Investment code today uses `DateTime.Today` (local). UTC differs from
Brazilian local date between 21:00 and midnight BRT. The consequence is bounded — it can shift the
staleness marker for a few evening hours, never which price is selected. `PaymentsDueService` shows
the one-parameter `TimeZoneInfo` upgrade path if a real defect appears.

---

## R5 — Aggregating cash flows to portfolio and broker level

**Decision.** Plain concatenation. **Do not merge same-date flows.**

**Rationale — proven from the code, not assumed.** `XirrCalculator` sorts internally
(`OrderBy(cf => cf.Date)`, a stable sort) and `NetPresentValue` sums
`amounts[i] / Math.Pow(growthFactor, years[i])` term by term. Two entries sharing a date share a
`years[i]`, so `a/(1+r)^y + b/(1+r)^y ≡ (a+b)/(1+r)^y` — the solved rate is **indifferent** to
merging. But two gates read shape rather than sum, and merging makes both strictly worse:

- `if (cashFlows.Count < 2) return null;` counts raw entries — merging can only reduce the count.
- `HasSignChange` needs a strictly positive **and** a strictly negative entry. Merging a −100 buy in
  one holding against a +100 sale in another on the same date yields a single `0`, which is neither —
  turning a solvable series into a null.

So merging is unnecessary and can only lose answers.

**Where it lives: Application**, an `internal static` helper alongside `AssetCashFlowBuilder`. Not
Domain: it fails both extraction criteria (one caller shape — a broker's holdings are just
`SelectMany` over its portfolios), it is concatenation and a `Sum` rather than an algorithm, and the
flows are `AssetCashFlowDTO`, an Application type. The genuine algorithm is already in Domain and
FR-044 keeps it untouched.

**One additive signature change.** `XirrCalculationService.Calculate` hardcodes
`series.Add((DateTime.Today, terminalValue))`. Add an `asOf` overload and have the two-arg member
delegate to it. FR-044 protects `XirrCalculator`; `XirrCalculationService` is an Application shim, and
the overload leaves `XirrController` and its OpenAPI path unchanged.

**Currency safety is structural, not arithmetic.** The only aggregation entry points are
`GetPortfolioSummary(brokerName, portfolioName)` and `GetBrokerSummary(brokerName)`, both already
scoped to one broker. FR-045 is satisfied by not adding an all-brokers entry point — worth an explicit
acceptance test rather than a runtime guard.

---

## R6 — The level DTO shape

**Decision.** Extend `AggregatedSummaryDTO` additively with five fields. No new DTO, no new endpoint.

```csharp
decimal? MarketValue;           // sum over holdings that could be valued
int      HoldingCount;          // M
int      UnvaluedHoldingCount;  // N
decimal? PriceOnlyReturn;       // fraction, null when unavailable
decimal? TotalReturn;           // fraction, null when unavailable
```

Rules, each traced: `MarketValue` is published even when incomplete (FR-040) but is `null` when
holdings exist and *none* could be valued; an empty portfolio is `0m` with `HoldingCount == 0`
(FR-041 — this is why the count must be published, not just the unvalued count); both returns are
`null` whenever `UnvaluedHoldingCount > 0` (FR-042 — "a different answer", not an approximation);
`UnvaluedHoldingCount` is published rather than derived, because two independent subtractions are two
chances to disagree (FR-061). Rates are **fractions**, matching `XirrResultDTO.Xirr` and
`XirrCalculator`; the fraction-vs-percentage inconsistency FR-036 complains about is settled by making
the wire always a fraction and fixing formatting in the UI increment.

**A holding with `Quantity == 0` is valued at 0 without needing a price** — it is not "unvalued".
The spec's own edge case says a sold-out position "costs nothing, **is worth nothing** and contributes
no share", and FR-055 scopes the missing-price report to *open* holdings. Otherwise one closed
position would withhold an entire portfolio's rate.

**Blast radius, measured.** Five new properties under one existing schema: no new path, no renamed or
removed field. `types.ts` needs **zero hand edits** — the alias is
`export type AggregatedSummaryDto = Schema<'AggregatedSummaryDTO'>`, so regeneration carries the
fields through, and `openapiFreshness.test.ts` catches a forgotten regeneration in the same PR. WPF
compiles unchanged: the DTO is `sealed` with `init`-only properties and every construction is an
object initializer.

**Alternatives rejected.** A separate `LevelValuationDTO` behind a new endpoint — defensible only if
level valuation were expensive enough to want on its own cadence, but it is an in-memory pass over an
already-loaded graph, so the extra round trip buys nothing and permanently splits the contract.
FR-038 also asks for it in so many words: a market value and a rate "**alongside the amounts already
reported**".

**The real wire risk is elsewhere and is sequenced separately**: `TotalInvested`'s *meaning* changes
under FR-019 with its name and type intact, so the contract test is blind to it; and
`PortfolioWeight` must become `decimal?` under FR-047, which is a genuine break. See R8.

---

## R7 — Separating the one number into three

**Decision.** Raw measurements in one record, per-scope policy in another, arithmetic in Domain.

- **Domain** — `OpenPositionCostCalculator.CostOfUnitsHeld(asset) => Math.Max(0m, quantity * averagePrice)`.
  Scope-free, so legal in Domain. The clamp is FR-021's guarantee, sited where the product can go
  negative. Named narrowly so R1's `HoldingValuation` consumes it rather than duplicating "cost of
  units held" — one of the three sites SC-007 counts.
- **Application** — `AssetTotals` gains `OpenPositionCost` and moves to its own file with a static
  `For(asset)` factory. It is declared today on line 7 of `PortfolioAssetSummaryBuilder.cs`, which is
  why `BrokerBreakdownService` cannot reach it and re-derives its own tuple.
- **Application** — `AssetAmountBases(InvestedAmount, WeightBasis, IncomeYieldBasis)` and its
  selector replace `AssetInvestedAmountSelector`.

**The non-obvious part.** `InvestedAmount` and `IncomeYieldBasis` both become the corrected
cost-of-open-units figure (per the amended FR-049 — see the spec amendments below), while
`WeightBasis` is initialised to the *old* frozen formula so that the first PR moves exactly one thing.
`WeightBasis` is repointed at market value only in the US6 increment. Three named fields make FR-025's
"separately identified even where they currently hold the same value" structural rather than a
naming convention.

**`AssetTotals.For` gives all three services one entry point**, so FR-018 and FR-022 hold by
construction rather than by three services agreeing to agree.

**Alternatives rejected.** Extending `AssetTotals` with the three outputs (merges measurement with
policy — how one number got three jobs in the first place). Moving `InvestmentScope` into Domain (a
large cross-cutting move for no requirement). Three separate scalar selector methods (nothing stops a
caller using one where it means another — the current defect with extra steps).

---

## R8 — Making portfolio weight nullable safely

**Decision.** Widen `PortfolioAssetSummaryItemDTO.PortfolioWeight` to `decimal?` in its own
compatibility-boundary PR, *before* the value can ever be null, and unify the format to **2 decimal
places** in the same PR.

**Rationale.** The hazard is confined to two sites, which was not obvious. Of the four render sites,
the two detail views are already null-safe *and* gated to Historic scope — `AssetSummaryTab.tsx:137`
sits inside `{scope === 'historic' && …}` and WPF's `DisplayRealizedPortfolioWeight` is bound with
`Visibility="{Binding AssetDetails.IsHistoricScope…}"` — where FR-050 keeps the basis on cost and the
value can never be unknown. Only the two **grids** would render `0.0%`.

Widening while the value is still always non-null is the only ordering where `main` never shows
`0.0%` for an unvaluable holding — FR-047 calls that reading "the opposite of true" for holdings that
include the largest position in two portfolios.

The widening is also a **useful tripwire**: after regeneration `tsc -b` fails at
`PortfolioSummaryTab.tsx:117` and `csc` fails at `PortfolioAssetSummaryRowViewModel.cs:154`, forcing
both grids to declare their unknown-rendering rather than silently defaulting.

**2 dp, not 1.** It matches both detail views, and it matches the column immediately beside it in the
same grid — `PortfolioSummaryTab.tsx:160` already renders `lastMonthCreditsPercent` at 2 dp, and
`PortfolioAssetSummaryRowViewModel.cs:73` uses `F2` for the same field. Choosing 1 dp would change
three sites instead of two and leave the grid internally inconsistent. `—` is the established
unavailable marker in both front ends.

**Alternatives rejected.** A `-1` or `0` sentinel (FR-047/FR-064 forbid nought, and the sort accessor
at `PortfolioSummaryTab.tsx:202` would sort unvaluable holdings to a meaningful position). A parallel
`PortfolioWeightAvailable: bool` (two fields that can disagree, and a bigger wire change). Deferring
nullability to the market-value PR (that PR would exceed 8 files and puts the `0.0%` state one bad
merge away).

**Flagged for verification, not blocking**: WPF `F2` (current culture, `MidpointRounding.AwayFromZero`
on `decimal`) and `Intl.NumberFormat` (browser locale, half-even on `double`) can disagree on the last
digit and on separators. FR-060 demands identity "to the last digit displayed", so a parity test on a
`.005` boundary is warranted.

---

## R9 — What `SummaryService` must change, and what moves on screen

**Decision.** Replace the aggregate-level formula with a per-asset sum, delete the `Quantity != 0`
Active filter, and delete the XML comment above it.

**Rationale.** `Σ max(0, qᵢ·apᵢ) ≠ ΣB − ΣS`, so an aggregate-level formula cannot be made to
reconcile with the rows at all — FR-022 requires the per-asset sum. The filter goes because FR-023
requires rows and total to treat a zero-quantity Active holding identically, and the rows never
filtered. The comment goes because it is **factually wrong**, not merely obsolete: it asserts "every
asset there has `Quantity == 0` by definition", and four historic holdings carry a quantity.

**Measured effect on the live data** — this is the largest visible change in the feature and must be
in the PR body:

| | Today | After |
|---|---|---|
| Active holdings whose invested amount moves | — | **5 of 28** (AGNC ISA, BRCO11, HGRU11, Bitcoin, and FreeTrade S&P 500 from **−683.09 to 28.65**) |
| Active portfolios where the total ≠ Σ rows | 0 of 10 | 0 of 10 — but 4 totals *move*, by up to 711.74 |
| Historic portfolios where the total ≠ Σ rows | **15 of 15** | 0 of 15 — e.g. `XPI/FII` **2,949.87 → 61,413.07** |
| Active holdings with `Quantity == 0` | **0** | 0 — so removing the filter changes no number today |
| Allocation chart | — | gains one slice: FreeTrade S&P 500 crosses `> 0` |

The historic movements are large because the historic total is currently `ΣB − ΣS` while the historic
rows have always shown `ΣB`. That is FR-022 being satisfied for the first time, not a regression.

**Alternatives rejected.** Fixing FR-022 by making the front ends sum the rows (inverts FR-061 and
leaves the API serving a total that contradicts its own rows for any non-browser consumer). Having
`SummaryService` call `PortfolioAssetSummaryService` and sum the DTOs (recomputes cash flows, credits
analysis and XIRR inputs for every asset just to add one column). Keeping the filter for
`TotalInvested` only (with the new definition a zero-quantity holding contributes exactly 0, so it is
provably a no-op there while still corrupting the other three fields).

---

## R10 — The front-end footers

**Decision.** Keep both footers; stop deriving Total Invested and Total Credits client-side and read
them from `AggregatedSummaryDTO`. Leave realised gain, current-month credits and estimated annual
credits as client sums — those are not figures this specification defines.

**Rationale.** FR-061 is categorical, and FR-022's own justification names these footers as the
defect: "each front end already prints two of those answers on the same screen, a portfolio total
above a column footer that sums the rows differently."

After the first PR the two are *arithmetically* equal — which is the strongest argument for deleting
the derivation, not for keeping it. An equality holding only because two independent implementations
currently agree is exactly the silent breakage SC-005 warns about.

In React, `PortfolioSummaryTab` already renders `<AggregatedSummaryTab />`, which calls
`useAggregatedSummary()` internally; lift that call into `PortfolioSummaryTab` and pass the summary
down, so footer and header read the same response object with still exactly one request. In WPF it is
one line — `summary` is already a parameter of `LoadPortfolioSummary` and already feeds the header.

**Alternatives rejected.** Deleting the footers (removes a real affordance — a column-aligned total
under a dense grid is a different reading task from a summary card — and over-reaches a spec that asks
for reconciliation, not removal). Keeping the client sums (the one honest argument, that two
independent requests can momentarily disagree, is defeated by lifting the hook; and there is no
client-side filtering of the grid, only sorting, so "sum of visible rows" has nothing to mean).

---

## R11 — How date-ordered replay lands in `Transactions`

**Decision.** Separate "append raw" from "recompute", with an in-order fast path, and let the
recompute write the ordered sequence back into `_items`. `Add` appends, then applies incrementally if
the new transaction still sorts last, or triggers a full `Recompute()` if it does not.
`Update`/`RemoveById` keep their positional copy-then-`Rebuild` shape.

**Rationale.** `Add` **must** stay the population entry point: `Asset.Transactions` has a `private
set` wired by `ReflectionJsonTypeInfoHelpers.WirePropertySetter`, so `System.Text.Json` constructs a
fresh `Transactions` and calls `ICollection<Transaction>.Add` once per element in file order. Any
design assuming a bulk hand-off breaks loading — and `InvestmentTypeInfoResolverTests` already proves
the per-element path empirically.

The fast path costs almost nothing on real data: 899 transactions across 160 holdings, one holding
stored out of date order and two storing a same-date sale before a purchase, so `Recompute()` fires
**three times** in a whole-file load and the other 896 appends pay one extra comparison.
`OrderBy`/`ThenBy` is documented-stable, so ties keep file order — sufficient for FR-003, because
weighted-average folding over buys is commutative and sells do not move the average. No tertiary
tiebreaker: adding one would make the order arbitrary, not more deterministic.

**Accepted costs.** `Add` is no longer O(1)-guaranteed (worst case O(n log n); the largest holding has
223 transactions), which must be documented on the method because the signature reads as an
incremental fold. And `_items` now holds replay order, so the next write re-orders the transaction
arrays of exactly three holdings in the data file — a small, explainable diff.

**Alternatives rejected.** Always recompute (turns a whole-file load into 899 recomputes to change
three holdings; the `stillInOrder` check that avoids it is three lines). Ordered insert keeping the
incremental fold (a mid-list insert invalidates every subsequent sale's realised gain anyway, so the
recompute is still needed and the binary search buys nothing). Keeping `_items` in stored order and
replaying a sorted copy (puts two orders inside one object — `Update`/`RemoveById` index into
`_items` while the figures come from a different sequence — and lets the file's order permanently
disagree with the order the figures came from, the same staleness FR-005 exists to remove).

**Iteration order changes as a consequence, and that is safe.** Every consumer re-sorts:
`NavigationService` (`OrderByDescending(Date)`), `TransactionService` (`OrderBy(Date).ThenBy(Name)`),
`AssetCashFlowBuilder` (`Sort` by date), `AssetTotalsCalculator` (commutative sums),
`PriceHistoryChartBuilder` (`Min`/`Max` and `OrderBy`), both grids (sortable-column behaviours). The
only observable difference is the same-date tie-break for the two affected holdings. Do **not** add a
separate display-order concept — one order, derived once, is what makes FR-003 and FR-004 inspectable.

---

## R12 — Where the impossible-sale rule lives

**Decision.** Three parts:

1. **`Financial.Investment.Domain/Rules/SaleCoverageRule`** — a pure, non-throwing
   `FindFirstUncoveredSale(IEnumerable<Transaction>)` returning the offending sale, the quantity held
   on its date, and the shortfall, or null.
2. **`Financial.Investment.Domain/Rules/TransactionReplayOrder`** — owns the FR-002 ordering, so
   `Transactions`, the coverage rule and the data-quality report cannot drift apart. This is the
   single highest-value extraction in the feature.
3. **New strict methods on `Asset`** raise `InvestmentRuleViolationException`; the existing tolerant
   `AddTransaction`/`UpdateTransaction`/`RemoveTransaction` are left untouched.

**Rationale.** Both `Domain/Rules/` criteria are met: two call sites (the entity guard and the FR-054
report; three for the ordering) *and* a stateless algorithm with its own Domain test class. The
`*Service`/`*Policy`/`*Specification` ban is satisfied by naming — `SaleCoverageRule` joins
`DividendValuationRules`, `ProfitCalculator`, `CreditFrequencyAnalyzer`, `XirrCalculator`.

Raising from `Asset` matches all eight existing `InvestmentRuleViolationException` sites (`Broker`,
`Investments`), and the check-then-mutate shape is verbatim `Broker.CreatePortfolio`.
`DomainExceptionMappingMiddleware` already maps it to 409 with the domain message in the
ProblemDetails `detail`, which `financialApiClient.ts` already surfaces verbatim as `saveError`.

**The candidate sequence needs no new API** — `Transactions` is already `IEnumerable<Transaction>`, so
add is `[.. Transactions, t]`, edit is a `Select` swap, delete is a `Where`. FR-065 and FR-066 then
fall out for free: the walk covers the whole resulting history, and the returned sale is the one to
name. The message branches on whether the offending sale *is* the transaction being edited.

**FR-012 detail**: the held quantity must be formatted with round-trip `decimal.ToString()`, **not**
the `N2`/`formatN2` used everywhere else — the point of the requirement is that the user can copy
`2128.37599271` out of the message.

**Where it runs**: inside the mutation lambda in `TransactionService`, so the check executes inside
`ApplyAndSaveAsync`. A throw out of `applyChanges()` means serialize and write never run and the
`finally` still releases the gate — so nothing is stored *and* nothing changes in memory, which is
FR-009's "MUST leave nothing stored" in full.

**Alternatives rejected.** The check in `TransactionService` (splits the rule from its message,
diverges from eight Domain-raised sites, leaves the invariant unenforced for any future caller). The
check inside `Transactions.Add` (ruled out by the code: `Update`/`RemoveById` replay through `Add`, so
the three breaching holdings become uncorrectable, and deserialization fails at startup).

---

## R13 — The seam that keeps loading and importing tolerant

**Decision.** The seam is **method identity**, not a flag, and it already exists in the call graph:

| Method | Production callers | Treatment |
|---|---|---|
| `Transactions.Add` (`ICollection` member) | System.Text.Json, per element, on load | stays tolerant |
| `Asset.AddTransactions` → `Transactions.AddRange` | exactly one: the spreadsheet importer | stays tolerant (FR-014) |
| `Asset.AddTransaction` / `UpdateTransaction` / `RemoveTransaction` | exactly one each: `TransactionService` | stays tolerant — see below |
| **New** strict methods on `Asset` | `TransactionService` is repointed at these | validates |

**Why not simply add the check to the singular `Asset.AddTransaction`**, given `TransactionService` is
its only production caller? Because it would break legitimate test fixtures that deliberately build
the short states FR-013 requires to remain loadable — `CreditServiceTests` (a sell with no purchase),
`AssetTests.PositionType_NegativeQuantity_ReturnsShort`, `NavigationServiceTests`'
`[InlineData(-10, PositionType.Short)]`. Those tests are correct; making them throw would be a false
signal. **If any of them needs editing during implementation, it proves the check was put on the
tolerant method instead of the strict one.**

---

## R14 — The divide-by-zero fix, and a trap in generalising it

**Decision.** Guard **only** the zero denominator, in the **Buy branch only**, setting
`AveragePrice = 0`.

**Rationale.** `Transaction` already rejects `quantity <= 0`, so a Buy producing a zero resulting
quantity is reachable only when the quantity is already negative — i.e. only after a stored oversell.
Zero is the right value: it is what the field initialises to, what `Rebuild` resets it to, and what a
flat never-opened position reports. Carrying the old average forward would be exactly the "ghost of
the closed run" FR-008 forbids. Verified: no stored holding triggers it — the three oversold holdings
end negative with no subsequent purchase.

**The trap.** Generalising the guard to "whenever the resulting quantity is zero, zero the average"
would also fire on the ordinary sell-to-flat path and change the displayed average price of ~128 flat
historic holdings — directly violating FR-006 and SC-002's "exactly two and no others".
`TransactionsTests.Add_Sell_DecreasesQuantityAndKeepsAveragePrice` pins today's behaviour and must
pass unmodified, which it does because the surgical guard never touches the Sell path.

---

## R15 — Testing strategy, and a correction to the coverage premise

**The blocking floor is 90%, not 95%.** `.github/actions/coverage-gate/action.yml` bands green (100),
yellow (95–99.99), amber (90–94.99), red (<90) and fails **only on red**. 95–99.99% is an accepted
band that asks the PR to note what is uncovered. This was stated as "95% enforced" earlier in this
work and is corrected here and in `plan.md`, because the wrong figure would rule out designs the
repository actually accepts.

**`Tools/*` is not wildcard-excluded from coverage.** `coverlet.runsettings` enumerates assemblies by
name, and the file is self-documenting by convention — every entry has a rationale comment. A new
`Tools/` project therefore needs an explicit entry *and* an added rationale line, and still needs
tests: the guide is explicit that the excluded tools are "excluded from the coverage gate … but not
from the requirement to be tested". `Tests/Financial.InvestmentSpreadsheetImport.Tests` is the shape
to copy — plain `net10.0` xUnit, one `ProjectReference` to the tool, hand-written stubs, registered in
`Financial.slnx`.

**Cheaper alternative, recommended**: put the report's logic in `Financial.Investment.Application`
where it is measured and where `StubInvestmentRepository` already exists, and make the `Tools/`
project a thin `Program.cs` that resolves and prints. Then the excluded surface is a printer, not the
rule.

**Per-layer requirements** follow the `testing-guide-Financial` skill: Domain rules get unit tests
only (a test per `throw`, boundary values, no doubles); Application services get unit tests covering
every branch **plus the observability contract** (success span with `OperationResult == Success`;
failure records the exception and rethrows *without logging*, asserted via `RecordingTelemetryTracer`)
plus integration through the API host; WPF view models get the full state matrix including
server-error, asserting entered data is preserved, and select through `TreeNodeViewModel.IsSelected`
never by assigning `SelectedNode`; React hooks mock only `financialApiClient`. Every feature also gets
AC-tracing integration tests, and **negative criteria get their own tagged test** rather than being
implied.

**No existing test asserts the two changed holdings' figures.** Every `Bitcoin`/`AGNC` hit across the
test tree is a synthetic fixture reusing the name; no test project reads `data/data-investment.json`.
So nothing needs updating — but **FR-006/SC-002 is currently unverified by anything**, and needs a new
test built from a copy of the real file or a fixture reproducing the two same-date shapes with the
exact before/after values.

**`TransactionsTests` needs zero changes.** All 15 methods use ascending dates with no same-date
buy/sell pair and none constructs an oversell. That all-green result is the Principle V signal worth
having: it means the change is genuinely additive rather than a redefinition. If a reviewer finds
themselves relaxing an assertion in that file, the design has gone wrong.

---

## R16 — A latent WPF crash this feature makes reachable

**Finding.** `Financial.App/ViewModels/Investment/TransactionsTabViewModel.cs` has **no `try`/`catch`
at all** in `Add`, `Update` or `Delete`, and `App.xaml.cs` registers no
`DispatcherUnhandledException` handler. WPF composes Application in-process, so the
`InvestmentRuleViolationException` this feature introduces would propagate out of an `async void`
command handler and **crash the desktop application**.

The web side is already correct: `ApiError.message` carries the ProblemDetails `detail` into
`saveError`, rendered in a `MessageBar`.

**Decision.** Fix it in the same increment as the rule, following the existing precedent in
`MainNavigationViewModelBase` — catch in `Add`/`Update`/`Delete` and surface `ex.Message` for
`InvestmentRuleViolationException`, keeping a generic message for everything else. Without it FR-062
("the same wording and the same reason in both front ends") fails outright and SC-003 is unreachable
from the desktop.

**This is a pre-existing defect that this feature makes reachable**, not one it introduces — worth
recording because it is exactly the kind of thing that would otherwise surface as a crash during
manual verification rather than as a planned task. Whether a global handler should also be added is
left open.

---

## Spec amendments this research forced

| Requirement | Change | Why |
|---|---|---|
| **FR-032** | "on or before" → **"strictly before"** the valuation date | The original formula contradicted its own worked example: on a Monday it marked every Friday price stale. |
| **FR-037 / FR-075 (new)** | Unrealised gain is **not reported at all** in Historic Investments | FR-034 fixes historic market value at nought, so `market − cost` would fabricate a ~2,000 loss on the one historic holding that still carries 28 units. A closed position is measured by what it realised. |
| **FR-049 / SC-011** | Yield denominator **repointed** at the corrected invested amount, no longer frozen | Confirmed with the user. The frozen denominator is **negative** (−683.09) for one holding, inverting its yield's sign. Still measured on cost — FR-049's intent was to prevent a move to *market* value, which this does not do. Four holdings' percentages change, one flipping sign. |
| **FR-006** | Qualified: the split between the two holdings holds **at displayed precision** | At full stored precision both average prices move; one rounds to the same displayed value either way. A test must state its precision or it reads as a contradiction. |
| **FR-005** | Names **two** persisted derived figures, not one | `Portfolio.IsEmpty` is written to every portfolio for the same reason `PositionType` is written to every holding. Both are computed properties with no setter, so neither is ever read back — which is what makes removing them safe in both directions. |

## Open items carried into Phase 1

- **Where the FR-047/FR-048 shortfall disclosure lives on the wire.** The portfolio-assets endpoint
  returns a bare array with nowhere to hang a portfolio-level statement. Not resolvable until the
  valuation increment defines "unvaluable"; may force the weight increment into two PRs.
- **`MarketValue` for a genuinely empty portfolio** — `0m` with `HoldingCount == 0` is proposed;
  FR-041 only requires the two states be distinguishable, which `HoldingCount` achieves either way.
- **WPF resolves no valuation service directly.** Valuation arrives precomputed on the summary DTOs,
  so no view model needs `IHoldingValuationService`. Cleaner than the `IProfitCalculationService`
  precedent, but worth noting that "in-process via DI" is satisfied through the summary services.
- **A bulk price fetch must trigger a summary refetch** (FR-035, FR-063). The DTO is computed before
  the fetch writes the new price, so both front ends need that wiring in the UI increment.
