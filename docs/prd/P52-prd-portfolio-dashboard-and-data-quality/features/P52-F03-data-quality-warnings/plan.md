# P52-F03 — Data-Quality Warnings — Implementation Plan

## Prerequisites
- On branch `feature/p52-f03-data-quality-warnings` (already checked out).
- Read `spec.md` in full, especially the Decisions/Assumptions subsection — every phase below
  implements one or more of those decisions.
- No dependency on any other P52 feature (PRD §8: F03 has no dependencies).

## Phase 1 — Application: extend the DTO and the existing service
1. Add `OpenHoldingMissingCostBasisFinding` and `UnresolvedTaxClassificationFinding` records, and
   the 3 new fields on `DataQualityReportDTO`, in the field order spec.md §4/§6 Rule 4 specifies
   (`Financial.Investment.Application/DTOs/DataQualityReportDTOs.cs`).
2. Add `IHoldingValuationService` as a new constructor dependency of `DataQualityReportService`,
   with the same null-guard style every other constructor parameter in the file already uses.
3. Implement the 3 new fields inside `GenerateReport()`, reusing the method's existing flattened
   `allHoldings`/`activeHoldings` locals, per spec.md §6 Rules 1–3: `OpenHoldingsMissingCostBasis`
   (Active-only, zero invested amount plus nonzero market value), `StaleValuationCount` (Active-only
   count of `MarketStatus.Stale`), `UnresolvedTaxClassifications` (both scopes, `Active`-status
   classifications with `Incomplete`/`RequiresReview` calculation status).
4. Update `DataQualityReportServiceTests.cs`'s `CreateService()` helper to pass a
   `TestHoldingValuationService` (from `Tests/Financial.TestUtilities`) as the new dependency, and
   confirm every one of the file's existing 10 test methods still passes unmodified.
5. Add the new Unit tests listed in spec.md §7 (Unit) for the 3 new fields, including the
   supersede-never-reported regression test and the "existing 5 fields unaffected" regression test.

## Phase 2 — Presentation and Tools wiring
1. Add `DataQualityReportController` (`Financial.Api/Controllers/DataQualityReportController.cs`),
   `[Route("data-quality-report")]`, one `[HttpGet]` action returning `Ok(DataQualityReportDTO)`
   from `IDataQualityReportService.GenerateReport()` — no route/query parameters, mirroring
   `DashboardController`'s shape (spec.md §3, §4). No DI registration change needed — both
   dependent interfaces are already registered.
2. Fix `Tools/InvestmentDataQualityReport/Program.cs` to construct a `HoldingValuationService`
   (with a fresh `XirrCalculationService` and the tool's existing `NoOpTelemetryTracer`/
   `NullLogger` conventions) and pass it as `DataQualityReportService`'s new constructor argument,
   per spec.md Decision 3.
3. Extend `Tools/InvestmentDataQualityReport/DataQualityReportFormatter.cs` with the 2 new
   `Append*` sections and the `StaleValuationCount` line, in the file's own existing per-section
   format, per spec.md Decision 10.
4. Add `Controllers/DataQualityReportControllerTests.cs` (guard-clause/smoke test, matching
   whichever convention `DashboardControllerTests` already established).

## Phase 3 — AC-tracing tests and contract regeneration
1. Add the literal AC ids to
   `docs/prd/P52-prd-portfolio-dashboard-and-data-quality/prd-portfolio-dashboard-and-data-quality.md`
   §9's F03 group only (`P52-F03-data-quality-warnings-01` through `-04`, in the order the four
   bullets already appear), per `testing-guide-Financial`'s `references/feature-traceability.md` —
   do not touch any other feature's §9 group.
2. Write `Tests/Financial.Api.Tests/Acceptance/DataQualityWarningsAcceptanceTests.cs`, one
   `[Trait("AC", "P52-F03-data-quality-warnings-0N")]` test per bullet added in step 1, per
   spec.md §7's Integration test list.
3. Regenerate the OpenAPI snapshot
   (`UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests` in bash, or the PowerShell
   two-step form from CLAUDE.md) and review the diff — it should show only the extended
   `DataQualityReportDTO` schema (3 new fields, 2 new referenced schemas) and the new
   `/data-quality-report` path.
4. Regenerate the frontend types (`cd Financial.Web && npm run generate-api-types`) and commit the
   result; confirm `src/api/generated/__tests__/openapiFreshness.test.ts` passes.
5. Run the full backend suite (`dotnet test --settings coverlet.runsettings --results-directory TestResults`)
   and `cd Financial.Web && npm run build` to confirm no type-level fallout from the new generated
   types (none expected — nothing in `Financial.Web` references the new fields yet; F05 does).
