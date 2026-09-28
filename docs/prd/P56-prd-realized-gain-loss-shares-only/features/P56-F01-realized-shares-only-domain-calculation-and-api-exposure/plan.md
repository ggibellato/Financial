# Plan: F01. Realized (Shares Only) domain calculation and API exposure

## Prerequisites

- None — Wave 1, no PRD dependencies.

## Phase 1: Domain and Application changes

1. **Domain property** - Add `RealizedGainLossSharesOnly` to `Financial.Investment.Domain/Entities/Asset.cs`, immediately after `RealizedGainLoss`, summing active `DisposalRecords`' `GainLoss` with `Credits` excluded.
2. **DTO fields** - Add `RealizedGainLossSharesOnly` to `PortfolioAssetSummaryItemDTO` and `AssetDetailsDTO`, immediately after each DTO's existing `RealizedGainLoss` field, matching its type and nullability.
3. **Builder mapping** - Extend `PortfolioAssetSummaryBuilder`'s `AssetComputedData` record, `ComputeAssetData`, and `ToDTO` to carry and map the new value from `asset.RealizedGainLossSharesOnly`.
4. **NavigationService mapping** - Extend `NavigationService`'s `AssetDetailsDTO` construction to map `asset.RealizedGainLossSharesOnly` alongside the existing `RealizedGainLoss` mapping.

## Phase 2: Backend test coverage

5. **Domain test** - Add a test in `Tests/Financial.Investment.Domain.Tests/Domain/AssetTests.cs` asserting `RealizedGainLossSharesOnly` excludes credits while `RealizedGainLoss` is unchanged, per spec.md's Testing Strategy.
6. **Application tests** - Add tests in `Tests/Financial.Investment.Application.Tests/Services/PortfolioAssetSummaryServiceTests.cs` (Active and Historic scope) and the file covering `NavigationService`'s `AssetDetailsDTO` mapping, per spec.md's Testing Strategy.

## Phase 3: API contract regeneration

7. **OpenAPI snapshot** - Regenerate `Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json` per this repo's documented `UPDATE_OPENAPI_SNAPSHOT=1` procedure, and confirm `OpenApiContractTests` passes against the new snapshot.
8. **Web generated types** - Regenerate `Financial.Web/src/api/generated/openapi.ts` via `npm run generate-api-types`, and confirm the openapi freshness test and `tsc -b` pass.
