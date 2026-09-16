# P52-F02 — Allocation Breakdown — Implementation Plan

## Prerequisites
- On branch `feature/p52-f02-allocation-breakdown` (already checked out).
- Read `spec.md` in full, especially the Decisions/Assumptions subsection — every phase below
  implements one or more of those decisions.
- No dependency on any other P52 feature (PRD §8: F02 has no dependencies). F01 and F03 are already
  merged into `main`, so their conventions (`StartSpan`/`MarkSuccess`/`MarkFailed`, `StubInvestmentRepository`,
  `TestHoldingValuationService`, flat controller routes) are already present in the codebase to
  follow directly, not just to read from their specs.

## Phase 1 — Application: DTOs, builder, service, DI

1. Add `AllocationBreakdownDTO` and its four entry types (`AssetClassAllocationEntryDTO`,
   `CurrencyAllocationEntryDTO`, `CountryAllocationEntryDTO`, `BrokerAllocationEntryDTO`) in one new
   file, per spec.md §5's field list and §1 Decision 4's per-dimension typing (enum + converter for
   Class/Country, plain string for Currency/BrokerName).
2. Add `IAllocationBreakdownService` (`AllocationBreakdownDTO GetAllocationBreakdown();`, no
   parameters, synchronous per spec.md §1 Decision 2).
3. Add `AllocationBreakdownBuilder` (internal static): given the Active-scope holdings collected
   with their owning broker's `Name`/`Currency`, computes each asset's `WeightBasis` via
   `AssetAmountBases.For(InvestmentScope.Active, AssetTotals.For(asset),
   valuation.MarketValue).WeightBasis`, drops assets with a `null` basis, groups the rest into the
   four dimensions, sums `MarketValue` and computes `Percentage` per entry, and sorts each
   dimension's list per spec.md §6 Rules 1–5.
4. Add `AllocationBreakdownService`, wired like `PortfolioAssetSummaryService`/`BrokerBreakdownService`
   (constructor null-guards for `repository`, `holdingValuationService`, `tracer`, `logger`;
   `StartSpan`/`MarkSuccess`/`MarkFailed` wrapper). `GetAllocationBreakdown()` walks
   `investments.ActiveBrokers` → portfolios → assets (mirroring
   `PortfolioDashboardService.CollectHoldings`/`AddHoldings`'s own broker-then-asset walk, since
   `Asset` carries no back-reference to its owning `Broker`), builds the per-asset valuation +
   weight basis, and delegates grouping/sorting to `AllocationBreakdownBuilder`.
5. Register `IAllocationBreakdownService` in `AddFinancialApplication()`.

## Phase 2 — Unit tests

1. Write `AllocationBreakdownServiceTests.cs` per spec.md §7's Unit test list: constructor guards;
   per-dimension grouping (Class, Currency, Country, Broker); the two Unknown-bucket proofs (Class,
   Country); the unpriced-holding exclusion proof (spanning all four dimensions at once); the
   Active-scope-only proof (a Historic holding never contributes); the percentages-sum-to-100 proof
   for all four dimensions independently; the descending-sort proof; the zero-priced-holdings empty-
   lists proof; the unrecognized-currency fail-fast proof; and the negative-path (repository
   failure) span/log assertion.
2. Decide, per spec.md §7's note, whether `AllocationBreakdownBuilder`'s own branching needs a
   dedicated `AllocationBreakdownBuilderTests.cs` (only if its logic isn't already fully exercised
   through the service tests above — `PortfolioAssetSummaryBuilder`'s own precedent is to have no
   separate test file). Add it only if warranted.
3. Run `dotnet test Tests/Financial.Investment.Application.Tests` and confirm every new test passes
   and no existing test in the project regresses.

## Phase 3 — Presentation, AC-tracing tests, contract regeneration

1. Add `AllocationBreakdownController` (`Financial.Api/Controllers/AllocationBreakdownController.cs`),
   `[Route("allocation-breakdown")]`, one `[HttpGet]` action returning
   `Ok(AllocationBreakdownDTO)` from `IAllocationBreakdownService.GetAllocationBreakdown()` — no
   route/query parameters (spec.md §3, §4).
2. Add `Controllers/AllocationBreakdownControllerTests.cs` (guard-clause/smoke test, matching
   whichever convention `DashboardControllerTests`/`DataQualityReportControllerTests` already
   established).
3. Add the literal AC ids to
   `docs/prd/P52-prd-portfolio-dashboard-and-data-quality/prd-portfolio-dashboard-and-data-quality.md`
   §9's F02 group only (`P52-F02-allocation-breakdown-01` through `-03`, in the order the three
   bullets already appear), per `testing-guide-Financial`'s `references/feature-traceability.md` —
   do not touch any other feature's §9 group.
4. Write `Tests/Financial.Api.Tests/Acceptance/AllocationBreakdownAcceptanceTests.cs`, one
   `[Trait("AC", "P52-F02-allocation-breakdown-0N")]` test per bullet added in step 3, per spec.md
   §7's Integration test list.
5. Regenerate the OpenAPI snapshot
   (`UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests` in bash, or the PowerShell
   two-step form from CLAUDE.md) and review the diff — it should show only the new
   `AllocationBreakdownDTO` schema family and the new `/allocation-breakdown` path.
6. Regenerate the frontend types (`cd Financial.Web && npm run generate-api-types`) and commit the
   result; confirm `src/api/generated/__tests__/openapiFreshness.test.ts` passes.
7. Run the full backend suite (`dotnet test --settings coverlet.runsettings --results-directory TestResults`)
   and `cd Financial.Web && npm run build` to confirm no type-level fallout from the new generated
   types (none expected — nothing in `Financial.Web` references the new DTO yet; F05 does).
