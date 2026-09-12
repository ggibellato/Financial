# Research: Valuation Methods and Provenance

All items below were open technical-design questions after the spec was finalized (no
`NEEDS CLARIFICATION` markers remain in `spec.md` — these are implementation-level decisions the
plan phase needs before data-model/contracts can be written). Three of the spec's own clarifications
already settled the naming, source-granularity, and per-snapshot-method questions; the rest were
resolved here by reading the actual code paths this feature touches.

## 1. `Asset.PriceHistory` → `Asset.PriceSnapshots` rename (spec Clarifications, Key Entities)

**Decision**: Rename the property, backing field, and private setter method on
`Financial.Investment.Domain/Entities/Asset.cs` (`_priceHistory`/`PriceHistory`/`SetPriceHistory` →
`_priceSnapshots`/`PriceSnapshots`/`SetPriceSnapshots`). Update every Domain-entity call site that
reads it: `NavigationService.cs:93` (`asset.PriceHistory` → `asset.PriceSnapshots`) and
`DataQualityReportService.cs:46` (`h.Asset.PriceHistory.Count` → `h.Asset.PriceSnapshots.Count`).
`AssetDetailsDTO.PriceHistory` (the API wire field) and every React/WPF name (`usePriceHistory.ts`,
`PriceHistoryTab.tsx`, `PriceHistoryTabViewModel.cs`, `AssetPriceHistoryService`) are **unchanged** —
confirmed by grep that every other `.PriceHistory` reference in the codebase is on the DTO/ViewModel
side, never the Domain entity, so this rename has zero blast radius outside `Asset.cs` +
`NavigationService.cs` + `DataQualityReportService.cs` + their direct unit tests.

**Migration**: `Asset` serializes directly to JSON via `InvestmentTypeInfoResolver` (no persistence
DTO), so the JSON key literally is `"PriceHistory"` today. Add a migration step to
`InvestmentDataMigrations.Apply` (bump `CurrentVersion` from 2 to 3): for every asset object under
`ActiveBrokers`/`HistoricBrokers`/.../`Assets`, rename the `"PriceHistory"` JSON property to
`"PriceSnapshots"` if present. This reuses the exact tree-walk `RenameCreditType`/`EnumerateCredits`
already do, generalized into an `EnumerateAssets(root, brokerGroupName)` helper both steps share
(removes duplication rather than adding a new abstraction).

**Rationale**: Confirmed with the user directly — `PriceHistory` collided in review with the
unrelated Active/Historic filing concept (`Investments.ActiveBrokers`/`HistoricBrokers`). Scoping the
rename to Domain-only avoids an OpenAPI/generated-TS-types/React/WPF ripple for a pure internal
naming fix.

## 2. Snapshot source: which provider, not just "automatic vs. manual"

**Decision**: `AssetValueSnapshot` (`Financial.Investment.Domain/ValueObjects/AssetValueSnapshot.cs`,
today `record(string Ticker, string Name, decimal Price, DateTimeOffset AsOf)`) gains a `Source`
field. Every concrete `IFinanceService` stamps its own identity into the snapshot it returns
(`GoogleFinanceService`, `YahooFinanceService`, `StatusInvestFinanceService`,
`DicionarioDoInvestidorFinanceService`, `RedentiaFinanceService`) — confirmed by reading
`InvestmentInfrastructureServiceCollectionExtensions.cs:32-58` that these are the five actual named
providers, wired as two independent fallback chains (`Google→Yahoo` for standard/crypto lookups,
`StatusInvest→(DicionarioDoInvestidor→Redentia)` for bonds), not the "Google→Yahoo→StatusInvest"
single chain the roadmap loosely described. `FallbackFinanceService` needs **no change** — it already
returns whichever inner snapshot succeeded untouched; the source information rides through it for
free once the concrete services stamp themselves.

`AssetPriceSnapshot.Source` (Domain) becomes a closed enum:
`Unknown = 0, Manual, ProviderValuation, Google, Yahoo, StatusInvest, DicionarioDoInvestidor, Redentia`.
`IsManual` (FR-009) becomes a computed property: `Source == PriceSource.Manual`.

**Rationale**: Satisfies G10's actual stated goal ("detecting provider corrections") — a coarse
Automatic/Manual flag can't distinguish a correction from a different provider from a repeated read
of the same one.

## 3. `Asset.ValuationMethod`: representation and default behavior (FR-001–FR-003)

**Decision**: Add `ValuationMethod` as a **non-nullable enum with an explicit zero-value default**,
matching the existing convention `GlobalAssetClass`/`CountryCode` already use (`Unknown = 0`) rather
than a nullable field:

```
enum ValuationMethod { Unspecified = 0, MarketPrice, NAV, ProviderValue, Manual, BondQuote }
```

`Unspecified` (default for every asset that exists today, and for any new asset that doesn't set it)
means "fall back to today's `GlobalAssetClass`-based fetcher routing, unchanged" — this is exactly
what FR-003 requires ("a holding with no explicit valuation method resolved... MUST continue to
resolve through today's default routing"), and it means **zero migration is needed for this field**:
a JSON row missing `ValuationMethod` deserializes to the enum default `0` (`Unspecified`)
automatically, the same "new field defaults to CLR default on old rows" pattern P47 already
established (`specs/004-transaction-income-vocabulary/research.md` #5) for `Transaction.Withheld`/
`Credit.Withheld`.

**Rationale for non-nullable over nullable**: consistency with `GlobalAssetClass.Unknown`/
`CountryCode.Unknown` — this codebase's established idiom for "unclassified," not a new pattern.

## 4. Fetcher routing: valuation method takes priority over asset class (FR-002, User Story 3)

**Decision**: Widen `IAssetPriceFetcher.Supports` from `bool Supports(GlobalAssetClass assetClass)`
to `bool Supports(GlobalAssetClass assetClass, ValuationMethod valuationMethod)`:

- `StandardAssetPriceFetcher`: `valuationMethod is ValuationMethod.MarketPrice or ValuationMethod.NAV`
  **or** (`valuationMethod == ValuationMethod.Unspecified` **and** today's `ExchangeListedClasses`
  check) — unchanged behavior for every asset that hasn't set an explicit method.
- `BondAssetPriceFetcher`: `valuationMethod == ValuationMethod.BondQuote` **or**
  (`valuationMethod == ValuationMethod.Unspecified` **and** `assetClass == GlobalAssetClass.Bond`) —
  this is what fixes the concrete G11 tail case: an `Unknown`-class bond explicitly marked
  `BondQuote` now routes correctly even though its `GlobalAssetClass` never changes.
- `CryptocurrencyAssetPriceFetcher`: unchanged (`assetClass == GlobalAssetClass.Cryptocurrency`) —
  there is no distinct "crypto" valuation method; a crypto holding is priced via `MarketPrice`/
  `Unspecified` exactly as today.

`ProviderValue` and `Manual` methods are **never** routed to a fetcher at all (FR-007) — handled one
layer up, in `AssetPriceLookupService`, not by adding a no-op fetcher (decision #6).

**Rationale**: Minimal, additive change to the one method every existing fetcher already implements;
`Unspecified` preserves current behavior bit-for-bit, so the ~90 `GlobalAssetClass.Unknown` holdings
(roadmap G11, decision D5: no mandatory backfill) need no change to keep working exactly as today.

## 5. Provider-valued/manual holdings: reuse `AssetPriceSnapshot`, not a new entity (FR-004, FR-005)

**Decision**: The spec's Key Entities list a "Provider/manual valuation record" as if it might be a
new concept — it is not. `AssetPriceSnapshot.Price` (soon `AssetPriceSnapshot`'s recorded figure) *is*
the total value for a `ProviderValue`/`Manual`-method holding; the same `SetPrice`/`GetMostRecentPrice`/
`GetPriceAsOf` machinery records and reads it. What changes is only how `HoldingValuationCalculator`
interprets it:

- `ValuationMethod` is `MarketPrice`, `NAV`, `BondQuote`, or `Unspecified`: `MarketValue = quantity *
  snapshot.Price` (today's formula, unchanged).
- `ValuationMethod` is `ProviderValue` or `Manual`: `MarketValue = snapshot.Price` directly — the
  recorded figure already *is* the holding's total worth; quantity plays no role (FR-005). A holding
  using either of these methods needs no non-zero `Quantity` at all — it can stay at the `0` that
  `CapitalCall`/`ReturnOfCapital` transactions already leave it at (P47), closing the loop the roadmap
  called "no unit-free contribution" (G8) together with the P47 vocabulary and this feature's
  valuation side.

**Refined during implementation**: `HoldingValuationCalculator.Calculate` needs no new parameter for
this — it already receives the `AssetPriceSnapshot` being valued, and that snapshot already carries
its own `ValuationMethod` (decision #7). The branch reads `price.ValuationMethod` directly rather than
threading a separate `Asset.ValuationMethod` argument through `HoldingValuationService.GetValuation`
and `Calculate`'s signature — simpler, and automatically correct for a snapshot recorded under a
*previous* method after the holding's current method has since changed (exactly what decision #7 was
for).

**Rationale**: Avoids inventing a parallel "valuation record" entity/table/endpoint. `AssetPriceSnapshot`
already has everything needed (a date, a recorded figure, provenance fields from decision #2); only the
*interpretation* of `Price` at read time depends on `ValuationMethod`, exactly the kind of branch
`HoldingValuationCalculator.Calculate` already is.

## 6. `AssetPriceSnapshot.Price` zero-value validation (FR-006)

**Decision**: `AssetPriceSnapshot.Create`'s `ValidatePrice` (`price <= 0` throws, today unconditional)
becomes conditional on the valuation method the snapshot is recorded under: `MarketPrice`/`NAV`/
`BondQuote`/`Unspecified` keep the existing `price <= 0` rejection (a market price of zero or less is
never valid); `ProviderValue`/`Manual` reject only `price < 0` — `0` becomes valid and distinct from
"nothing recorded yet" (a write-down to nothing, FR-006). `Create` gains a `ValuationMethod` parameter
to make this decision (also needed regardless, per decision #7 below).

**Rationale**: Directly required by FR-006; a single existing validation method already gates every
snapshot write, so this is a one-method change plus a new required constructor argument.

## 7. Valuation method captured per snapshot, independent of `Asset.ValuationMethod` (spec Clarifications)

**Decision**: `AssetPriceSnapshot` gains its own `ValuationMethod` field (the method active when
*this* snapshot was recorded), separate from `Asset.ValuationMethod` (the holding's *current*
classification). `Asset.SetPrice(date, price, isManual, ...)` is extended to pass the asset's current
`ValuationMethod` through to `AssetPriceSnapshot.Create` at the moment of recording. Changing
`Asset.ValuationMethod` later never rewrites existing snapshots (FR-011) because each one already
carries its own copy from when it was written.

**Rationale**: Already settled in the spec's own clarification session — restated here because it
drives the `Create`/`SetPrice` signature change decisions #6 and #9 depend on.

## 8. "Market status" is computed at read time, not stored per snapshot (FR-008, FR-013, FR-014)

**Decision**: Despite FR-008's literal wording ("every snapshot MUST capture... a market status"),
implement market status (`Current` / `Stale` / `Unavailable`) as a **derived** value computed by
`HoldingValuationCalculator` from `(snapshot.Date, snapshot.RetrievedAt, valuationDate)` at read time —
never persisted as a raw field on `AssetPriceSnapshot` itself. `HoldingValuationCalculator` already
computes exactly this kind of thing today: `HoldingValuation.IsPriceStale` is
`price.Date < PreviousWeekday(valuationDate)` (`HoldingValuationCalculator.cs:36`). This feature widens
that single `bool` into a three-value `MarketStatus` enum (`Current`/`Stale`/`Unavailable`), computed
the same way, plus `Unavailable` for the case `HoldingValuationCalculator.Calculate` currently returns
`price is null` (see decision #10) or the case `AssetPriceLookupService.FetchWithPriceHistoryFallback`
currently `throw`s when there is no fallback snapshot at all (decision #10).

**Rationale**: A snapshot's own staleness is inherently relative to "now," not a fixed fact about when
it was written — storing it would make it wrong the moment time passes, exactly the anti-pattern G10
already flagged for `Asset.PositionType` ("persisted despite being derived purely from quantity...
can and does disagree with the transactions beneath it") and that `InvestmentTypeInfoResolver`'s
`ExcludedProperties` list already exists to prevent for `AveragePrice`/`Quantity`/`PositionType`/
`NetCash`/`NetAmount`. This is the same "derive, don't store" discipline applied to a new field before
it ships wrong, not a deviation invented for this feature.

## 9. Currency default for a fetched snapshot (FR-008)

**Decision**: `AssetValueSnapshot`/`GoogleFinanceService` (confirmed by reading it) never captures a
currency from the scraped source today — `WebPageParserMappers.ToAssetValueSnapshot` maps only
`Ticker`/`Name`/`Price`/`AsOf`. Default a fetched snapshot's `Currency` to the asset's `Broker.Currency`
(already a per-broker field). This is a known, bounded approximation — a US-listed holding in a UK GBP
account would show `GBP` provenance rather than the `USD` it's actually quoted in — accepted because
the spec's own Assumptions already scope currency here as "provenance only, not conversion" (Wave 3/P49
owns precision), and a `Manual`/`ProviderValue` snapshot lets the investor pick the correct currency
explicitly at entry time regardless.

**Rationale**: The alternative (teaching each scraper to parse a currency out of the page) is real new
scraping work with no consumer in this feature (nothing here converts currency) — deferred to whichever
of P48/P49 actually needs currency precision, not invented speculatively now (Constitution Principle IV).

## 10. `Unavailable` status requires `AssetPriceLookupService` to stop throwing on total failure (FR-014)

**Found while reading the code**: `AssetPriceLookupService.FetchWithPriceHistoryFallback` today
`throw`s the original fetch exception when there is no prior snapshot to fall back to
(`AssetPriceLookupService.cs:127-131`) — a genuinely priceless holding surfaces today as an
unhandled error, not a graceful state. `HoldingValuationCalculator.Calculate` similarly returns
`MarketValue: null` when `price is null`, which today's front ends render as a blank cell, not an
explicit "Unavailable" status.

**Decision**: `GetCurrentPriceAsync` catches this specific "nothing to fall back to" case and returns
an `AssetPriceDTO` describing unavailability (no price, `MarketStatus = Unavailable`) instead of
letting the exception propagate; `HoldingValuationCalculator.Calculate`'s `price is null` branch maps
to `MarketStatus = Unavailable` in the widened `HoldingValuation` record.

**Rationale**: Directly required by User Story 2 Scenario 3 / FR-014 — a stale-data warning UI needs
something to render instead of an unhandled error.

## 11. `Asset.ValuationMethod`/`Asset.IncomePolicy` as mutators, not constructor parameters

**Found while reading the code**: `Asset.Create` has three overloads and `grep` confirms 24 files
call one of them (mostly test fixtures, plus `AssetAdminService.cs` and
`Tools/InvestmentSpreadsheetImport/GoogleGenerator.cs`). Adding two more required constructor
parameters would force a mechanical edit across all 24.

**Decision**: Add `Asset.SetValuationMethod(ValuationMethod)` and `Asset.SetIncomePolicy(IncomePolicy)`
as small dedicated mutators (mirroring `SetPrice`), leaving every `Create` overload's signature
untouched. Both default to their enum's `0` value (`Unspecified`/`Unknown`) when never called — no
existing call site needs to change, and no migration is needed for either field (same reasoning as
decision #3).

**Rationale**: Confirmed by the actual call-site count that adding constructor parameters would be a
disproportionate, unrelated-file-touching change for two optional classification fields — the
project's own "don't over-engineer for scale/complexity that isn't needed" principle (Constitution
Principle IV), backed here by a concrete grep count rather than a general appeal to it.

## 12. `Asset.IncomePolicy` values (FR-010)

**Decision**: `enum IncomePolicy { Unknown = 0, Distributing, Accumulating }` — same "explicit zero
default" idiom as decision #3. Consuming this classification in a data-quality warning or dashboard
signal is Wave 6 (P52, spec Assumptions) — out of scope here; this feature only records and displays
it (Admin CRUD create/update forms gain a field, `AssetDetailsDTO`/asset detail views display it
read-only).

**Rationale**: Matches the spec's own explicit scope boundary; no design ambiguity beyond the enum
shape decision #3 already established as this codebase's convention.

## 13. Admin CRUD: `AssetAdminCreateDTO`/`AssetAdminUpdateDTO` mirror the existing `Class` pattern

**Decision**: Both DTOs gain `ValuationMethod? ValuationMethod` and `IncomePolicy? IncomePolicy` —
nullable at the wire level, mirroring exactly how `Class` is already `GlobalAssetClass?` on both DTOs
today ("left null to auto-resolve... set to override explicitly"). `AssetAdminService` calls
`created.SetValuationMethod(request.ValuationMethod ?? ValuationMethod.Unspecified)` /
`SetIncomePolicy(request.IncomePolicy ?? IncomePolicy.Unknown)` after `Asset.Create` (decision #11).

**Rationale**: Reuses an established DTO idiom in this exact codebase rather than inventing a new one
for two conceptually similar new fields.

## 14. `MarketStatus` widening touches `HoldingValuation`/`AssetDetailsDTO`/`PortfolioAssetSummaryItemDTO`,
    not just `AssetPriceSnapshotDTO`

**Decision**: `HoldingValuation.IsPriceStale: bool` is widened to `MarketStatus: MarketStatus`
(`Current`/`Stale`/`Unavailable`, decision #8). Every consumer that reads `IsPriceStale` today
(`AssetDetailsDTO.IsPriceStale`, `PortfolioAssetSummaryItemDTO.IsPriceStale`, and their React/WPF
renderers) is widened to `MarketStatus` at the same time — this is the same shape of change P47 made
to `TotalReturn`→`+TotalReturnNetOfTax` (additive DTO field, both front ends updated together per
Constitution Principle III), not a breaking rename since the new field fully subsumes what the old
boolean showed (`Stale` ⟺ old `true`, `Current`/`Unavailable` split what old `false` conflated).

**Rationale**: `IsPriceStale` cannot stay side-by-side with `MarketStatus` without them being able to
disagree — one derived field for one concept, consistent with decision #8's "derive, don't store"
framing applied one level up (don't derive-and-store the same fact twice in two shapes either).

## 15. `Asset.SetPrice` keeps a simple overload; full provenance is a second, additive one

**Found during task planning**: `grep` for `.SetPrice(` finds **60+ call sites** across the test
suite (`AssetTests.cs`, `HoldingValuationServiceTests.cs`, `PortfolioAssetSummaryServiceTests.cs`,
`SummaryServiceTests.cs`, `NavigationServiceTests.cs`, `DataQualityReportServiceTests.cs`, and more)
— overwhelmingly incidental test-fixture setup ("give this asset a price so its valuation isn't
null"), not tests of `SetPrice`/provenance behavior itself. Forcing every one of these to supply
`Currency`/`Source`/`SourceReference`/`RetrievedAt` would be the same disproportionate,
unrelated-file-touching change decision #11 already rejected for `Asset.Create` — just one layer
down. `AssetPriceSnapshot.Create` itself, by contrast, has only ~12 direct call sites (`Asset.cs` and
two Domain test files already exercising this exact entity), so widening *that* signature directly is
proportionate and already decision #6/#7's plan.

**Decision**: `Asset.SetPrice(DateOnly date, decimal price, bool isManual)` **stays**, unchanged in
signature, as the simple path — internally it now maps `isManual` to `PriceSource.Manual`/
`PriceSource.Unknown`, uses `this.ValuationMethod` for the snapshot's method, defaults `Currency` to
the asset's broker currency (decision #9) and `RetrievedAt` to `DateTimeOffset.UtcNow`, `SourceReference`
to `null`. A new, explicit overload — `Asset.SetPrice(DateOnly date, decimal price, PriceSource source,
string currency, string? sourceReference, DateTimeOffset retrievedAt)` — is added for the two call
sites that actually need to supply real provenance: `AssetPriceHistoryService.SetPriceAsync` (a manual
or provider-valuation entry, decision #7) and `AssetPriceLookupService.RecordAutomaticPriceIfNeededAsync`
(an automatic fetch, stamping the named provider from decision #2). Both overloads funnel into the
same private `UpsertPriceEntry`/`AssetPriceSnapshot.Create` path — no duplicated validation.

**Rationale**: Keeps ~60 unrelated tests compiling unchanged, exactly the "don't force a mechanical
edit across many unrelated files for an optional capability" principle decision #11 already
established — applied here to the second layer (`SetPrice`) that the same call-site-count evidence
now also applies to.

## 16. Technical Context resolution (no NEEDS CLARIFICATION remain)

| Field | Value |
|---|---|
| Language/Version | C# / .NET 10 (`Financial.Investment.*`, `Financial.Api`, `Financial.App`); TypeScript 5 / React 18 (`Financial.Web`) |
| Primary Dependencies | ASP.NET Core (API); `System.Text.Json` + `InvestmentTypeInfoResolver` (persistence); xUnit + FluentAssertions (no mocking — `Financial.TestUtilities` fakes); Vite + Vitest + React Testing Library + Playwright (`Financial.Web`); existing `IFinanceService` chain (Google/Yahoo/StatusInvest/DicionarioDoInvestidor/Redentia) — no new external dependency |
| Storage | Single JSON document `data/data-investment.json`, `LocalJson`/`GoogleDrive` provider (unchanged), loaded once at process startup |
| Testing | `dotnet test --settings coverlet.runsettings` (unit + `WebApplicationFactory` integration + `OpenApiContractTests` snapshot); `npm run lint && npm test && npm run build` (`Financial.Web`); `npm run smoke-test` (Playwright, CI-gated) |
| Target Platform | Docker/Linux container (API + built SPA); Windows desktop (`Financial.App`, in-process, not an HTTP client) |
| Project Type | Web application (ASP.NET Core API + React SPA) plus a WPF desktop client sharing backend layers in-process — matches the existing repository structure; no new project needed |
| Performance Goals | N/A — single-user, self-hosted tool (Constitution Principle IV); the version-3 migration must complete in well under a second against current data volume, consistent with every migration already in this codebase |
| Constraints | JSON loaded once at startup (restart required after migration); full-document rewrite on every save; OpenAPI snapshot + generated TS types MUST be regenerated in the same PR as any DTO shape change; both front ends MUST ship each user story's increment together (Wave 0/Wave 1 precedent) |
| Scale/Scope | 160 assets / ~62+ price snapshots at spec-writing time (illustrative, volatile — roadmap's own stated warning); 5 valuation methods; 8 price sources (`Unknown`, `Manual`, `ProviderValuation`, + 5 named providers); 3 market statuses |
