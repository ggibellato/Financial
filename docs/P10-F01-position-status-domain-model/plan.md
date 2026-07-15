# Implementation Plan: F01. Position Status Domain Model

**Prerequisites:**
- No new tools, libraries, or environment variables required
- Builds on the existing `Financial.Domain`, `Financial.Application`, and `Financial.Web` projects as they stand on `main`

### Stage 1: Domain Layer

**1. PositionStatus Enum and Asset.Status Property** - Add the `PositionStatus` enum (`Long`, `Flat`, `Short`) nested inside `Asset`, and a computed `Status` property that derives its value purely from `Asset.Quantity`'s sign. Leave `Asset.Active` untouched. Reference the spec's Section 3 decision on enum placement.

**2. Domain Unit Tests** - Extend `AssetTests.cs` with cases covering an asset with positive, zero, and negative quantity, asserting `Status` resolves to `Long`, `Flat`, and `Short` respectively.

### Stage 2: Application Layer

**3. DTO Updates** - Add a `Status` property to `AssetNodeDTO` and `AssetDetailsDTO`, following the existing enum-serialization pattern already used for `Country` and `Class` on those same DTOs.

**4. Mapping Updates** - Update `NavigationMapper.MapAsset` to populate the new `Status` field, update `NavigationMapper.BuildAssetTreeNode` to add a `PositionStatus` entry to the tree node's `Metadata` dictionary alongside the existing `IsActive` entry, and update `NavigationService.GetAssetDetails` to populate `Status` on the returned `AssetDetailsDTO`.

**5. Application Unit Tests** - Extend `NavigationMapperTests.cs` to verify the navigation tree's asset metadata includes the correct `PositionStatus`, and that both `AssetNodeDTO.Status` and `AssetDetailsDTO.Status` match the source asset's computed status across long, flat, and short cases.

### Stage 3: Web Types and Fixtures

**6. TypeScript Type Updates** - Add a required `status` field to the `AssetNodeDto` and `AssetDetailsDto` interfaces in `types.ts`, matching the existing `isActive` field's requiredness.

**7. Test Fixture Updates** - Update the asset-literal fixtures in the five affected test files (`CurrentValuesPage.test.tsx`, `useAssetSummary.test.ts`, `useCredits.test.ts`, `useTransactions.test.ts`, `AssetSummaryTab.test.tsx`) to include a `status` value consistent with each fixture's existing `isActive`/`quantity` value.

**8. Build and Test Verification** - Run the TypeScript build (`tsc -b --noEmit`) and the existing Vitest and .NET test suites to confirm the new required field introduces no compilation or test regressions.
