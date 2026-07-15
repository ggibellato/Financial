# Feature Spec: F01. Position Status Domain Model

## 1. Technical Overview

**What:** Add a three-state `PositionType` enum (`Long`, `Flat`, `Short` — standard industry terminology for a position's directional state) as a top-level type in `Financial.Domain.Entities`, expose it as a computed `Asset.PositionType` property derived purely from `Asset.Quantity`'s sign, and surface that value through the existing asset DTOs (`AssetNodeDTO`, `AssetDetailsDTO`) and their TypeScript counterparts (`AssetNodeDto`, `AssetDetailsDto`) via the current mapping paths (`NavigationMapper.MapAsset`, `NavigationMapper.BuildAssetTreeNode`, `NavigationService.GetAssetDetails`).

**Why:** `Asset.Active => Quantity > 0` is a boolean today, so a closed position (`Quantity = 0`) and an open short (`Quantity < 0`) both evaluate to `false` and render identically in the WPF status converter. This feature replaces that binary signal with a genuine three-state one, computed the same way `Active` already is (a pure function of `Quantity`), with no new persisted state and no schema change.

**Scope:**
- **Included:** `PositionType` enum; `Asset.PositionType` computed property; `PositionType`/`positionType` field added to `AssetNodeDTO`/`AssetDetailsDTO` (C#) and `AssetNodeDto`/`AssetDetailsDto` (TS); mapping updates in `NavigationMapper` and `NavigationService`; a matching `PositionType` entry in the WPF tree's `Metadata` dictionary; Domain and Application unit tests; updates to existing Web test fixtures that construct these DTOs as literals.
- **Excluded:** Any UI rendering of the new position type (color converters, tree/detail panel visuals) — that belongs to F08 (Web) and F10 (WPF). Any API scope selector (`?scope=active|historic`) — that belongs to F05. Any change to `Asset.Active` or its existing consumers (`SummaryService`, `CreditService`, `BrokerBreakdownService` all filter with `.Where(a => a.Active)` for real aggregation logic, not just display) — `Active` stays exactly as it is today; `PositionType` is additive, not a replacement.

## 2. Architecture Impact

**Affected components:**
- `Financial.Domain/Entities/PositionType.cs` — new top-level enum (`Long`, `Flat`, `Short`)
- `Financial.Domain/Entities/Asset.cs` — new `PositionType` computed property
- `Financial.Application/DTOs/AssetNodeDTO.cs` — new `PositionType` property
- `Financial.Application/DTOs/AssetDetailsDTO.cs` — new `PositionType` property
- `Financial.Application/Services/NavigationMapper.cs` — `MapAsset` populates `PositionType`; `BuildAssetTreeNode` adds a `PositionType` entry to `Metadata`
- `Financial.Application/Services/NavigationService.cs` — `GetAssetDetails` populates `PositionType` on the returned `AssetDetailsDTO`
- `Financial.Web/src/api/types.ts` — `AssetNodeDto`/`AssetDetailsDto` gain a required `positionType: string` field
- Five existing Web test files gain a `positionType` value in their `AssetNodeDto`/`AssetDetailsDto` literals (see Section 4)
- `Tests/Financial.Domain.Tests/Domain/AssetTests.cs` — new/extended tests
- `Tests/Financial.Application.Tests/Services/NavigationMapperTests.cs` — new/extended tests

**Data flow:**

```mermaid
graph TD
    A["Asset.Quantity"] --> B["Asset.PositionType (computed)"]
    B --> C["NavigationMapper.MapAsset"]
    B --> D["NavigationService.GetAssetDetails"]
    C --> E["AssetNodeDTO.PositionType"]
    C --> F["NavigationMapper.BuildAssetTreeNode Metadata[PositionType]"]
    D --> G["AssetDetailsDTO.PositionType"]
    E --> H["AssetNodeDto.positionType (TS, via JSON)"]
    G --> I["AssetDetailsDto.positionType (TS, via JSON)"]
```

## 3. Technical Decisions

| Decision | Chosen Approach | Alternative Considered | Trade-off |
|----------|-----------------|-------------------------|-----------|
| Naming | `PositionType` (industry term for Long/Flat/Short), not `PositionStatus`/`Status` | `PositionStatus`/`Status` | User correction mid-implementation: "Position Type" is the term the industry actually uses for this concept |
| Enum placement | Top-level `PositionType` enum in `Financial.Domain.Entities` (own file `PositionType.cs`), not nested inside `Asset` | Nest inside `Asset` as `Asset.PositionType`, matching `Transaction.TransactionType` / `Credit.CreditType` | C# does not allow a class to declare both a nested type and a property with the identical name (`CS0102`); since the property is named `PositionType` to match the enum's name exactly (mirroring how `CountryCode Country`/`GlobalAssetClass Class` reference top-level Domain enums), the enum must live outside `Asset`, following the `CountryCode`/`GlobalAssetClass` convention instead |
| Relationship to `Active` | Keep `Asset.Active` unchanged; `PositionType` is a second, independent computed property | Remove/rename `Active` in favor of `PositionType` everywhere | `Active` is load-bearing business-logic filtering in `SummaryService`, `CreditService`, and `BrokerBreakdownService` (all outside this PRD's scope) — changing it would silently alter aggregation behavior those services rely on today |
| `Metadata` dictionary key for WPF tree | Add a new `"PositionType"` key alongside the existing `"IsActive"` key in `BuildAssetTreeNode` | Replace `"IsActive"` with `"PositionType"` | F10 (not this feature) owns switching the WPF converter/binding over; keeping `"IsActive"` prevents breaking today's binding until F10 lands |
| TS field requiredness | `positionType: string` required, matching `isActive`'s existing required-field convention | `positionType?: string` optional, to avoid touching existing test fixtures | User decision: consistency with `isActive` outweighs the one-time cost of updating 5 test fixture files |
| DTO property type | `AssetNodeDTO.PositionType`/`AssetDetailsDTO.PositionType` typed directly as the domain `PositionType` enum (serialized via `[JsonConverter(typeof(JsonStringEnumConverter))]`), mirroring how `Country`/`Class` are typed directly as `CountryCode`/`GlobalAssetClass` | Introduce a separate Application-layer enum decoupled from Domain | Matches the codebase's existing convention of DTOs referencing Domain enums directly; introducing a parallel Application enum would require an extra mapping step for no benefit in this codebase |

## 4. Component Overview

**Backend:**

| File Path | New/Modified | Purpose | Key Responsibilities |
|-----------|--------------|---------|------------------------|
| `Financial.Domain/Entities/PositionType.cs` | New | Domain enum | `PositionType { Long, Flat, Short }`, top-level in `Financial.Domain.Entities` |
| `Financial.Domain/Entities/Asset.cs` | Modified | Domain state | `PositionType` computed property (`Quantity > 0` → `Long`, `< 0` → `Short`, else `Flat`) |
| `Financial.Application/DTOs/AssetNodeDTO.cs` | Modified | Tree node DTO | New `PositionType` property (`[JsonConverter(typeof(JsonStringEnumConverter))]`) |
| `Financial.Application/DTOs/AssetDetailsDTO.cs` | Modified | Asset details DTO | New `PositionType` property (same converter) |
| `Financial.Application/Services/NavigationMapper.cs` | Modified | Mapping | `MapAsset` sets `PositionType = asset.PositionType`; `BuildAssetTreeNode` adds `Metadata["PositionType"] = asset.PositionType` (reads from the already-mapped `AssetNodeDTO`) |
| `Financial.Application/Services/NavigationService.cs` | Modified | Mapping | `GetAssetDetails` sets `PositionType = asset.PositionType` on the constructed `AssetDetailsDTO` |

**Frontend:**

| File Path | New/Modified | Purpose | Key Responsibilities |
|-----------|--------------|---------|------------------------|
| `Financial.Web/src/api/types.ts` | Modified | Type contracts | `AssetNodeDto`/`AssetDetailsDto` gain required `positionType: string` |
| `Financial.Web/src/pages/__tests__/CurrentValuesPage.test.tsx` | Modified | Test fixture | `makeBroker`'s nested asset literal gains a `positionType` value |
| `Financial.Web/src/hooks/useAssetSummary.test.ts` | Modified | Test fixture | `ASSET_DETAILS` literal gains a `positionType` value |
| `Financial.Web/src/hooks/useCredits.test.ts` | Modified | Test fixture | `ASSET_DETAILS` literal gains a `positionType` value |
| `Financial.Web/src/hooks/useTransactions.test.ts` | Modified | Test fixture | `ASSET_DETAILS` literal gains a `positionType` value |
| `Financial.Web/src/components/__tests__/AssetSummaryTab.test.tsx` | Modified | Test fixture | `ASSET` literal gains a `positionType` value |

No other Web test file needs changes: `InvestmentTree.test.tsx` builds `TreeNodeDto.metadata` as a loosely-typed `Record<string, unknown>` (no compile-time requirement to add the new key), and `DetailPanel.test.tsx`/`useAssetSummary.test.ts`'s other fixtures build `SelectedNode`, whose `isActive` field is already optional and which does not gain a `positionType` field in this feature.

**Database:** None — `PositionType` is computed at read time from `Quantity`; no schema or `data.json` shape change.

## 5. API Contracts

Not applicable — this feature has no new or modified endpoints. Existing `GET` endpoints that already return `AssetNodeDTO`/`AssetDetailsDTO` will include the new `positionType` field automatically once the DTOs are updated (F05 later adds the `scope` query parameter to these same endpoints).

## 6. Data Model

Not applicable — no persisted schema change. `PositionType` is a computed, in-memory value derived from `Asset.Quantity` on every read, exactly like `Asset.Active` today.

## 7. Testing Strategy

**Test File Structure:**

| Test File | Test Type | Target | Coverage Goal |
|-----------|-----------|--------|----------------|
| `Tests/Financial.Domain.Tests/Domain/AssetTests.cs` | Unit | `Asset.PositionType` | All three states |
| `Tests/Financial.Application.Tests/Services/NavigationMapperTests.cs` | Unit | `NavigationMapper.MapAsset`, `NavigationService.GetAssetDetails` | `PositionType` correctly mapped onto both DTOs and into tree `Metadata` |

**Test Functions:**

| Test Function | Description | Assertions |
|----------------|-------------|------------|
| `PositionType_PositiveQuantity_ReturnsLong` | Asset with `Quantity > 0` (e.g., after a buy) | `asset.PositionType.Should().Be(PositionType.Long)` — covers PRD acceptance criterion "Quantity > 0 → Long" |
| `PositionType_ZeroQuantity_ReturnsFlat` | Newly created asset, or fully sold down to zero | `asset.PositionType.Should().Be(PositionType.Flat)` — covers "Quantity = 0 → Flat" |
| `PositionType_NegativeQuantity_ReturnsShort` | Asset sold without a prior matching buy (net short) | `asset.PositionType.Should().Be(PositionType.Short)` — covers "Quantity < 0 → Short" |
| `GetNavigationTree_AssetNode_MetadataIncludesPositionType` | Extend the existing `NavigationMapperTests` pattern | `assetNode.Metadata["PositionType"].Should().Be(PositionType.Long)` for a long asset |
| `GetAssetsByBrokerPortfolio_AssetNodeDto_PositionTypeMatchesAssetPositionType` | Build assets with Long/Flat/Short quantities via `StubRepository` | Each returned `AssetNodeDTO.PositionType` matches the source asset's `PositionType` — covers "PositionType is present on Active-scoped AssetNodeDTO" |
| `GetAssetDetails_ReturnsPositionTypeMatchingAsset` | Build a single asset via `StubRepository`, call `GetAssetDetails` | Returned `AssetDetailsDTO.PositionType` matches `asset.PositionType` — covers "PositionType is present on ... AssetDetailsDto" |

**Web side:** No new test files — the existing fixture updates (Section 4) are what keep `npm run typecheck`/`tsc -b --noEmit` and the existing Vitest suites green with the now-required `positionType` field. No new Web behavior is introduced by this feature, so no new Web test assertions are needed.

**Deferred cross-feature integration:** PRD Section 9's Cross-Feature Integration criterion "Position status computed by F01 appears correctly on every Active-scoped asset returned by F05" cannot be tested yet — F05 (the `scope=active|historic` selector) does not exist. The unit tests above establish that `PositionType` is correctly computed and correctly present on both DTOs today; F05's own spec should add the actual end-to-end assertion once its scoped endpoints exist. Note for whoever specs F05/F08/F10 next: the PRD text still says "Status"/"status" — read that as `PositionType`/`positionType`, the name this feature actually shipped with.
