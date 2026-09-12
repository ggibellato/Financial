# Implementation Plan: Valuation Methods and Provenance

**Branch**: `005-valuation-methods-provenance` | **Date**: 2026-09-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/005-valuation-methods-provenance/spec.md`

## Summary

Add an explicit `ValuationMethod` (market price / NAV / provider value / manual / bond quote) and
`IncomePolicy` (distributing / accumulating / unknown) classification per holding, so a value-based
fund or property-platform investment ("Inco") — currently unrepresentable at all — can record its
worth as a directly-entered total value instead of quantity × per-unit price. Widen
`AssetPriceSnapshot` (rename from `Asset.PriceHistory`/`PriceSnapshots`, per this feature's own
clarification session) with currency, a named source, a source reference, the valuation method it
was recorded under, and a retrieved-at instant; compute a `MarketStatus` (Current/Stale/Unavailable)
at read time rather than storing staleness, which would go stale itself. Route automatic price
fetching by valuation method (falling back to today's asset-class-based routing when unset), fixing
the concrete case where an unresolved-class bond or crypto holding is queried through the wrong
source. Both front ends surface source/as-of/market-status and a stale-data warning together, per
Constitution Principle III.

The technical approach reuses existing machinery rather than introducing new entities:
`AssetPriceSnapshot`/`Asset.SetPrice` already record one dated figure per holding — for a
provider-valued/manual holding that figure *is* the total value, interpreted differently by
`HoldingValuationCalculator`, not stored differently. `IAssetPriceFetcher.Supports` gains a
`ValuationMethod` parameter alongside its existing `GlobalAssetClass` one. `InvestmentDataMigrations`
gains one more version-gated step (2 → 3), following the exact pattern P47 already established for
its own document-version upgrade. See `research.md` for the design decisions behind each of these,
including two found while reading the code rather than anticipated from the spec alone (research.md
#10, #11).

## Technical Context

**Language/Version**: C# / .NET 10 (`Financial.Investment.*`, `Financial.Api`, `Financial.App`);
TypeScript 5 / React 18 (`Financial.Web`)

**Primary Dependencies**: ASP.NET Core (API controllers); `System.Text.Json` with a custom
`DefaultJsonTypeInfoResolver` (`InvestmentTypeInfoResolver`) for private-setter persistence; xUnit +
FluentAssertions (no mocking framework — hand-written fakes in `Financial.TestUtilities`); Vite +
Vitest + React Testing Library + Playwright (`Financial.Web`); the existing `IFinanceService` chain
(`GoogleFinanceService`/`YahooFinanceService`/`StatusInvestFinanceService`/
`DicionarioDoInvestidorFinanceService`/`RedentiaFinanceService` via `FallbackFinanceService`) — no new
external dependency

**Storage**: Single JSON document `data/data-investment.json`, `LocalJson`/`GoogleDrive` provider
(unchanged), loaded once at process startup — a migration requires a full process restart afterward

**Testing**: `dotnet test --settings coverlet.runsettings` (unit + `WebApplicationFactory`
integration + `OpenApiContractTests` snapshot); `npm run lint && npm test && npm run build`
(`Financial.Web`); `npm run smoke-test` (Playwright, CI-gated)

**Target Platform**: Docker/Linux container (API + built SPA, `docker-compose up`); Windows desktop
(`Financial.App`, in-process against the same Application/Domain layers, not an HTTP client)

**Project Type**: Web application (ASP.NET Core API + React SPA) plus a WPF desktop client sharing
backend layers in-process — matches the existing repository structure exactly (no new project
needed; this feature widens existing Domain/Application/Infrastructure/Api/App/Web code)

**Performance Goals**: N/A — single-user, self-hosted tool (Constitution Principle IV); the
version-3 migration must complete in well under a second against current data volume, consistent
with every migration already in this codebase

**Constraints**: JSON loaded once at startup (restart required after migration, never sufficient on
its own — Constitution: Technology & Persistence Constraints); full-document rewrite on every save;
OpenAPI snapshot + generated TS types MUST be regenerated in the same PR as any DTO shape change
(`contracts/api-contract.md`); both front ends MUST ship each user story's increment together, not
sequentially (Wave 0/Wave 1 precedent)

**Scale/Scope**: 160 assets / 62+ price snapshots at spec-writing time (illustrative, volatile — the
roadmap's own stated warning; the data file gained five prices during a single working session); 5
valuation methods; 8 price sources (`Unknown`, `Manual`, `ProviderValuation`, + 5 named providers); 3
market statuses; ~90 `GlobalAssetClass.Unknown` holdings that must keep working unchanged (FR-003)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Assessment |
|---|---|
| I. Clean Architecture, Strictly Layered | **Pass.** `ValuationMethod`, `IncomePolicy`, `PriceSource`, `MarketStatus` enums and the widened `AssetPriceSnapshot`/`Asset` live in Domain; `HoldingValuationCalculator`'s branching lives in Domain (`Financial.Investment.Domain/Rules`); the version-3 migration step lives in Infrastructure (`InvestmentDataMigrations`); `IAssetPriceFetcher` implementations (Infrastructure) depend on Domain enums, not the reverse. `Financial.Api` controllers, `Financial.Web`, `Financial.App` are presentation-only consumers of the widened DTOs. |
| II. Bounded Context Isolation | **Pass.** Entirely within Investment; CashFlow is untouched. |
| III. WPF/Web Feature Parity | **Pass, same deliberate deviation Wave 0/Wave 1 recorded**: each user story's front-end slice (React + WPF) must land in the same increment — showing `marketStatus`/`priceSnapshots` in one front end while the other still reads the old `isPriceStale`/`priceHistory` field would be a parity regression (both DTO renames are breaking, not additive), not an unfinished increment. `Financial.App` continues resolving Application interfaces in-process. |
| IV. Right-Sized Engineering | **Pass.** No new entity for "provider/manual valuation record" (research.md #5 — reuses `AssetPriceSnapshot`); no new mutation surface for `ValuationMethod`/`IncomePolicy` beyond two small setters, chosen specifically *because* adding constructor parameters would touch 24 existing call sites (research.md #11) — a concrete, evidence-based right-sizing decision, not a general appeal to the principle. `MarketStatus` is computed, never stored, avoiding a field that would need its own future correctness fix (research.md #8, mirroring the `PositionType` anti-pattern G10 already flagged). |
| V. Test-Backed Changes | **Pass, to be detailed in `/speckit-tasks`.** xUnit + FluentAssertions, no new mocking framework; extends `AssetTests`, `AssetPriceSnapshotTests`, `HoldingValuationCalculatorTests`, `InvestmentSerializerAdapterTests` (version-3 cases mirroring the existing version-2 ones), `AssetPriceLookupServiceTests`, `AssetAdminServiceTests`; API round-trip tests via `WebApplicationFactory`; Vitest/RTL for the widened price/value entry and display; WPF ViewModel tests through existing conventions. |
| VI. Evidence-Based, Spec-Driven Change | **Pass, exemplified during this feature's own planning**: two of the roadmap's/spec's own framings were checked against the actual code and corrected before this plan was written — the "Provider/manual valuation record" the spec's Key Entities implied might be a new entity turned out to be the existing `AssetPriceSnapshot` reinterpreted (research.md #5), and the "Google→Yahoo→StatusInvest" single fallback chain the roadmap described turned out to be two independent two-provider chains plus a fifth provider (research.md #2), found by reading `InvestmentInfrastructureServiceCollectionExtensions.cs` directly rather than building on the roadmap's description. |
| VII. Incremental Vertical Delivery | **Pass.** `/speckit-tasks` slices by the spec's own priority order (US1 → US2 → US3), each a complete, independently testable/deployable increment per the spec's own "Independent Test" for each story. |
| VIII. Production Deployability After Every Merge | **Pass.** The `PriceHistory`→`PriceSnapshots` rename and the `IsManual`→`Source` derivation both ship as an on-the-fly document-version upgrade (`InvestmentDataMigrations`, version 2 → 3): `InvestmentSerializerAdapter.Deserialize` upgrades any document below the current version on load, so a plain process restart is the only deployment step. |

No unjustified violations — Complexity Tracking is not needed.

## Project Structure

### Documentation (this feature)

```text
specs/005-valuation-methods-provenance/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── api-contract.md  # Phase 1 output
└── tasks.md              # Phase 2 output (/speckit-tasks — not created by this command)
```

### Source Code (repository root)

```text
Financial.Investment.Domain/
├── Entities/
│   ├── Asset.cs                        # rename PriceHistory→PriceSnapshots; +ValuationMethod, +IncomePolicy, +SetValuationMethod/SetIncomePolicy
│   ├── AssetPriceSnapshot.cs            # +Currency, +Source, +SourceReference, +ValuationMethod, +RetrievedAt; IsManual becomes computed; Create() signature change
│   ├── ValuationMethod.cs               # NEW enum
│   ├── IncomePolicy.cs                  # NEW enum
│   └── PriceSource.cs                   # NEW enum
├── ValueObjects/
│   └── AssetValueSnapshot.cs            # +Source
└── Rules/
    ├── HoldingValuationCalculator.cs    # MarketValue branches on ValuationMethod; IsPriceStale→MarketStatus
    └── MarketStatus.cs                  # NEW enum

Financial.Investment.Application/
├── Services/
│   ├── NavigationService.cs            # asset.PriceHistory→PriceSnapshots
│   ├── NavigationMapper.cs             # MapPriceEntry widened
│   ├── DataQualityReportService.cs     # h.Asset.PriceHistory→PriceSnapshots
│   ├── AssetPriceLookupService.cs      # DescribeWith populates ValuationMethod; short-circuit ProviderValue/Manual before fetcher call; Unavailable status instead of throw (research.md #10)
│   ├── AssetPriceHistoryService.cs     # SetPriceAsync derives Source from asset.ValuationMethod
│   ├── AssetAdminService.cs            # CreateAssetAsync/UpdateAssetAsync set ValuationMethod/IncomePolicy via new setters
│   └── HoldingValuationService.cs      # (no change — consumes widened HoldingValuation as-is)
├── DTOs/
│   ├── AssetPriceDTOs.cs               # AssetPriceSnapshotDTO, SetAssetPriceDTO widened (contracts/api-contract.md)
│   ├── AssetPriceDTO.cs                # +Source, +MarketStatus
│   ├── AssetPriceRequestDTO.cs         # +ValuationMethod
│   ├── AssetDetailsDTO.cs              # PriceHistory→PriceSnapshots; IsPriceStale→MarketStatus; +ValuationMethod, +IncomePolicy
│   ├── PortfolioAssetSummaryItemDTO.cs # IsPriceStale→MarketStatus
│   └── AssetAdmin*.cs                  # +ValuationMethod?, +IncomePolicy?

Financial.Investment.Infrastructure/
├── Interfaces/
│   └── IAssetPriceFetcher.cs           # Supports(GlobalAssetClass, ValuationMethod)
├── Services/
│   ├── StandardAssetPriceFetcher.cs, BondAssetPriceFetcher.cs, CryptocurrencyAssetPriceFetcher.cs   # Supports() widened per research.md #4
│   ├── GoogleFinanceService.cs, YahooFinanceService.cs, StatusInvestFinanceService.cs, DicionarioDoInvestidorFinanceService.cs, RedentiaFinanceService.cs   # stamp Source on the AssetValueSnapshot they return
│   └── AssetPriceService.cs            # (no change — Supports() call already threads whatever IAssetPriceFetcher.Supports needs)
└── Persistence/
    ├── InvestmentTypeInfoResolver.cs   # exclude new computed IsManual
    └── InvestmentDataMigrations.cs     # NEW version-3 step: rename PriceHistory→PriceSnapshots key + IsManual→Source mapping; EnumerateAssets helper shared with existing EnumerateCredits

Financial.Api/Controllers/
└── AssetPricesController.cs            # GetCurrentPrice: +valuationMethod query param

Financial.Web/src/
├── hooks/usePriceHistory.ts            # .priceHistory→.priceSnapshots, .isPriceStale→.marketStatus
├── components/PriceHistoryTab.tsx      # +source column (named provider), +market-status/stale badge
├── components/(Admin asset form)       # +ValuationMethod, +IncomePolicy fields
└── api/generated/openapi.ts, api/types.ts   # regenerated (contracts/api-contract.md)

Financial.App/ViewModels/Investment/
├── PriceHistoryTabViewModel.cs         # widened DTO fields, same rename
├── AssetDetailsViewModel.cs            # MarketStatus display
└── (Admin)ViewModels/Admin/AssetsViewModel.cs   # +ValuationMethod, +IncomePolicy fields

Tests/
├── Financial.Investment.Domain.Tests/          # Asset, AssetPriceSnapshot, HoldingValuationCalculator
├── Financial.Investment.Application.Tests/     # AssetPriceLookupService, AssetAdminService, NavigationService, DataQualityReportService
├── Financial.Investment.Infrastructure.Tests/  # fetcher routing, InvestmentSerializerAdapter version-3 cases
├── Financial.Api.Tests/                        # round-trip + OpenAPI contract snapshot
├── Financial.Presentation.Tests/               # WPF ViewModel tests
└── Financial.Web (Vitest, co-located)
```

**Structure Decision**: This feature widens existing files in the established Investment
bounded-context layout (Domain → Application → Infrastructure → Api/App/Web). No new project is
needed — unlike P47, no standalone migration tool is needed either: the version-3 upgrade is a
second step added directly to the already-existing `InvestmentDataMigrations`/
`InvestmentSerializerAdapter` machinery P47 built, so there is nothing to build, run once, and
delete this time.

## Complexity Tracking

*(Not needed — no unjustified Constitution Check violations.)*
