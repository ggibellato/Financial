# Data Model: Valuation Methods and Provenance

Source: `spec.md` Key Entities, Requirements FR-001–FR-015; design decisions in `research.md`.

## Asset (widened)

`Financial.Investment.Domain/Entities/Asset.cs`

| Field | Type | Change | Notes |
|---|---|---|---|
| `PriceSnapshots` *(was `PriceHistory`)* | `IReadOnlyCollection<AssetPriceSnapshot>` | **renamed** | Property, backing field (`_priceSnapshots`), and setter (`SetPriceSnapshots`) — research.md #1. Same collection, same semantics. |
| `ValuationMethod` | `ValuationMethod` (enum) | **new** | Default `Unspecified` (`= 0`) for every existing asset — no migration needed (research.md #3). Set via new `SetValuationMethod(ValuationMethod)` mutator, not a `Create` constructor parameter (research.md #11). |
| `IncomePolicy` | `IncomePolicy` (enum) | **new** | Default `Unknown` (`= 0`). Set via new `SetIncomePolicy(IncomePolicy)` mutator (research.md #11, #12). |

`SetPrice(DateOnly date, decimal price, bool isManual)` keeps its existing signature unchanged — a
new `SetPrice(DateOnly date, decimal price, PriceSource source, string currency, string?
sourceReference, DateTimeOffset retrievedAt)` overload is added alongside it for callers that supply
full provenance (research.md #15); both funnel into the same `UpsertPriceEntry`.

**Validation rules**: none beyond the enums' own closed value sets — both are optional
classifications with a defined default, not invariants tying them to `Class`/`Quantity`/anything
else (a `ValuationMethod.ProviderValue` asset with a nonzero `Quantity` is unusual but not rejected;
the roadmap's own G14 lesson — "every rule must key off which scope a holding is filed under, never
off whether its quantity happens to be zero" — argues against adding a new cross-field invariant here
that isn't required by any FR).

## ValuationMethod (new enum, Domain)

`Financial.Investment.Domain/Entities/ValuationMethod.cs` (new file, sibling to `GlobalAssetClass.cs`)

```csharp
public enum ValuationMethod
{
    Unspecified = 0,
    MarketPrice,
    NAV,
    ProviderValue,
    Manual,
    BondQuote
}
```

`Unspecified` means "no explicit method set — resolve exactly as today" (FR-003). The other four are
G8's own list from the roadmap, unchanged.

## IncomePolicy (new enum, Domain)

`Financial.Investment.Domain/Entities/IncomePolicy.cs` (new file)

```csharp
public enum IncomePolicy
{
    Unknown = 0,
    Distributing,
    Accumulating
}
```

## AssetPriceSnapshot (widened)

`Financial.Investment.Domain/Entities/AssetPriceSnapshot.cs`

| Field | Type | Change | Notes |
|---|---|---|---|
| `Date` | `DateOnly` | unchanged | The as-of date this snapshot prices. |
| `Price` | `decimal` | validation relaxed | `MarketPrice`/`NAV`/`BondQuote`/`Unspecified`: `> 0` required (unchanged). `ProviderValue`/`Manual`: `>= 0` required — `0` is a valid, distinct value (FR-006, research.md #6). For `ProviderValue`/`Manual` this figure *is* the holding's total worth, not a per-unit price (FR-005, research.md #5). |
| `IsManual` | `bool` | **removed, replaced by computed property** | `IsManual => Source == PriceSource.Manual` (FR-009, research.md #2). No longer a stored field. |
| `Currency` | `string` | **new** | Defaults to the asset's broker currency for a fetched snapshot when the source doesn't supply one (research.md #9); the investor sets it explicitly for a `Manual`/`ProviderValue` entry. |
| `Source` | `PriceSource` (enum) | **new** | `Unknown = 0, Manual, ProviderValuation, Google, Yahoo, StatusInvest, DicionarioDoInvestidor, Redentia` (research.md #2). |
| `SourceReference` | `string?` | **new** | Free-text identifier of the specific source record (e.g. the URL or provider batch id the fetch came from); `null`/empty for a manual entry with nothing to reference. |
| `ValuationMethod` | `ValuationMethod` (enum) | **new** | The method active when *this specific* snapshot was recorded — independent of `Asset.ValuationMethod`, which can change later without rewriting history (FR-011, research.md #7). |
| `RetrievedAt` | `DateTimeOffset` | **new** | When the value was actually retrieved/entered, distinct from `Date` (the date it prices). |

**Removed**: `MarketStatus` is deliberately **not** a field here — see `HoldingValuation` below
(research.md #8).

**Validation rules** (Domain, `AssetPriceSnapshot.Create`):
- `Create(DateOnly date, decimal price, ValuationMethod valuationMethod, PriceSource source, string
  currency, string? sourceReference, DateTimeOffset retrievedAt)` — `IsManual`/`isManual` parameter
  removed (derived from `source`).
- `ValidatePrice`: `price <= 0` throws when `valuationMethod is ValuationMethod.MarketPrice or
  ValuationMethod.NAV or ValuationMethod.BondQuote or ValuationMethod.Unspecified`; `price < 0` throws
  when `valuationMethod is ValuationMethod.ProviderValue or ValuationMethod.Manual` (research.md #6).
- `ValidateDate`: unchanged (`date` cannot be in the future).

**Existing-row migration** (version 3, `InvestmentDataMigrations`):
1. Rename the `Asset`-level JSON key `"PriceHistory"` → `"PriceSnapshots"` (research.md #1).
2. For every existing snapshot entry: map stored `IsManual: true` → `Source: "Manual"`; stored
   `IsManual: false` → `Source: "Unknown"` (an honest "some automatic provider, which one is lost to
   history" — not equivalent to "no data"). This is the one non-additive change; every other new
   field (`Currency`, `SourceReference`, `ValuationMethod`, `RetrievedAt`) defaults safely via CLR
   default on a missing JSON property (empty string / enum `0` / `null` / `DateTimeOffset.MinValue`
   respectively) with no explicit migration step, the same "additive field" pattern P47 established
   (`specs/004-transaction-income-vocabulary/research.md` #5).

## HoldingValuation (widened)

`Financial.Investment.Domain/Rules/HoldingValuationCalculator.cs`

| Field | Type | Change | Notes |
|---|---|---|---|
| `MarketValue` | `decimal?` | formula branches on method | `quantity * price.Price` for `MarketPrice`/`NAV`/`BondQuote`/`Unspecified`; `price.Price` directly for `ProviderValue`/`Manual` (research.md #5). |
| `IsPriceStale` | `bool` | **replaced by `MarketStatus`** | See below. |
| `MarketStatus` | `MarketStatus` (enum) | **new** | `Current` (fresh snapshot), `Stale` (`price.Date < PreviousWeekday(valuationDate)`, today's existing threshold, unchanged), `Unavailable` (no snapshot to value from at all — `price is null` today, plus the newly-graceful case from research.md #10). Computed by `HoldingValuationCalculator`, never stored (research.md #8). |
| every other field | unchanged | | `CostOfUnitsHeld`, `UnrealisedGain`, `PriceAsOfDate`, `PriceOnlyReturn`, `TotalReturn`, `TotalReturnNetOfTax` — unaffected. |

## MarketStatus (new enum, Domain)

`Financial.Investment.Domain/Rules/MarketStatus.cs` (new file, sibling to `HoldingValuationCalculator.cs`)

```csharp
public enum MarketStatus { Current, Stale, Unavailable }
```

## PriceSource (new enum, Domain)

`Financial.Investment.Domain/Entities/PriceSource.cs` (new file)

```csharp
public enum PriceSource
{
    Unknown = 0,
    Manual,
    ProviderValuation,
    Google,
    Yahoo,
    StatusInvest,
    DicionarioDoInvestidor,
    Redentia
}
```

## AssetValueSnapshot (widened, Infrastructure boundary value object)

`Financial.Investment.Domain/ValueObjects/AssetValueSnapshot.cs`

| Field | Type | Change | Notes |
|---|---|---|---|
| `Ticker`, `Name`, `Price`, `AsOf` | unchanged | | |
| `Source` | `PriceSource` | **new** | Stamped by the concrete `IFinanceService` implementation that produced it (research.md #2); `FallbackFinanceService` passes it through unchanged. |

## IAssetPriceFetcher (widened, Infrastructure interface)

`Financial.Investment.Infrastructure/Interfaces/IAssetPriceFetcher.cs`

```csharp
public interface IAssetPriceFetcher
{
    bool Supports(GlobalAssetClass assetClass, ValuationMethod valuationMethod);
    AssetValueSnapshot GetSnapshot(AssetPriceRequestDTO request);
}
```

Each implementation's `Supports` change is described in research.md #4. `ProviderValue`/`Manual`
methods never reach any `IAssetPriceFetcher` — `AssetPriceLookupService` short-circuits before
fetcher resolution (research.md #4, #5), extending the existing "manual price for today is
authoritative, no fetch is made" short-circuit (`AssetPriceLookupService.cs:48-56`) to apply
universally (any date) for these two methods, not only "today."

## Relationships (unchanged)

`Asset` 1—* `AssetPriceSnapshot` (via `PriceSnapshots`, was `PriceHistory`) — same aggregate shape,
just a renamed collection of a wider element type. No new entity, no new one-to-many relationship;
research.md #5 is explicitly why a new "valuation record" entity was rejected.

## State / lifecycle

No new lifecycle. `AssetPriceSnapshot` remains an immutable, replace-on-same-date record
(`Asset.UpsertInto`, unchanged). `Asset.ValuationMethod`/`IncomePolicy` are plain mutable
classifications with no state machine — changing `ValuationMethod` takes effect for the *next*
snapshot recorded; it never touches snapshots already on file (FR-011).
