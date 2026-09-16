# P52-F04 — Upcoming Income — Implementation Plan

## Prerequisites
- On branch `feature/p52-f04-upcoming-income` (already checked out).
- Read `spec.md` in full, especially the Decisions/Assumptions subsection — every phase below
  implements one or more of those decisions.
- No dependency on any other P52 feature (PRD §8: F04 has no dependencies). F01, F02 and F03 are
  already merged into `main`, so their conventions (`StartSpan`/`MarkSuccess`/`MarkFailed`,
  `StubInvestmentRepository`, flat controller routes, the broker-then-portfolio-then-asset walk)
  are already present in the codebase to follow directly, not just to read from their specs.
- `CreditFrequencyAnalyzer.DetectFrequencyPerYear` (`Financial.Investment.Domain/Rules/`) and its
  existing test suite need no changes; this feature only calls it.

## Phase 1 — Application: DTO, builder, service, DI

1. Add `UpcomingIncomeDTO` (`Financial.Investment.Application/DTOs/UpcomingIncomeDTO.cs`) as a
   single record with `AssetName`, `BrokerName`, `LastCreditDate`, `ProjectedNextDate`,
   `ProjectedAmount`, per spec.md §5's field list and §1 Decision 3 (no wrapper container).
2. Add `IUpcomingIncomeService` (`IReadOnlyList<UpcomingIncomeDTO> GetUpcomingIncome();`, no
   parameters, synchronous per spec.md §1 Decision 2).
3. Add `UpcomingIncomeBuilder` (internal static): given one Active asset and its owning broker's
   name, calls `CreditFrequencyAnalyzer.DetectFrequencyPerYear(asset.Credits)`, returns `null` when
   no frequency is detected, and otherwise builds one `UpcomingIncomeDTO` from the asset's
   max-dated `Credit` and the frequency-to-interval mapping, per spec.md §6 Rules 1–3.
4. Add `UpcomingIncomeService`, wired like `AllocationBreakdownService` (constructor null-guards for
   `repository`, `tracer`, `logger` — no `IHoldingValuationService`, per spec.md §1 Decision 5;
   `StartSpan`/`MarkSuccess`/`MarkFailed` wrapper). `GetUpcomingIncome()` walks
   `investments.ActiveBrokers` → portfolios → assets, delegates each asset's projection to
   `UpcomingIncomeBuilder.TryBuildEntry`, drops the `null` results, and sorts the rest per spec.md
   §6 Rule 6.
5. Register `IUpcomingIncomeService` in `AddFinancialApplication()`.

## Phase 2 — Unit tests

1. Write `UpcomingIncomeServiceTests.cs` per spec.md §7's Unit test list: constructor guards; one
   projection test per detected frequency (monthly, quarterly, four-monthly); the irregular-payer
   and fewer-than-two-credits omission proofs; the `ProjectedAmount`-reads-`NetAmount` proof; the
   Active-scope-only proof; the no-valuation-dependency proof; the sort-and-tie-break proof; the
   empty-result proof; and the negative-path (repository failure) span/log assertion.
2. Write `UpcomingIncomeBuilderTests.cs` per spec.md §7's Unit test list: the three interval
   mappings, the no-detectable-frequency `null` result, and the max-dated-credit selection proof
   for both `LastCreditDate` and `ProjectedAmount` together.
3. Run `dotnet test Tests/Financial.Investment.Application.Tests` and confirm every new test passes
   and no existing test in the project regresses.

## Phase 3 — Presentation, AC-tracing tests, contract regeneration

1. Add `UpcomingIncomeController` (`Financial.Api/Controllers/UpcomingIncomeController.cs`),
   `[Route("upcoming-income")]`, one `[HttpGet]` action returning
   `Ok(IReadOnlyList<UpcomingIncomeDTO>)` from `IUpcomingIncomeService.GetUpcomingIncome()` — no
   route/query parameters (spec.md §3, §4, §1 Decision 4).
2. Add `Controllers/UpcomingIncomeControllerTests.cs` (guard-clause/smoke test, matching whichever
   convention `AllocationBreakdownControllerTests`/`DataQualityReportControllerTests` already
   established).
3. Add the literal AC ids to
   `docs/prd/P52-prd-portfolio-dashboard-and-data-quality/prd-portfolio-dashboard-and-data-quality.md`
   §9's F04 group only (`P52-F04-upcoming-income-01` through `-04`, in the order the four bullets
   already appear), per `testing-guide-Financial`'s `references/feature-traceability.md` — do not
   touch any other feature's §9 group.
4. Write `Tests/Financial.Api.Tests/Acceptance/UpcomingIncomeAcceptanceTests.cs`, one
   `[Trait("AC", "P52-F04-upcoming-income-0N")]` test per bullet added in step 3, per spec.md §7's
   Integration test list — including the two backend-precondition-only tests for the window-filter
   and empty-state bullets, with each test's own note explaining what remains F05/F06's
   responsibility.
5. Regenerate the OpenAPI snapshot
   (`UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests` in bash, or the PowerShell
   two-step form from CLAUDE.md) and review the diff — it should show only the new
   `UpcomingIncomeDTO` schema and the new `/upcoming-income` path.
6. Regenerate the frontend types (`cd Financial.Web && npm run generate-api-types`) and commit the
   result; confirm `src/api/generated/__tests__/openapiFreshness.test.ts` passes.
7. Run the full backend suite (`dotnet test --settings coverlet.runsettings --results-directory TestResults`)
   and `cd Financial.Web && npm run build` to confirm no type-level fallout from the new generated
   types (none expected — nothing in `Financial.Web` references the new DTO yet; F05 does).
