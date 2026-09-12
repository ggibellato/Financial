---
description: "Task list for Valuation Methods and Provenance"
---

# Tasks: Valuation Methods and Provenance

**Input**: Design documents from `/specs/005-valuation-methods-provenance/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contract.md, quickstart.md

**Tests**: Included — Constitution Principle V ("No feature is complete without tests... unit tests
required, integration tests where applicable") makes tests mandatory in this codebase, not optional.

**Organization**: Tasks are grouped by user story. Unlike P47 (004), which had a strict sequential
chain, this feature's three stories are **mostly independent** after Foundational: US1 (recording a
value directly) doesn't need US2's display work or US3's routing fix to be testable via the API; US2
(provenance/staleness display) doesn't need US1's value-recording path — any holding with a stale
price demonstrates it; US3 (fetcher routing) only needs the Foundational `ValuationMethod` enum. All
three can proceed in parallel once Foundational is done.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1–US3, matching spec.md's priorities. Setup/Foundational/Polish tasks carry no story label.

## Path Conventions

Existing repository layout (no new path convention introduced): `.NET` projects flat at repo root
(`Financial.Investment.{Domain,Application,Infrastructure}`, `Financial.Api`, `Financial.App`),
`Financial.Web/src/`, one test project per layer under `Tests/`.

---

## Phase 1: Setup

- [X] T001 Verify baseline is green on branch `005-valuation-methods-provenance`: `dotnet build --configuration Release` and `dotnet test --settings coverlet.runsettings --results-directory TestResults` both pass before any change in this feature begins

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The widened Domain model, persistence, and Admin CRUD surface every user story
depends on — `AssetPriceSnapshot`'s `Create` signature change alone touches every write path.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Create `ValuationMethod` enum in `Financial.Investment.Domain/Entities/ValuationMethod.cs`: `Unspecified = 0, MarketPrice, NAV, ProviderValue, Manual, BondQuote` (data-model.md)
- [X] T003 [P] Create `IncomePolicy` enum in `Financial.Investment.Domain/Entities/IncomePolicy.cs`: `Unknown = 0, Distributing, Accumulating` (data-model.md)
- [X] T004 [P] Create `PriceSource` enum in `Financial.Investment.Domain/Entities/PriceSource.cs`: `Unknown = 0, Manual, ProviderValuation, Google, Yahoo, StatusInvest, DicionarioDoInvestidor, Redentia` (data-model.md)
- [X] T005 Rename `Asset.PriceHistory`→`PriceSnapshots` in `Financial.Investment.Domain/Entities/Asset.cs`: backing field `_priceHistory`→`_priceSnapshots`, property, and `SetPriceHistory`→`SetPriceSnapshots` setter method; add `ValuationMethod`/`IncomePolicy` properties (default `Unspecified`/`Unknown`) with `SetValuationMethod(ValuationMethod)`/`SetIncomePolicy(IncomePolicy)` mutators (research.md #1, #11) (depends on T002, T003)
- [X] T006 Widen `Financial.Investment.Domain/Entities/AssetPriceSnapshot.cs`: add `Currency` (`string`), `Source` (`PriceSource`), `SourceReference` (`string?`), `ValuationMethod` (`ValuationMethod`), `RetrievedAt` (`DateTimeOffset`); remove the stored `IsManual` field, replace with computed `IsManual => Source == PriceSource.Manual`; widen `Create(...)` to take all of the above; make `ValidatePrice` reject `<= 0` for `MarketPrice`/`NAV`/`BondQuote`/`Unspecified` and only `< 0` for `ProviderValue`/`Manual` (research.md #2, #6, #7, #9; data-model.md) (depends on T002, T004)
- [X] T007 **Adjusted during implementation**: `Asset` has no back-reference to its parent `Broker`, so "default `Currency` to the asset's broker currency" (as first planned) is not available inside `Asset.cs` itself. The simple `SetPrice(date, price, isManual)` overload now defaults `Currency` to `string.Empty` instead — still zero-touch for all ~60 existing call sites, and no story in this feature actually asserts a broker-derived default. The new `SetPrice(date, price, PriceSource source, string currency, string? sourceReference, DateTimeOffset retrievedAt)` overload was added exactly as planned; both funnel into the same `UpsertPriceEntry`/`AssetPriceSnapshot.Create` path (research.md #15)
- [X] T008 [P] Widen `Financial.Investment.Domain/ValueObjects/AssetValueSnapshot.cs`: add `Source` (`PriceSource`) (research.md #2) (depends on T004)
- [X] T009 Add `(Asset, nameof(Asset.IsManual))`-style exclusion for `AssetPriceSnapshot.IsManual` to `Financial.Investment.Infrastructure/Persistence/InvestmentTypeInfoResolver.cs`'s `ExcludedProperties` set, now that it's computed (depends on T006)
- [X] T010 In `Financial.Investment.Infrastructure/Persistence/InvestmentDataMigrations.cs`: extract an `EnumerateAssets(root, brokerGroupName)` helper from the existing asset-walk inside `EnumerateCredits`, reuse it in both; add a version-3 step (bump `CurrentVersion` to `3`) that renames each asset's `"PriceHistory"` JSON key to `"PriceSnapshots"` and maps each snapshot's `"IsManual"` bool to a `"Source"` string (`true`→`"Manual"`, `false`→`"Unknown"`) (research.md #1, #2) (depends on T005, T006)
- [X] T011 Update `Financial.Investment.Application/Services/NavigationService.cs:93`: `asset.PriceHistory`→`asset.PriceSnapshots` (depends on T005)
- [X] T012 [P] Update `Financial.Investment.Application/Services/DataQualityReportService.cs:46`: `h.Asset.PriceHistory`→`h.Asset.PriceSnapshots` (depends on T005)
- [X] T013 Widen `AssetPriceSnapshotDTO` in `Financial.Investment.Application/DTOs/AssetPriceDTOs.cs`: add `Currency`, `Source`, `SourceReference`, `ValuationMethod`, `RetrievedAt` (contracts/api-contract.md) (depends on T006)
- [X] T014 Widen `SetAssetPriceDTO` in `Financial.Investment.Application/DTOs/AssetPriceDTOs.cs`: add optional `Currency`, `SourceReference` (contracts/api-contract.md) (depends on T006)
- [X] T015 Rename `AssetDetailsDTO.PriceHistory`→`PriceSnapshots` in `Financial.Investment.Application/DTOs/AssetDetailsDTO.cs`; update `NavigationMapper.MapPriceEntry` in `Financial.Investment.Application/Services/NavigationMapper.cs` to map the widened `AssetPriceSnapshotDTO` fields (depends on T013)
- [X] T016 Widen `Financial.Investment.Application/DTOs/AssetAdminCreateDTO.cs` and `AssetAdminUpdateDTO.cs` (add nullable `ValuationMethod?`, `IncomePolicy?`) and `AssetAdminDTO.cs` (add non-nullable `ValuationMethod`, `IncomePolicy`) (contracts/api-contract.md, research.md #13) (depends on T002, T003)
- [X] T017 Update `Financial.Investment.Application/Services/AssetAdminService.cs`: `CreateAssetAsync`/`UpdateAssetAsync` call `SetValuationMethod(request.ValuationMethod ?? ValuationMethod.Unspecified)`/`SetIncomePolicy(request.IncomePolicy ?? IncomePolicy.Unknown)`; `ToDto` maps both onto `AssetAdminDTO` (depends on T005, T016)

### Foundational tests

- [X] T018 [P] Update `Tests/Financial.Investment.Domain.Tests/Domain/AssetTests.cs`: rename `.PriceHistory`→`.PriceSnapshots` assertions; add cases for `SetValuationMethod`/`SetIncomePolicy` defaults and explicit sets (depends on T005, T007)
- [X] T019 [P] Update `Tests/Financial.Investment.Domain.Tests/Domain/BrokerTests.cs`: rename `.PriceHistory`→`.PriceSnapshots` count assertions (depends on T005)
- [X] T020 [P] Extend `Tests/Financial.Investment.Domain.Tests/Domain/AssetPriceSnapshotTests.cs` for the widened `Create` signature and the conditional zero-price validation (`ProviderValue`/`Manual` accept `0`, reject `< 0`; other methods keep rejecting `<= 0`) (depends on T006)
- [X] T021 [P] Update `Tests/Financial.Investment.Infrastructure.Tests/Persistence/InvestmentTypeInfoResolverTests.cs`: rename `GetTypeInfo_RoundTripsAssetWithPriceHistory_PreservesEntries`/`GetTypeInfo_DeserializesAssetJsonWithoutPriceHistoryProperty_LoadsAsEmptyCollection` and their JSON literals to `PriceSnapshots` (depends on T005, T006, T009)
- [X] T022 [P] Extend `Tests/Financial.Investment.Infrastructure.Tests/Persistence/InvestmentSerializerAdapterTests.cs`: version-3 cases mirroring the existing version-2 `Rent` ones — `"PriceHistory"` key rename, `IsManual`→`Source` mapping for both `true`/`false`, no-op on an already-version-3 document; update `Serialize_WritesCurrentVersion` to assert `"Version": 3` (depends on T010)
- [X] T023 [P] Update `Tests/Financial.Investment.Infrastructure.Tests/Services/AssetPriceLookupServiceTests.cs`: rename `ReloadAssetFromDisk(...).PriceHistory`→`.PriceSnapshots` (depends on T005)
- [X] T024 [P] Extend `Tests/Financial.Investment.Application.Tests/Services/AssetAdminServiceTests.cs` for `ValuationMethod`/`IncomePolicy` on create/update, including the null-request-defaults-to-`Unspecified`/`Unknown` case (depends on T016, T017)
- [X] T025 [P] Update `Tests/Financial.Investment.Application.Tests/Services/DataQualityReportServiceTests.cs`: rename `.PriceHistory`→`.PriceSnapshots` (depends on T012) — **no change needed**: this file only calls `asset.SetPrice(...)`, never reads `.PriceHistory` directly
- [X] T026 [P] Update `Tests/Financial.Investment.Application.Tests/Services/NavigationServiceTests.cs` for the `PriceSnapshots` rename and widened `AssetPriceSnapshotDTO` fields (depends on T011, T015)
- [X] **Unplanned, found necessary during Foundational**: `AssetDetailsDTO`/`AssetAdminDTO`'s wire-shape changes are breaking (`priceHistory` removed, `AssetAdminDTO`/Create/Update gained required fields), so `Financial.Web` and `Financial.App` needed the mechanical rename applied immediately to keep both building — not deferred to US2. Updated: `Financial.App/ViewModels/Investment/AssetDetailsViewModel.cs` (`.PriceSnapshots`), `Financial.Web/src/hooks/usePriceHistory.ts` (`.priceSnapshots`, `currency`/`sourceReference: null` on `setAssetPrice`), `Financial.Web/src/pages/AssetsPage.tsx` (`valuationMethod`/`incomePolicy: null` on create/update — no form field yet, a later increment), and ~12 Web test fixture files. Regenerated the OpenAPI snapshot and `Financial.Web`'s generated types (`npm run generate-api-types`) in the same phase.

**Checkpoint**: `Financial.Investment.Domain`/`Application`/`Infrastructure` build; every existing and
new Foundational test passes. Every user story can now proceed independently.

---

## Phase 3: User Story 1 - Valuing a holding that has no market price (Priority: P1) 🎯 MVP

**Goal**: A holding classified `ProviderValue`/`Manual` can have its total value recorded directly
(no unit quantity or per-unit price), reflected in market value/gain-loss, and is never touched by
an automatic fetch.

**Independent Test**: Classify a holding with the appropriate valuation method, record a total value
for it, and confirm market value/unrealised gain reflect that recorded value with no quantity/price
required; confirm an automatic fetch against it is a no-op.

### Implementation for User Story 1

- [X] T027 [US1] **Adjusted during implementation**: no new parameter needed — `HoldingValuationCalculator.Calculate` already receives the `AssetPriceSnapshot` being valued, and that snapshot already carries its own `ValuationMethod` (T006/T007). The branch reads `price.ValuationMethod` directly: `MarketValue = price.Price` when `ProviderValue`/`Manual`, else the existing `quantity * price.Price` (research.md #5, refined)
- [X] T028 [US1] **No change needed** — since T027 didn't add a parameter, `HoldingValuationService.GetValuation` already passes the right snapshot unchanged; nothing to thread through
- [X] T029 [US1] Update `Financial.Investment.Application/Services/AssetPriceHistoryService.cs`'s `SetPriceAsync`: derive `Source` from the resolved asset's `ValuationMethod` (`ProviderValuation` when `ProviderValue`, else `Manual`) and call `Asset.SetPrice`'s full-provenance overload (T007) with the request's `Currency`/`SourceReference` and `DateTimeOffset.UtcNow` (depends on T007, T014)
- [X] T030 [US1] Update `Financial.Investment.Application/Services/AssetPriceLookupService.cs`'s `GetCurrentPriceAsync`: when the resolved asset's `ValuationMethod` is `ProviderValue`/`Manual`, return its most recent recorded snapshot (any date) directly and never call `_assetPriceService.GetCurrentPrice` — generalizing the existing "manual price for today" short-circuit (`FindManualPriceForToday`) to apply for any date under these two methods; throws `InvalidOperationException` when no snapshot has ever been recorded (a graceful `Unavailable` status is T040, US2) (depends on T005)

### Tests for User Story 1

- [X] T031 [P] [US1] Extend `Tests/Financial.Investment.Domain.Tests/Domain/HoldingValuationCalculatorTests.cs` for the `ProviderValue`/`Manual` `MarketValue` branch, including a `0`-value write-down (depends on T027)
- [X] T032 [P] [US1] Extend `Tests/Financial.Investment.Application.Tests/Services/HoldingValuationServiceTests.cs` for a `ProviderValue` asset (depends on T028)
- [X] T033 [P] [US1] Extend `Tests/Financial.Investment.Infrastructure.Tests/Services/AssetPriceHistoryServiceTests.cs` for `Source` derivation per `ValuationMethod` (depends on T029)
- [X] T034 [P] [US1] Extend `Tests/Financial.Investment.Infrastructure.Tests/Services/AssetPriceLookupServiceTests.cs` for the `ProviderValue`/`Manual` no-fetch short-circuit on a non-today date, plus the no-snapshot-yet throw case (depends on T030)
- [X] T035 [US1] Extend `Tests/Financial.Api.Tests/AssetPriceEndpointsTests.cs`: `PUT /prices` a `0` value for a `ProviderValue` asset and confirm `200 OK` with `marketValue: 0`; `GET /prices/current` for a `Manual` asset and confirm no fetch is attempted (depends on T029, T030)
- [X] T036 [US1] **No additional regeneration needed** — Foundational's OpenAPI snapshot regen already covered every DTO field US1 uses; US1 changed behavior only, no DTO shape. Confirmed by a clean `dotnet test Tests/Financial.Api.Tests` run with no snapshot diff (depends on T013, T014, T015, T035)

**Checkpoint**: US1 is independently testable via the API — a value-based/Inco-style holding can be
fully recorded and valued.

---

## Phase 4: User Story 2 - Trusting the price shown on screen (Priority: P2)

**Goal**: Every holding's price/value shows its source, as-of date, and market status
(Current/Stale/Unavailable), with a visible warning when stale or unavailable, identically in both
front ends.

**Independent Test**: View a stale holding, a fetch-failed holding, and a fresh holding; confirm each
is visibly distinguishable in both React and WPF.

### Implementation for User Story 2

- [X] T037 [US2] Create `MarketStatus` enum in `Financial.Investment.Domain/Rules/MarketStatus.cs`: `Current, Stale, Unavailable` (research.md #8)
- [X] T038 [US2] Widen `HoldingValuation`/`HoldingValuationCalculator.Calculate` in `HoldingValuationCalculator.cs`: replace `IsPriceStale: bool` with `MarketStatus: MarketStatus`; extracted the `PreviousWeekday` threshold into a new shared `MarketStatusCalculator.For(priceDate, asOfDate)` (Domain/Rules) so `AssetPriceLookupService` (T042) can reuse the identical staleness rule outside `HoldingValuationCalculator`; `Unavailable` when `price is null` and quantity is non-zero (depends on T027, T037)
- [X] T039 [US2] **No change needed** — `HoldingValuationService.GetValuation` already passes the `HoldingValuation` record through untouched; the renamed field required no logic change (depends on T038)
- [X] T040 [US2] Fix `AssetPriceLookupService.cs`: both the `ProviderValue`/`Manual` "no recorded value" branch (T030) and `FetchWithPriceHistoryFallback`'s no-fallback branch now return a `MarketStatus: Unavailable` `AssetPriceDTO` via a new `UnavailablePriceFor` helper instead of throwing (research.md #10) (depends on T005)
- [X] T041 [US2] Stamp `Source` on the `AssetValueSnapshot` each concrete `IFinanceService` returns: `GoogleFinanceService.cs`, `YahooFinanceService.cs`, `StatusInvestFinanceService.cs`, `DicionarioDoInvestidorFinanceService.cs`, `RedentiaFinanceService.cs`, plus `AssetSnapshotSourceAdapter.cs` (found during implementation — a sixth Google-backed caller) and the shared `WebPageParserMappers.ToAssetValueSnapshot` helper they route through (research.md #2) (depends on T008)
- [X] T042 [US2] Widen `AssetPriceDTO` in `Financial.Investment.Application/DTOs/AssetPriceDTO.cs`: add `Source`, `MarketStatus`; `AssetPriceService.cs` (Infrastructure) stamps `MarketStatus.Current` + the fetched `Source` on a live fetch; `AssetPriceLookupService.cs`'s `BuildPriceFrom`/`UnavailablePriceFor` populate them for the stored/fallback/unavailable paths (depends on T040, T041)
- [X] T043 [US2] Rename `IsPriceStale`→`MarketStatus` on `AssetDetailsDTO.cs` and `PortfolioAssetSummaryItemDTO.cs`; update `NavigationService.cs` and `PortfolioAssetSummaryBuilder.cs`'s mapping (depends on T038)
- [X] T044 [US2] Add read-only `ValuationMethod`/`IncomePolicy` to `AssetDetailsDTO.cs`, mapped in `NavigationService.cs`'s `GetAssetDetails` (FR-001, FR-010) (depends on T005)
- [X] T045 [US2] Update `Financial.Web/src/hooks/usePriceHistory.ts` and any other hook reading these DTOs: `.priceHistory`→`.priceSnapshots`, `.isPriceStale`→`.marketStatus` (depends on T036 regen, T043)
- [X] T046 [US2] Update `Financial.Web/src/components/PriceHistoryTab.tsx`: the existing "Source" column now shows the actual named provider (`entry.source`, e.g. "Google"/"StatusInvest"/"Provider Valuation") via a `SOURCE_LABELS` map, with the source reference as a tooltip, instead of a binary Manual/Automatic label (depends on T045)
- [X] T047 [US2] Updated `AssetSummaryTab.tsx` and `PortfolioSummaryTab.tsx` (found to be the actual asset-details/portfolio-grid components, not generically named): the existing "(Stale)"/"(S)" badge now reads `marketStatus === 'Stale'`; added a parallel "(Unavailable)"/"(U)" badge for `marketStatus === 'Unavailable'` (depends on T045)
- [X] T048 [P] [US2] Added Vitest tests for T046/T047: `PriceHistoryTab.test.tsx` (named-provider label), `AssetSummaryTab.test.tsx` and `PortfolioSummaryTab.test.tsx` (Stale/Unavailable badge show/hide) — plus fixed ~15 pre-existing test fixtures across the Web suite for the widened `AssetPriceSnapshotDto`/`AssetDetailsDto`/`AssetPriceDto` shapes (depends on T045, T046, T047)
- [X] T049 [US2] `PriceHistoryView.xaml`'s "Source" column (bound directly to the `AssetPriceSnapshotDTO`, no ViewModel needed) now shows `Source` directly instead of an IsManual-driven Automatic/Manual `DataTrigger`. `AssetDetailsViewModel.cs`/`PortfolioAssetSummaryRowViewModel.cs`: `IsPriceStale` now computed from `MarketStatus == Stale`; added a parallel `IsPriceUnavailable` (`MarketStatus == Unavailable`) with matching XAML badges in `PortfolioSummaryView.xaml` (asset-details "(Unavailable)" and grid-row "(U)") (depends on T036, T043, T044)
- [X] T050 [P] [US2] Added `IsPriceUnavailable` coverage to `PortfolioAssetSummaryRowViewModelTests.cs` and `AssetDetailsViewModelCoverageTests.cs` (depends on T049)
- [X] T051 [US2] Regenerated the OpenAPI snapshot + `Financial.Web` generated types for `AssetPriceDTO`/`AssetDetailsDTO`/`PortfolioAssetSummaryItemDTO`/`AssetAdminDTO` family (same procedure as Foundational's regen) (depends on T042, T043, T044)

### Tests for User Story 2

- [X] T052 [P] [US2] Extended `Tests/Financial.Investment.Domain.Tests/Domain/HoldingValuationCalculatorTests.cs` for `MarketStatus` (`Current`/`Stale`/`Unavailable`), including the zero-quantity-no-price and non-zero-quantity-no-price distinction (depends on T038)
- [X] T053 [P] [US2] Extended `Tests/Financial.Investment.Infrastructure.Tests/Services/AssetPriceLookupServiceTests.cs` for the `Unavailable`-not-throw case (both the ProviderValue/Manual and fetch-failure paths) and `Source`/`MarketStatus` propagation on a live fetch and on the history fallback (depends on T040, T041, T042)
- [X] T054 [P] [US2] Extended `Tests/Financial.Investment.Application.Tests/Services/PortfolioAssetSummaryServiceTests.cs` and `NavigationServiceTests.cs` for `MarketStatus`/`ValuationMethod`/`IncomePolicy` in the DTO (depends on T043, T044)
- [X] T055 [US2] Extended `Tests/Financial.Api.Tests/AssetPriceEndpointsTests.cs` for `MarketStatus` in the live-fetch response, including the `Unavailable` case (the pre-existing `NoHistoryEntry_ReturnsBadRequest` test now asserts `200 OK` + `Unavailable`, matching the behavior change) (depends on T042)

**Checkpoint**: US2 is independently testable — source/as-of/market-status and a stale-data warning
are visible for any holding, identically in React and WPF.

---

## Phase 5: User Story 3 - Getting the price from the right source (Priority: P3)

**Goal**: Automatic price fetching is routed by `ValuationMethod`, not `GlobalAssetClass` alone, so
an unresolved-class bond/crypto holding is never queried through the wrong source; a holding with no
explicit method still resolves exactly as today.

**Independent Test**: Classify one holding per valuation method and trigger a fetch; confirm each is
routed appropriately, and an unclassified holding still resolves through today's default.

### Implementation for User Story 3

- [X] T056 [US3] Widen `IAssetPriceFetcher.Supports` in `Financial.Investment.Infrastructure/Interfaces/IAssetPriceFetcher.cs` to `Supports(GlobalAssetClass assetClass, ValuationMethod valuationMethod)` (depends on T002)
- [X] T057 [P] [US3] Update `StandardAssetPriceFetcher.cs`'s `Supports`: `valuationMethod is MarketPrice or NAV`, or `Unspecified` with today's `ExchangeListedClasses` check (research.md #4) (depends on T056)
- [X] T058 [P] [US3] Update `BondAssetPriceFetcher.cs`'s `Supports`: `valuationMethod == BondQuote`, or `Unspecified` with `assetClass == Bond` (research.md #4) (depends on T056)
- [X] T059 [P] [US3] Update `CryptocurrencyAssetPriceFetcher.cs`'s `Supports`: unchanged `assetClass == Cryptocurrency` logic, new signature (depends on T056)
- [X] T060 [US3] Update `AssetPriceService.cs`'s `GetCurrentPrice` to call `fetcher.Supports(request.AssetClass, request.ValuationMethod)` (depends on T056)
- [X] T061 [US3] Widen `AssetPriceRequestDTO` in `Financial.Investment.Application/DTOs/AssetPriceRequestDTO.cs`: add `ValuationMethod` (default `Unspecified`) (depends on T002)
- [X] T062 [US3] Update `AssetPriceLookupService.cs`'s `DescribeWith` to populate `ValuationMethod` from `asset.ValuationMethod` (depends on T061)
- [X] T063 [US3] Add a `valuationMethod` query parameter to `Financial.Api/Controllers/AssetPricesController.cs`'s `GetCurrentPrice`, parsed the same `Enum.TryParse<ValuationMethod>(..., ignoreCase: true)` way `assetClass` already is (depends on T061)

### Tests for User Story 3

- [X] T064 [P] [US3] Extend `Tests/Financial.Investment.Infrastructure.Tests/Services/AssetPriceServiceTests.cs` (or the per-fetcher test files, whichever exist) for the widened `Supports`, including an `Unknown`-class asset explicitly marked `BondQuote` routing to the bond fetcher (depends on T057, T058, T059, T060)
- [X] T065 [P] [US3] Extend `Tests/Financial.Investment.Application.Tests`/`Infrastructure.Tests` `AssetPriceLookupServiceTests.cs` for `ValuationMethod`-driven routing end-to-end (depends on T062)
- [X] T066 [US3] Extend `Tests/Financial.Api.Tests/AssetPriceEndpointsTests.cs` for the new `valuationMethod` query parameter routing an `Unknown`-class bond correctly (depends on T063)
- [X] T067 [US3] Regenerate the OpenAPI snapshot + `Financial.Web` generated types for the new query parameter (same procedure as T036) (depends on T063, T066)

**Checkpoint**: US3 is independently testable — fetch routing honors an explicit `ValuationMethod`
over `GlobalAssetClass`, and unclassified holdings are unaffected.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Whole-feature verification and documentation cleanup — depends on all three stories
being complete.

- [X] T068 [P] Run `npm run lint && npm run build` in `Financial.Web` — `tsc -b` catches any remaining call site reading the removed `.priceHistory`/`.isPriceStale` fields. Clean: 0 lint errors (3 pre-existing warnings in the generated `coverage/` report only), `tsc -b` + `vite build` succeed
- [X] T069 [P] Run `npm run smoke-test` (Playwright) against a locally published build. **Adjusted**: ran on port 8099, not 8080 — CLAUDE.md flags 8080 as the live Docker port and instructs checking `netstat` first; published the API + web build against a temp copy of `Tests/Financial.Api.Tests/TestData/*.test.json`, never the live data file. Passed
- [X] T070 Execute all seven sections of `quickstart.md` end-to-end against a full local run, including the §7 before/after regression diff (SC-004). **Adjusted**: §1 covered by the full `dotnet test`/`npm test` runs already green; §2/§7 verified against real production data — `data/data-investment.json` was already migrated to Version 3 by earlier live usage, and diffing it against the pre-migration `data-investment.backup-migration-20260912-084244.json` (auto-saved by the migration itself) confirmed zero `Date`/`Price` values changed across all 28 assets, only the key rename and new provenance fields; §3–§5 run live via curl against a temp test-data copy on port 8099 (US1: `ProviderValue` recording confirmed `marketValue` reflects the recorded total directly, a `0` write-down is distinguishable from a never-priced `Unavailable` holding, and an automatic fetch against it is skipped entirely; US2: `Current`/`Stale`/`Unavailable` all confirmed on live asset-details responses; US3: an identical `Unknown`-class request routed to a different fetcher purely off the `valuationMethod` query parameter, confirmed via each fetcher's distinct validation-error message rather than a real network call, since hitting live finance providers from this environment isn't reliable); §6 (front-end parity) relies on the existing automated coverage (`PriceHistoryTab.test.tsx`, `AssetSummaryTab.test.tsx`, `PortfolioSummaryTab.test.tsx`, `PortfolioAssetSummaryRowViewModelTests`, `AssetDetailsViewModelCoverageTests`) rather than a live side-by-side browser+WPF session, which this environment can't run
- [X] T071 Self-review the full diff against `docs/rules/implementation.md`'s Definition of Done, and `docs/ui/review-checklist.md` in full for the US2 front-end changes (per `docs/rules/ui.md`'s scope-of-compliance rule, not only the items tied to this feature's original trigger). **Found and fixed a real parity gap**: `PriceHistoryView.xaml`'s Source column bound directly to the `PriceSource` enum, rendering `ProviderValuation`/`DicionarioDoInvestidor` (no spaces) instead of React's `SOURCE_LABELS` text (`Provider Valuation`/`Dicionario do Investidor`) — added `PriceSourceToLabelConverter.cs` mirroring React's map exactly, wired into `App.xaml` and the column binding, with `PriceSourceToLabelConverterTests.cs` covering every enum value. Every other checklist item passed as-is (badges use text + the existing `--error`/`SystemFillColorCriticalBrush` token on both platforms, not colour alone; gating against loading/failed states matches between React and WPF; no forms, grids-beyond-Source, or responsive behavior were touched)
- [X] T072 Update `docs/investment-performance-roadmap.md` §6 Wave 2 to record this feature as delivered, matching how Wave 1 (P47) was marked merged in the same document. **Adjusted**: Wave 1's heading itself carries no explicit "merged" marker in the document (only inline `**[fixed/corrected date]**` annotations on individual gap rows) — applied that same inline-marker convention to the Wave 2 heading instead (`**[delivered 2026-09-12]**`), plus a short note below the table listing the four merged PRs and the one known follow-up (no front-end form field yet for `ValuationMethod`/`IncomePolicy`)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup. **Blocks every user story.**
- **User Story 1 (Phase 3)**: Depends on Foundational only.
- **User Story 2 (Phase 4)**: Depends on Foundational only — independent of US1's value-recording path.
- **User Story 3 (Phase 5)**: Depends on Foundational only — independent of US1 and US2.
- **Polish (Phase 6)**: Depends on all three user stories.

Unlike P47 (004), these three stories genuinely are independently parallelizable once Foundational is
done — none of US1/US2/US3's Acceptance Scenarios requires another story's work to exist first.

### Within Each Phase

- Domain/Application implementation before its own tests where a test needs the production code to
  exist to compile against (tests are written test-and-implementation-together, not strict TDD —
  Principle V requires tests, not a red-green-refactor order).
- DTO changes before the services/mappers that consume them.
- Backend (Domain → Application → Infrastructure → Api) before Web/WPF display work (US2).

### Parallel Opportunities

- Foundational: T002, T003, T004 (enums) in parallel; T012 parallel to T011; T018–T026 (tests) in
  parallel once their respective production-code task lands.
- US1: T031–T034 in parallel once their dependencies land.
- US2: T037 first (blocks T038); T041 parallel to T037/T038; T048/T050 (front-end tests) in parallel
  once their implementation tasks land; T052–T054 in parallel.
- US3: T057, T058, T059 in parallel once T056 lands; T064/T065 in parallel.
- **US1, US2, and US3 themselves can proceed in parallel** (different files, no cross-story
  dependency) once Foundational is done — see note above.

---

## Parallel Example: Foundational Phase

```bash
# These three enums have no dependency on each other:
Task: "Create ValuationMethod enum (Financial.Investment.Domain/Entities/ValuationMethod.cs)"
Task: "Create IncomePolicy enum (Financial.Investment.Domain/Entities/IncomePolicy.cs)"
Task: "Create PriceSource enum (Financial.Investment.Domain/Entities/PriceSource.cs)"
```

## Parallel Example: User Stories 1–3

```bash
# Once Foundational is done, all three stories can proceed independently:
Task: "Widen HoldingValuationCalculator.Calculate for ProviderValue/Manual MarketValue (US1)"
Task: "Create MarketStatus enum and widen HoldingValuation (US2)"
Task: "Widen IAssetPriceFetcher.Supports for ValuationMethod routing (US3)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (Setup) and Phase 2 (Foundational — blocking).
2. Complete Phase 3 (User Story 1).
3. **STOP and VALIDATE**: run `quickstart.md` §3 against a running API; confirm a value-based/Inco
   holding can be fully recorded and valued.
4. This is a real, demoable increment — the two instrument types the roadmap flags as
   "not representable at all today" become representable via the API, even before US2/US3's polish.

### Incremental Delivery

1. Setup + Foundational → shared model/persistence/Admin CRUD ready.
2. Add User Story 1 → validate via API/tests → mergeable increment (value-based holdings work).
3. Add User Story 2 → validate via `quickstart.md` §4/§6 → mergeable increment (provenance/staleness
   visible in both front ends).
4. Add User Story 3 → validate via `quickstart.md` §5 → mergeable increment (correct fetch routing).
5. Polish → whole-feature regression guard, roadmap doc update, self-review.

### Parallel Team Strategy

With multiple developers, once Foundational is done: Developer A takes US1, Developer B takes US2,
Developer C takes US3 — all three integrate independently, unlike P47's forced sequential chain.
