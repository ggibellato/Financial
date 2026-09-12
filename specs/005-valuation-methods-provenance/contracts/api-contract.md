# API Contract Changes: Valuation Methods and Provenance

`Financial.Api` DTOs are the literal wire format (Constitution: Technology & Persistence
Constraints). Every change below is a wire-format change and MUST regenerate the OpenAPI snapshot
(`Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json`) and `Financial.Web`'s generated TS
types in the same PR.

## `AssetPriceSnapshotDTO` (read model, part of `AssetDetailsDTO.PriceSnapshots`)

`Financial.Investment.Application/DTOs/AssetPriceDTOs.cs`

| Field | Change |
|---|---|
| `Date`, `Price` | Unchanged shape. |
| `IsManual` | Unchanged wire shape (`bool`), now server-computed from `Source` (`data-model.md`) rather than a stored flag — no consumer-visible difference. |
| `Currency` | **New** `string`. |
| `Source` | **New** `string` — one of `Unknown, Manual, ProviderValuation, Google, Yahoo, StatusInvest, DicionarioDoInvestidor, Redentia`. |
| `SourceReference` | **New** nullable `string`. |
| `ValuationMethod` | **New** `string` — one of `Unspecified, MarketPrice, NAV, ProviderValue, Manual, BondQuote`; the method active when this specific snapshot was recorded (FR-011). |
| `RetrievedAt` | **New** `DateTimeOffset`. |

`AssetDetailsDTO.PriceHistory` → **`AssetDetailsDTO.PriceSnapshots`** (rename, matching
`data-model.md`'s `Asset.PriceSnapshots`). This is a breaking wire-format rename, not additive —
`Financial.Web`'s `usePriceHistory.ts`/`PriceHistoryTab.tsx` and `Financial.App`'s
`PriceHistoryTabViewModel`/`AssetDetailsViewModel` call sites reading `.priceHistory`/`.PriceHistory`
off this DTO update to `.priceSnapshots`/`.PriceSnapshots` in the same PR (the component/ViewModel
*names* themselves — `PriceHistoryTab`, `usePriceHistory`, `PriceHistoryTabViewModel` — are
unaffected per the spec's Clarifications: only the Domain property and this one DTO field rename;
UI/feature names stay as-is).

## `SetAssetPriceDTO` (write model)

`Financial.Investment.Application/DTOs/AssetPriceDTOs.cs`

| Field | Change |
|---|---|
| `BrokerName`, `PortfolioName`, `AssetName`, `Date`, `Price` | Unchanged. `Price <= 0` server-side rejection relaxes to `Price < 0` when the target asset's `ValuationMethod` is `ProviderValue`/`Manual` (FR-006, `data-model.md`). |
| `Currency` | **New**, optional `string?` — defaults to the asset's broker currency when omitted (research.md #9). |
| `SourceReference` | **New**, optional nullable `string`. |

`Source` and `ValuationMethod` are **not** client-supplied fields on this DTO — the server derives
`Source` (`Manual` if the asset's `ValuationMethod` is `MarketPrice`/`NAV`/`BondQuote`/`Unspecified`;
`ProviderValuation` if the asset's `ValuationMethod` is `ProviderValue`) and copies the asset's
current `ValuationMethod` onto the new snapshot at write time (`AssetPriceHistoryService.SetPriceAsync`
already resolves the asset before calling `Asset.SetPrice`, so this is a same-method addition, not a
new round trip).

## `AssetPriceDTO` (live-fetch response)

`Financial.Investment.Application/DTOs/AssetPriceDTO.cs`

| Field | Change |
|---|---|
| `Exchange`, `Ticker`, `Name`, `Price`, `AsOf`, `AsOfDate` | Unchanged. |
| `IsManual` | Unchanged wire shape, now derived the same way as `AssetPriceSnapshotDTO.IsManual`. |
| `Source` | **New** `string` — populated from `AssetValueSnapshot.Source` (`data-model.md`) for a live fetch, or from the stored snapshot's `Source` for the fallback-to-history path. |
| `MarketStatus` | **New** `string` — `Current`/`Stale`/`Unavailable` (research.md #8, #10). Replaces the implicit "no price at all" exception path (research.md #10): `GetCurrentPrice` returns `200 OK` with `MarketStatus: "Unavailable"` and no `Price` instead of the request failing, when there is no fetched price and no fallback snapshot to use. |

`GET /prices/current` gains a `valuationMethod` optional query parameter (parsed the same
`Enum.TryParse<ValuationMethod>(..., ignoreCase: true)` way `assetClass` already is in
`AssetPricesController.GetCurrentPrice`), passed through to `AssetPriceRequestDTO.ValuationMethod` so
`AssetPriceService`/`IAssetPriceFetcher.Supports` (data-model.md) can route by it. When
`valuationMethod` resolves to `ProviderValue`/`Manual`, `AssetPriceLookupService` returns the most
recent stored snapshot directly and never calls a fetcher (data-model.md), the same short-circuit
already used for "manual price recorded for today," generalized to any date for these two methods.

## `AssetPriceRequestDTO` (Application, not wire — internal contract between `AssetPriceLookupService` and `AssetPriceService`)

`Financial.Investment.Application/DTOs/AssetPriceRequestDTO.cs`

| Field | Change |
|---|---|
| `Exchange`, `Ticker`, `AssetClass`, `BrokerName`, `Name`, `PortfolioName`, `AssetName` | Unchanged. |
| `ValuationMethod` | **New** `ValuationMethod`, default `Unspecified` — populated from `asset.ValuationMethod` in `AssetPriceLookupService.DescribeWith` (same place `AssetClass` is already populated from `asset.Class`, `AssetPriceLookupService.cs:100-110`). |

## `AssetAdminCreateDTO` / `AssetAdminUpdateDTO` / `AssetAdminDTO`

`Financial.Investment.Application/DTOs/AssetAdmin*.cs`

| Field | Change |
|---|---|
| `ValuationMethod` | **New**, nullable on Create/Update (`ValuationMethod?` — `null` auto-resolves to `Unspecified`, mirroring `Class`'s existing nullable-override convention); non-nullable, always-present on `AssetAdminDTO` (read model). |
| `IncomePolicy` | **New**, nullable on Create/Update (`IncomePolicy?` — `null` auto-resolves to `Unknown`); non-nullable, always-present on `AssetAdminDTO`. |

`Financial.Web`'s Admin asset create/edit form (wherever it consumes `AssetAdminCreateDTO`/
`AssetAdminUpdateDTO`) and `Financial.App`'s `Financial.App/ViewModels/Admin/AssetsViewModel.cs`
both gain the two new fields as form inputs, defaulting to unset/auto (FR-001, FR-010).

## `HoldingValuation` consumers: `AssetDetailsDTO`, `PortfolioAssetSummaryItemDTO`

`Financial.Investment.Application/DTOs/AssetDetailsDTO.cs`,
`Financial.Investment.Application/DTOs/PortfolioAssetSummaryItemDTO.cs`

| Field | Change |
|---|---|
| `IsPriceStale` | **Removed**, replaced by `MarketStatus` (`string`, `Current`/`Stale`/`Unavailable`) on both DTOs (research.md #8, #14). This is a breaking rename, not additive — every reader of `isPriceStale`/`IsPriceStale` in `Financial.Web` and `Financial.App` moves to `marketStatus`/`MarketStatus` in the same PR (both front ends ship together per Constitution Principle III, same as Wave 0/Wave 1 precedent). |
| `AssetDetailsDTO.ValuationMethod`, `AssetDetailsDTO.IncomePolicy` | **New**, read-only, sourced from `Asset.ValuationMethod`/`Asset.IncomePolicy` — displayed on the asset details view (FR-001, FR-010, FR-013). |

## Regeneration checklist (per `CLAUDE.md`)

1. `UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests` (bash) or the PowerShell
   env-var form, then unset it.
2. Review the snapshot diff — expect `PriceHistory`→`PriceSnapshots` and `IsPriceStale`→`MarketStatus`
   to show as removals+additions (breaking renames), everything else as pure additions.
3. `cd Financial.Web && npm run generate-api-types`.
4. `Financial.Web/src/api/types.ts` aliases: update the two renamed field names' usages at every call
   site `tsc -b` flags (do not add a compatibility alias for either — both are intentional renames,
   not deprecations).
5. `tsc -b` (via `npm run build`) to find every call site touching `.priceHistory`/`.isPriceStale`.
