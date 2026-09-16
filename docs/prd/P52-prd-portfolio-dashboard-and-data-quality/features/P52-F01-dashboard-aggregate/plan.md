# P52-F01 — Dashboard Aggregate — Implementation Plan

## Prerequisites
- On branch `feature/p52-f01-dashboard-aggregate` (already checked out).
- Read `spec.md` in full, especially the Decisions/Assumptions subsection — every phase below
  implements one or more of those decisions.
- No dependency on any other P52 feature (PRD §8: F01 has no dependencies).

## Phase 1 — Shared currency-conversion extraction (no behaviour change)
1. Extract `ConvertedSummaryBuilder`'s private nested `ConversionContext` class into a new
   standalone internal class, `CurrencyConversionContext`, taking the source `Currency` explicitly
   in its constructor instead of capturing it from an enclosing method (Decision 6). Keep its
   per-date rate cache, `AttemptCount`/`FailureCount`/`IsUnavailable`/`IsPartial` members and
   `from == to` short-circuit exactly as they are today.
2. Update `ConvertedSummaryBuilder` to construct a `CurrencyConversionContext` instead of its own
   private one; remove the now-dead nested class. No other line of `ConvertedSummaryBuilder`
   changes.
3. Move `ConvertedSummaryBuilderTests.cs`'s tests that exercise `ConversionContext`'s own behaviour
   (as opposed to `ConvertedSummaryBuilder`'s orchestration) into a new
   `CurrencyConversionContextTests.cs`; confirm the rest of `ConvertedSummaryBuilderTests.cs`
   still passes unmodified, proving the extraction is behaviour-preserving.

## Phase 2 — Portfolio-wide XIRR and DTO/service skeleton
1. Add `PortfolioDashboardDTO` (Application DTO) with the fields listed in spec.md §4 (the "off"
   example's field set is authoritative for what must always be present, non-conditionally).
2. Add `PortfolioXirrBuilder` (internal static): builds the combined gross and net-of-tax dated
   cash-flow series across `ActiveBrokers ∪ HistoricBrokers`'s assets via the existing
   `AssetCashFlowBuilder.BuildWithCredits`/`BuildNetOfTaxWithCredits`, appends one terminal
   `(asOf, MarketValue)` entry per priced Active holding (never for Historic), and calls
   `Financial.Investment.Domain.Rules.XirrCalculator.Calculate` directly for each series
   (spec.md Decision 3, Rule 6).
3. Add `IPortfolioDashboardService` and `PortfolioDashboardService`, wired like `SummaryService`
   (constructor null-guards, `StartSpan`/`MarkSuccess`/`MarkFailed` wrapper, `TimeProvider`
   default). Implement `GetDashboardAsync()` computing `MarketValue`, `Invested`,
   `UnrealisedGainLoss`, `RealisedGainLoss`, `IncomeYtd`, `IncomeLifetime`, `UnvaluedHoldingCount`,
   `IsPartial` per spec.md Rules 1–5 and 7, and `GrossXirr`/`NetXirr` via `PortfolioXirrBuilder`.
   Reporting-currency fields default to the "disabled" shape from spec.md §4 for this phase —
   conversion itself lands in Phase 3.
4. Register `IPortfolioDashboardService` in `AddFinancialApplication()`.
5. Write/update unit tests for `PortfolioXirrBuilder` and the non-currency-conversion parts of
   `PortfolioDashboardService`, per spec.md §7's Unit test list (constructor guards; MarketValue/
   Invested/UnrealisedGainLoss/RealisedGainLoss/IncomeYtd/IncomeLifetime/IsPartial/
   UnvaluedHoldingCount behaviour, including the Decision 4 regression test and the negative-path
   test).

## Phase 3 — Multi-currency conversion
1. Add `PortfolioDashboardConvertedBuilder` (internal static): groups assets by owning
   `Broker.Currency`, builds one `CurrencyConversionContext` per distinct source currency, converts
   each asset's transactions/credits/terminal value on its own record date, sums the six converted
   money totals, and re-runs `XirrCalculator.Calculate` over the converted combined series for
   `ConvertedGrossXirr`/`ConvertedNetXirr` — per spec.md Rule 8.
2. Wire `PortfolioDashboardService.GetDashboardAsync()` to call this builder only when
   `IReportingCurrencyProvider.IsReportingCurrencyEnabled()` is true, populating
   `ReportingCurrency`, `IsReportingCurrencyEnabled`, the eight `Converted*` fields,
   `IsReportingCurrencyPartial` and `IsReportingCurrencyUnavailable` (Decision 7's distinct naming
   from `IsPartial`).
3. Write/update unit tests for `PortfolioDashboardConvertedBuilder` per spec.md §7 (own-currency
   conversion, unavailable-only-when-every-currency-fails, partial-when-mixed, same-currency
   short-circuit) and the `PortfolioDashboardService` reporting-currency-disabled test.

## Phase 4 — Endpoint, contract regeneration, AC-tracing tests
1. Add `DashboardController` (`Financial.Api/Controllers/DashboardController.cs`),
   `[Route("dashboard")]`, one `[HttpGet]` action returning `Ok(PortfolioDashboardDTO)` from
   `IPortfolioDashboardService.GetDashboardAsync()` — no route/query parameters (spec.md §3, §4).
2. Add the literal AC ids to `docs/prd/P52-prd-portfolio-dashboard-and-data-quality/prd-portfolio-dashboard-and-data-quality.md`
   §9's F01 group only (`P52-F01-dashboard-aggregate-01` through `-04`, in the order the four
   bullets already appear), per `testing-guide-Financial`'s `references/feature-traceability.md` —
   do not touch any other feature's §9 group.
3. Write `Tests/Financial.Api.Tests/Acceptance/PortfolioDashboardAggregateAcceptanceTests.cs`, one
   `[Trait("AC", "P52-F01-dashboard-aggregate-0N")]` test per bullet added in step 2, per spec.md
   §7's Integration test list; add the guard/smoke controller test alongside it.
4. Regenerate the OpenAPI snapshot
   (`UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests` in bash, or the PowerShell
   two-step form from CLAUDE.md) and review the diff — it should show only the new
   `PortfolioDashboardDTO` schema and `/dashboard` path.
5. Regenerate the frontend types (`cd Financial.Web && npm run generate-api-types`) and commit the
   result; confirm `src/api/generated/__tests__/openapiFreshness.test.ts` passes.
6. Run the full backend suite (`dotnet test --settings coverlet.runsettings --results-directory TestResults`)
   and `cd Financial.Web && npm run build` to confirm no type-level fallout from the new generated
   types (none expected — nothing in `Financial.Web` references the new DTO yet; F05 does).
