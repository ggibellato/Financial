---

description: "Task list for Investment Calculation Core"
---

# Tasks: Investment Calculation Core

**Input**: Design documents from `/specs/003-investment-calculation-core/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/README.md, quickstart.md

**Tests**: Included. `docs/rules/implementation.md` §Tests, the `testing-guide-Financial` skill, and the
plan's Constitution Check (Principle V) require test-backed changes, AC-tracing integration tests, and
a dedicated test for every negative criterion — this is not optional for this project.

**Organization**: Tasks are grouped by user story, in the plan's non-negotiable increment order. This
feature's stories are **not independently orderable** the way the template usually assumes: US4 needs
US1's deterministic basis and US3's invested figure, US5 sums US4's per-holding valuations, and US6's
second half needs both US3 (invested) and its own first half (nullable weight) landed first. The
`Increment sequence` table in `plan.md` is the authority for ordering; phases below follow it exactly
(1→2→3→4→5→6→7→8→9→10→11→12→13), which is why User Story 6 appears in two non-adjacent phases.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1–US7, matching spec.md's priorities P1–P7
- Every task names its exact file path(s)

## Path Conventions

DDD-layered .NET solution + two independent front ends, per `plan.md` §Project Structure:
`Financial.Investment.{Domain,Application,Infrastructure}`, `Financial.Api`, `Financial.App`,
`Financial.Web/src`, `Tools/`, one test project per Domain/Application/Infrastructure/Presentation layer
under `Tests/`.

---

## Phase 1: Setup

- [X] T001 Run `dotnet restore` and `cd Financial.Web && npm install`; copy `data/data-investment.json`
      to a temp verification file (e.g. `/tmp/verify-investment.json` or the session scratchpad) per
      quickstart.md — every scenario below runs against this copy, **never** the live file

---

## Phase 2: Foundational

No blocking prerequisite phase applies. This feature widens no stored entity and adds no new project
until User Story 7 (`Tools/`). User Story 1 (Phase 3) establishes the shared date-ordering logic that
later stories build on — there is no infrastructure that must land before it.

---

## Phase 3: User Story 1 - Position figures that don't depend on the order I typed (Priority: P1) 🎯 MVP

**Goal**: A position's quantity, average price, realised gain and average sell price are derived by
replaying transactions in date order (purchases before sales on a shared date), deterministically
across restarts, with the two currently-persisted derived fields removed from storage.

**Independent Test**: Record a back-dated purchase after an existing sale, chronologically against one
holding and shuffled against another; confirm both report identical figures. Edit a transaction's date
backwards and confirm re-derivation. Restart and confirm figures survive.

### Increment 1 — Date-ordered replay + same-date rule + Buy-branch zero guard

- [X] T002 [US1] Write failing Domain tests in
      `Tests/Financial.Investment.Domain.Tests/Domain/TransactionsTests.cs` for: identical figures
      across shuffled vs. chronological entry order (SC-001), purchases-before-sales on a shared date
      (FR-002), re-derivation after a backdated date edit (FR-004), a closed-then-reopened position
      taking its average price only from post-reopen purchases (FR-008), and the Buy-branch
      zero-resulting-quantity guard not throwing and setting `AveragePrice = 0` (FR-016)
- [X] T003 [US1] Implement date-ordered replay in
      `Financial.Investment.Domain/Entities/Transactions.cs`: purchases-before-sales same-date
      tie-break, an in-order fast path on `Add` that triggers a full `Recompute()` only on an
      out-of-order arrival, and the Buy-branch-only zero-quantity guard (FR-001..FR-004, FR-007,
      FR-008, FR-016; R11, R14) — do **not** generalize the guard to the Sell path (would move ~128
      historic holdings' average price and break SC-002)
- [X] T004 [US1] Add the bounded-change regression fixture to
      `Tests/Financial.Investment.Domain.Tests/Domain/TransactionsTests.cs` reproducing the Bitcoin and
      AGNC (AGNC) 2 ISA same-date shapes, asserting the exact before/after figures (Bitcoin average
      price 62,713.15→62,709.05, realised gain 0.25→0.17; AGNC realised gain 9.98→9.66) and stating the
      precision the assertion runs at (FR-006, SC-002)

### Increment 2 — Stop persisting two derived fields

- [X] T005 [US1] Update
      `Tests/Financial.Investment.Infrastructure.Tests/Persistence/InvestmentTypeInfoResolverTests.cs`
      to assert `PositionType` and `Portfolio.IsEmpty` are no longer written to serialized JSON, and
      that a file still containing those keys loads cleanly (`UnmappedMemberHandling.Skip`)
- [X] T006 [US1] Remove `PositionType` and `IsEmpty` from the write path in
      `Financial.Investment.Infrastructure/Persistence/InvestmentTypeInfoResolver.cs` (FR-005) — both
      are computed properties with no setter, so this is safe in both directions
- [X] T007 [US1] Against the temp copy from T001: replay before/after this increment and confirm
      exactly Bitcoin and AGNC (AGNC) 2 ISA change, the other 158 holdings are untouched, and the file
      loads and saves cleanly (SC-002)

**Checkpoint**: Position replay is deterministic and order-independent; no derived figures persisted.
`TransactionsTests` outside T002/T004 needs zero changes — if it does, the design has gone wrong (R15).

---

## Phase 4: User Story 2 - The application refuses a sale of units I don't hold (Priority: P2)

**Goal**: A sale, edit or delete that would leave any sale at any date selling more than was held is
refused, nothing is stored, and the refusal names the held quantity (or the later sale left short).
Stored history that already breaches this still loads, opens, and is correctable.

**Independent Test**: Attempt an oversell from each front end → refused with held quantity named,
nothing persisted. Start the app and confirm the 3 already-breaching historic holdings still open.

### Increment 3 — SaleCoverageRule, TransactionReplayOrder, strict Asset methods, TransactionService repointed

- [X] T008 [P] [US2] Write Domain tests in new
      `Tests/Financial.Investment.Domain.Tests/Domain/TransactionReplayOrderTests.cs`: date-ascending
      order, purchases before sales on a tie, stable source-order tie-break, no tertiary tiebreaker
- [X] T009 [P] [US2] Write Domain tests in new
      `Tests/Financial.Investment.Domain.Tests/Domain/SaleCoverageRuleTests.cs`: no breach → null; a
      sale exceeding held quantity; a proposed edit/delete that leaves a *later* sale short (FR-065,
      FR-066, naming that sale and the shortfall); a sale of exactly the held quantity accepted
      (FR-011); the held-quantity value formatted round-trip, not `N2` (FR-012)
- [X] T010 [US2] Extract `Financial.Investment.Domain/Rules/TransactionReplayOrder.cs` (a `Sort` entry
      point) from the ordering logic T003 added inline to `Transactions.cs`, and repoint
      `Financial.Investment.Domain/Entities/Transactions.cs` to consume it, so `Transactions`, the
      coverage rule and the future report (Phase 10) share one owner of the FR-002 ordering (R12)
- [X] T011 [US2] Implement `Financial.Investment.Domain/Rules/SaleCoverageRule.cs`: a pure,
      non-throwing `FindFirstUncoveredSale(IEnumerable<Transaction>)` built on `TransactionReplayOrder`,
      walking the whole candidate sequence — not just the changed transaction (FR-009..FR-012, FR-065,
      FR-066)
- [X] T012 [US2] Add strict `RecordTransaction` / `ReviseTransaction` / `RetractTransaction` methods to
      `Financial.Investment.Domain/Entities/Asset.cs` that run `SaleCoverageRule` then delegate to the
      existing tolerant `AddTransaction`/`UpdateTransaction`/`RemoveTransaction`, raising
      `InvestmentRuleViolationException` on a breach (FR-009, FR-010, FR-013..FR-015; R13) — leave the
      tolerant methods and `AddTransactions` untouched so loading and bulk import stay tolerant
      (FR-013, FR-014)
- [X] T013 [US2] Extend
      `Tests/Financial.Investment.Domain.Tests/Domain/AssetTests.cs` for the new strict methods:
      refusal on oversell/edit/delete with the held-quantity message; confirm existing tolerant-method
      fixtures (`PositionType_NegativeQuantity_ReturnsShort`, short-position builders) are untouched —
      if any of them needs editing, the check was put on the wrong method (R13)
- [X] T014 [US2] Repoint `Financial.Investment.Application/Services/TransactionService.cs` to call the
      new strict `Asset` methods inside the `ApplyAndSaveAsync` mutation lambda, so a thrown refusal
      means serialize-and-write never runs and nothing changes in memory (R12)
- [X] T015 [US2] Extend
      `Tests/Financial.Investment.Application.Tests/Services/TransactionServiceMutationTests.cs` with
      refusal cases for record/edit/delete, including the "later transaction left short" message naming
      the later sale and the shortfall, using `StubInvestmentRepository`

### Increment 4 — Both front ends surface the refusal, incl. the WPF crash fix

- [X] T016 [US2] Add `try`/`catch` around `Add`/`Update`/`Delete` in
      `Financial.App/ViewModels/Investment/TransactionsTabViewModel.cs`, catching
      `InvestmentRuleViolationException` and surfacing `ex.Message`, a generic message otherwise,
      following the `MainNavigationViewModelBase` precedent (FR-062; R16 — this is a pre-existing
      latent crash this feature makes reachable)
- [X] T017 [US2] Add WPF view-model tests for `TransactionsTabViewModel` covering the refused
      add/edit/delete state: the app does not crash, entered data is preserved, selection goes through
      `TreeNodeViewModel.IsSelected` (never by assigning `SelectedNode` directly)
- [X] T018 [US2] Verify `Financial.Web`'s existing `financialApiClient`/`saveError` path surfaces the
      409 ProblemDetails `detail` verbatim; add a regression test under
      `Financial.Web/src/components/__tests__/` if the refusal path isn't already covered
- [X] T019 [US2] Run quickstart Scenario 2 from both front ends (`dotnet run --project Financial.Api`
      and `Financial.App`) against the temp copy: app starts, lists all 160 holdings, opens the 3
      historic holdings that already breach the rule; a fresh oversell/edit/delete is refused with the
      held quantity named to full precision (SC-003, SC-004)

**Checkpoint**: Impossible sales are refused everywhere; existing breaches still load and are
correctable; WPF no longer crashes on the refusal.

---

## Phase 5: User Story 3 - One meaning for "the amount I have invested" (Priority: P3)

**Goal**: One definition of invested amount per scope (cost of open units in Active, total purchases in
Historic), used identically by the row, the total, and the allocation chart; portfolio/broker totals
reconcile to the sum of their rows in both scopes.

**Independent Test**: Partially sell a position, confirm invested equals cost of units remaining;
confirm a portfolio's total equals the sum of its rows in both scopes; confirm the allocation chart uses
the same figures.

### Increment 5 — Separate the three figures; correct invested; SummaryService reconciles

- [X] T020 [P] [US3] Write Domain tests in new
      `Tests/Financial.Investment.Domain.Tests/Domain/OpenPositionCostCalculatorTests.cs`:
      `CostOfUnitsHeld` clamps a negative product to zero (FR-021)
- [X] T021 [US3] Implement `Financial.Investment.Domain/Rules/OpenPositionCostCalculator.cs`:
      `CostOfUnitsHeld(asset) => Math.Max(0m, quantity * averagePrice)`, scope-free (FR-019, FR-021; R7)
- [X] T022 [US3] Extract `AssetTotals` into its own file
      `Financial.Investment.Application/Services/AssetTotals.cs` with a static `For(asset)` factory and
      a new `OpenPositionCost` field, removing the inline declaration from
      `Financial.Investment.Application/Services/PortfolioAssetSummaryBuilder.cs` so
      `BrokerBreakdownService` can reach it instead of re-deriving its own tuple (R7)
- [X] T023 [US3] Replace `Financial.Investment.Application/Services/AssetInvestedAmountSelector.cs`
      with `Financial.Investment.Application/Services/AssetAmountBases.cs`: three separately identified
      fields — `InvestedAmount`, `WeightBasis`, `IncomeYieldBasis` — keyed by `InvestmentScope`, never
      by quantity; `InvestedAmount`/`IncomeYieldBasis` become the corrected cost-of-open-units figure in
      Active, total purchases in Historic; `WeightBasis` stays on the *old* cost formula this increment
      (FR-018..FR-021, FR-025, amended FR-049; R7)
- [X] T024 [P] [US3] Write Application tests for `AssetAmountBases` covering Active vs. Historic for
      `InvestedAmount` and `IncomeYieldBasis`, non-negative invested (FR-021), and `WeightBasis`
      unchanged this increment
- [X] T025 [US3] Repoint
      `Financial.Investment.Application/Services/PortfolioAssetSummaryBuilder.cs` and
      `Financial.Investment.Application/Services/PortfolioAssetSummaryService.cs` to
      `AssetAmountBases.InvestedAmount` and `AssetTotals.OpenPositionCost`; delete the `Quantity != 0`
      Active filter and its now-factually-wrong XML comment (FR-023; R9)
- [X] T026 [US3] Repoint `Financial.Investment.Application/Services/SummaryService.cs` to sum
      per-asset `AssetAmountBases.InvestedAmount` instead of the aggregate `ΣBought − ΣSold` formula
      (FR-018, FR-022; R9)
- [X] T027 [US3] Repoint `Financial.Investment.Application/Services/BrokerBreakdownService.cs` (and
      `BrokerBreakdownBuilder.cs` if it independently derives the tuple) to `AssetTotals.For` /
      `AssetAmountBases`, so the allocation chart includes exactly the holdings with `InvestedAmount >
      0` (FR-024, FR-070)
- [X] T028 [US3] Extend
      `Tests/Financial.Investment.Application.Tests/Services/SummaryServiceTests.cs` and
      `Tests/Financial.Investment.Application.Tests/Services/BrokerBreakdownServiceTests.cs`:
      portfolio/broker total equals sum of rows in both scopes (FR-022); a sold-to-zero Active holding
      treated identically by rows and total (FR-023); income-yield percentages measured against the
      corrected cost figure (amended FR-049, SC-011)
- [X] T029 [US3] Add an AC-tracing integration test through the API host asserting the FR-019
      redefinition's actual **value** for a partially-sold holding — not just its shape, since the
      contract snapshot, `openapiFreshness.test.ts` and `tsc -b` are all blind to a same-shape
      meaning-change (contracts/README.md §3)
- [X] T030 [US3] Confirm `openapiFreshness.test.ts` still passes; regenerate the OpenAPI snapshot and
      `Financial.Web` generated types only if a DTO shape actually changed this increment (`TotalInvested`
      keeps its name and type)

### Increment 6 — Front-end footers read the server total

- [X] T031 [US3] Lift the `useAggregatedSummary()` call out of `AggregatedSummaryTab` into
      `Financial.Web/src/components/PortfolioSummaryTab.tsx` and pass the summary down, so header and
      footer read one response object; stop deriving Total Invested and Total Credits client-side
      (FR-061; R10)
- [X] T032 [US3] In `Financial.App`, feed the already-loaded `summary` parameter of
      `LoadPortfolioSummary` into the footer so Total Invested and Total Credits read the server figure
      instead of a client sum (FR-061; R10)
- [X] T033 [US3] Update `Financial.Web/src/components/__tests__/PortfolioSummaryTab.test.tsx` and the
      corresponding WPF view-model tests to assert the footer equals the header's server-reported total
- [X] T034 [US3] Run quickstart Scenario 3 against the temp copy and record the expected movements in
      the PR body: 5 of 28 active invested amounts move (one −683.09→28.65), 4 active portfolio totals
      move by up to 711.74, all 15 historic portfolio totals move (e.g. XPI/FII 2,949.87→61,413.07), the
      allocation chart gains one slice, 4 yield percentages change (one flips sign) (SC-005)

**Checkpoint**: One definition of "invested" everywhere; totals reconcile to rows in both scopes;
front-end footers no longer diverge from headers.

---

## Phase 6: User Story 6 (part 1 of 2) - Compatibility boundary for market-based allocation (Priority: P6)

**Goal**: `PortfolioWeight` becomes nullable end-to-end *before* it can ever actually be null, forcing
both grids to declare their unknown-rendering ahead of Phase 9 repointing the weight basis to market
value. Sequenced here per plan.md's non-negotiable "5 before 12" / "7 before 12" ordering.

**Independent Test**: `tsc -b` and `csc` both fail at the two grid sites until each declares its
unknown-rendering; the value itself is still always non-null after this phase.

### Increment 7 — Compatibility boundary: weight nullable + one format, value still never null

- [X] T035 [US6] Widen `PortfolioWeight` from `decimal` to `decimal?` in
      `Financial.Investment.Application/DTOs/PortfolioAssetSummaryItemDTO.cs` (FR-047; R8)
- [X] T036 [US6] Regenerate the OpenAPI snapshot
      (`$env:UPDATE_OPENAPI_SNAPSHOT=1; dotnet test Tests/Financial.Api.Tests; Remove-Item
      Env:\UPDATE_OPENAPI_SNAPSHOT`) and `Financial.Web`'s generated types (`npm run
      generate-api-types`); commit both
- [X] T037 [US6] Fix the resulting compile break in
      `Financial.Web/src/components/PortfolioSummaryTab.tsx`: render `—` for a null weight, format to 2
      decimal places (matching `lastMonthCreditsPercent` beside it), and give the sort accessor a
      defined position for unknowns (R8)
- [X] T038 [US6] Fix the resulting compile break in
      `Financial.App/ViewModels/Investment/PortfolioAssetSummaryRowViewModel.cs`: render `—` for a null
      weight and switch its format string from 1 dp to 2 dp (R8, FR-051)
- [X] T039 [US6] Confirm `Financial.Web/src/components/AssetSummaryTab.tsx` and its WPF detail-view
      equivalent (already null-safe, gated to Historic scope) render at 2 dp consistently with the grids
      (FR-051)
- [X] T040 [US6] Extend `Financial.Web/src/components/__tests__/PortfolioSummaryTab.test.tsx` and the
      WPF row view-model tests for null-weight rendering and 2-dp formatting; add a parity test at a
      `.005` rounding boundary comparing WPF `F2` (`MidpointRounding.AwayFromZero` on `decimal`) against
      `Intl.NumberFormat` (half-even on `double`) — flagged as a real risk in R8 under FR-060's
      "identical to the last digit displayed"

**Checkpoint**: `PortfolioWeight` is nullable everywhere while still always non-null; both grids declare
unknown-rendering ahead of the value ever actually being unknown.

---

## Phase 7: User Story 4 - What a holding is worth, worked out once by the application (Priority: P4)

**Goal**: Market value, cost of units held, unrealised gain, price-only return and total return are
computed once by the application and rendered identically by both front ends, valued at the most recent
price on or before today, with unavailable and stale states distinguishable and never nought.

**Independent Test**: Open the same priced holding in both front ends → identical figures, each naming
the as-of date. Open an unpriced holding → both say unavailable, not nought.

### Increment 8 — HoldingValuationCalculator + service + holding-level DTO fields

- [X] T041 [P] [US4] Write Domain tests in new
      `Tests/Financial.Investment.Domain.Tests/Domain/HoldingValuationCalculatorTests.cs`: the
      invariants `UnrealisedGain is null ⟺ MarketValue is null` and `MarketValue is null ⟺
      PriceAsOfDate is null` (FR-037); staleness at the "strictly before the most recent weekday"
      boundary — a Friday price current on Monday, stale on Tuesday (FR-032); `NotMarkedToMarket` for
      Historic never reports unrealised gain (FR-034, FR-075)
- [X] T042 [US4] Implement the `HoldingValuation` record and `HoldingValuationCalculator` in
      `Financial.Investment.Domain/Rules/HoldingValuationCalculator.cs` with two named entry points —
      `Calculate(quantity, averagePrice, price, valuationDate)` and `NotMarkedToMarket(quantity,
      averagePrice)` — rather than a `bool` flag (FR-026..FR-037, FR-075; R1, R2, R4)
- [X] T043 [P] [US4] Write Domain tests for `GetPriceAsOf` in
      `Tests/Financial.Investment.Domain.Tests/Domain/AssetTests.cs`: most-recent-on-or-before
      semantics, a future-dated price ignored, no price returns null
- [X] T044 [US4] Add `GetPriceAsOf(DateOnly date)` to
      `Financial.Investment.Domain/Entities/Asset.cs`; do **not** sort `_priceHistory` (would re-open
      the "Collection was modified" concurrency bug `UpsertPriceEntry` fixed) (FR-028, FR-033; R3)
- [X] T045 [US4] Define `Financial.Investment.Application/Interfaces/IHoldingValuationService.cs` and
      implement `Financial.Investment.Application/Services/HoldingValuationService.cs` — an optional
      trailing `TimeProvider? timeProvider = null` defaulting to `TimeProvider.System`, calling
      `GetPriceAsOf` and dispatching Active/Historic to the two Domain entry points (R4)
- [X] T046 [US4] Register `IHoldingValuationService` and `services.TryAddSingleton(TimeProvider.System)`
      in
      `Financial.Investment.Application/DependencyInjection/InvestmentApplicationServiceCollectionExtensions.cs`
      (R4 — `TryAdd` avoids a duplicate with CashFlow's unconditional registration)
- [X] T047 [US4] Write Application tests for `HoldingValuationService` using `FakeTimeProvider` and
      `StubInvestmentRepository`, including the observability contract (success span with
      `OperationResult == Success`; failure records the exception and rethrows without logging, via
      `RecordingTelemetryTracer`) per `testing-guide-Financial`
- [X] T048 [US4] Add `MarketValue`, `CostOfUnitsHeld`, `UnrealisedGain`, `PriceAsOfDate`,
      `IsPriceStale`, `PriceOnlyReturn`, `TotalReturn` to
      `Financial.Investment.Application/DTOs/PortfolioAssetSummaryItemDTO.cs` and
      `Financial.Investment.Application/DTOs/AssetDetailsDTO.cs`
- [X] T049 [US4] Wire `Financial.Investment.Application/Services/PortfolioAssetSummaryBuilder.cs` /
      `PortfolioAssetSummaryService.cs` and the asset-details build path to call
      `IHoldingValuationService` and populate the new DTO fields
- [X] T050 [US4] Regenerate the OpenAPI snapshot and `Financial.Web` generated types for the DTO
      additions; commit both

### Increment 9 — Both front ends switch to server-computed valuation, incl. refresh-after-fetch

- [X] T051 [US4] Remove the ten sites (5 web, 2 desktop deriving market value; 3 further deriving cost
      of units held — SC-007) that derive a value from a price and a quantity of their own, replacing
      each with the corresponding server-computed DTO field from T048 (FR-036, FR-061) — search
      `Financial.Web/src` and `Financial.App/ViewModels/Investment` for price×quantity-shaped
      arithmetic before editing
- [X] T052 [US4] Render unavailable (`—`) and stale-price markers distinctly from computed values at
      every site touched in T051, in both front ends (FR-030, FR-032, FR-064)
- [X] T053 [US4] Wire a summary refetch after a bulk price fetch completes in both the
      `Financial.Web` price-fetch hook and the `Financial.App`
      `AssetPriceFetchViewModel`/`AssetPriceFetchResult` path, so valuation reflects a newly fetched
      price without further user action (FR-035, FR-063 — flagged in research.md as the step most
      likely to be missed)
- [X] T054 [US4] Update `Financial.Web/src/components/__tests__/AssetSummaryTab.test.tsx`,
      `PortfolioSummaryTab.test.tsx` and the corresponding WPF view-model tests to assert
      server-computed values render verbatim with no remaining client arithmetic
- [X] T055 [US4] Add a parity AC-tracing test confirming `Financial.Web` and `Financial.App` report
      identical market value, cost of units held, unrealised gain and both returns for the same holding,
      to the last digit displayed, each naming the as-of date (FR-060, SC-006)
- [X] T056 [US4] Run quickstart Scenario 4 against the temp copy: a priced holding parity-checked in
      both front ends; one of the 7 unpriced holdings reports unavailable in both, never nought; a
      Friday-priced holding read on Monday (current) vs. Tuesday (stale), pinned with `FakeTimeProvider`

**Checkpoint**: Market value, cost, unrealised gain and return have one owner; both front ends render
identically and refresh after a fetch.

---

## Phase 8: User Story 5 - What a portfolio and a broker are worth, and what they returned (Priority: P5)

**Goal**: A portfolio's and a broker's market value and both rates of return sum/solve over their
holdings; an incomplete total states its shortfall and withholds its rate rather than hiding it; no
total spans more than one broker.

**Independent Test**: A fully-valued portfolio's total equals the sum of its rows and its rate solves;
a portfolio with unvalued holdings publishes a marked-incomplete total, states the count, and withholds
both rates.

### Increment 10 — Level aggregation + AggregatedSummaryDTO fields

- [X] T057 [P] [US5] Write Application tests for the cash-flow concatenation helper: plain
      concatenation, no same-date merging (R5 — merging can only reduce the XIRR gate's entry count and
      can turn a solvable sign-changed series into a null)
- [X] T058 [US5] Implement the concatenation helper alongside
      `Financial.Investment.Application/Services/AssetCashFlowBuilder.cs`, aggregating a portfolio's or
      broker's holdings' dated amounts both excluding and including income (FR-042, FR-067, FR-068; R5)
- [X] T059 [US5] Add an `asOf` overload to
      `Financial.Investment.Application/Interfaces/IXirrCalculationService.cs` and
      `Financial.Investment.Application/Services/XirrCalculationService.cs`; the existing two-arg
      member delegates with `DateTime.Today` — `XirrController` and its OpenAPI path stay unchanged (R5)
- [X] T060 [US5] Add `MarketValue` (`decimal?`), `HoldingCount` (`int`), `UnvaluedHoldingCount` (`int`),
      `PriceOnlyReturn` (`decimal?`), `TotalReturn` (`decimal?`) to
      `Financial.Investment.Application/DTOs/AggregatedSummaryDTO.cs` (FR-038..FR-043; R6)
- [X] T061 [US5] Wire `Financial.Investment.Application/Services/SummaryService.cs` to populate the
      five fields per the state table in data-model.md §5: `MarketValue` null only when holdings exist
      and none could be valued, `0m` with `HoldingCount == 0` for an empty portfolio; both returns
      `null` whenever `UnvaluedHoldingCount > 0`; no all-brokers aggregation entry point (FR-039..FR-045;
      R6)
- [X] T062 [US5] Extend `Tests/Financial.Investment.Application.Tests/Services/SummaryServiceTests.cs`
      for the four level-total states (empty / fully valued / partially valued / nothing valuable) and
      confirm no total spans more than one broker (FR-045)
- [X] T063 [US5] Regenerate the OpenAPI snapshot and `Financial.Web` generated types for the
      `AggregatedSummaryDTO` additions; commit both

### Increment 11 — Both front ends render level totals and returns

- [X] T064 [US5] Render market value, price-only return and total return on
      `Financial.Web/src/components/AggregatedSummaryTab.tsx`, with an incomplete-total disclosure
      stating the unvalued holding count and withheld returns when `UnvaluedHoldingCount > 0` (FR-040,
      FR-042)
- [X] T065 [US5] Render the equivalent in `Financial.App`'s portfolio/broker summary view, distinguishing
      an empty portfolio from one where nothing can be valued (FR-041)
- [X] T066 [US5] Update `Financial.Web/src/components/__tests__/AggregatedSummaryTab.test.tsx` and the
      corresponding WPF tests for the four level-total states (`useAggregatedSummary.ts` itself is an
      unmodified passthrough, so `useAggregatedSummary.test.ts` needed no change)
- [X] T067 [US5] Run quickstart Scenario 5 against the temp copy: Trading 212/ETF ISA, Trading 212/ETF
      SIPP and XPI/Previdencia (no valued holding at all) each distinguishable from an empty portfolio; a
      partially-valued portfolio marked incomplete with returns withheld; no cross-broker total anywhere

**Checkpoint**: Portfolio and broker totals and returns are computed once; incompleteness is disclosed,
never hidden.

---

## Phase 9: User Story 6 (part 2 of 2) - Weight basis → market value + shortfall disclosure (Priority: P6)

**Goal**: A holding's portfolio share is derived from market value in Active Investments (cost in
Historic); an unvaluable holding's share is unknown, never 0%; the portfolio discloses once that its
shares don't total 100%, or that no share can be computed; income yield stays labelled and measured on
cost, unaffected.

**Independent Test**: An appreciated holding's share exceeds its cost-based share; an unvaluable
holding's share reads unknown, and the portfolio states the shortfall once; income-yield percentages are
numerically unchanged and labelled "on cost".

### Increment 12 — Weight basis → market value + shortfall disclosure

- [X] T068 [US6] Repoint `AssetAmountBases.WeightBasis` (from T023) from the cost-based formula to
      market value in Active Investments; Historic stays on cost (FR-046, FR-050; R7) —
      `Financial.Investment.Application/Services/AssetAmountBases.cs`
- [X] T069 [US6] Resolved the open item from research.md **with no wire change**: `AggregatedSummaryDTO`
      already carries `HoldingCount`/`UnvaluedHoldingCount`/`MarketValue` (shipped in Increment 10) for
      the exact same scope and asset set as `PortfolioAssetSummaryItemDTO`'s array, and in Active
      Investments a holding's share is unknown exactly when its market value is unavailable — the same
      predicate `UnvaluedHoldingCount` already counts. Both front ends already fetch this DTO alongside
      the assets array (Increment 11), so no OpenAPI snapshot or type regeneration was needed.
- [X] T070 [US6] Wired
      `Financial.Investment.Application/Services/PortfolioAssetSummaryBuilder.cs` to pass each holding's
      `HoldingValuation.MarketValue` into `AssetAmountBases.For` and to report a null (unknown) share —
      via a nullable-aware `CalculateWeight` — for an unvaluable holding instead of 0% (FR-047, FR-052);
      the portfolio-level shortfall disclosure is computed client-side from the T069 fields (FR-048)
- [X] T071 [US6] Updated `Financial.Web/src/components/PortfolioSummaryTab.tsx` (new
      `incompleteShareBasisMessage` + `.portfolio-summary__share-notice`, Active scope only) and
      `Financial.App/ViewModels/Investment/AssetDetailsViewModel.cs` (`HasIncompleteShareBasis` /
      `IncompleteShareBasisMessage`, rendered in `PortfolioSummaryView.xaml`'s `PortfolioSummaryTemplate`)
      for the once-per-portfolio shortfall disclosure; the market-based share and unknown-share marker
      needed no rendering change — both grids already handled a null `PortfolioWeight` since Increment 7
- [X] T072 [US6] Extended
      `Tests/Financial.Investment.Application.Tests/Services/PortfolioAssetSummaryServiceTests.cs`
      (market-based weight, appreciated-vs-cost divergence, unknown share for one/all unpriced holdings)
      and `Tests/Financial.Api.Tests/SummaryEndpointsTests.cs` (renamed the now-outdated
      `..._WeightStaysOnPriorBasis` AC test to assert the new null-share behaviour against the shared
      fixture), plus `PortfolioSummaryTab.test.tsx` and `AssetDetailsViewModelPortfolioSummaryTests.cs`
      for the portfolio-level disclosure states
- [X] T073 [US6] Added `GetPortfolioAssetsSummary_ActiveScope_IncomeYieldPercentagesStayOnCost_UnaffectedByMarketValue`
      (Application) confirming yield-on-cost is unaffected by a large market-value/cost divergence; no
      source change was needed since `AssetAmountBases.IncomeYieldBasis` was already untouched by T068
- [X] T074 [US6] Ran quickstart Scenario 6 against the scratchpad copy via the running API: XPI/Acoes
      (partially valued) shows real weights on the 5 priced holdings and a null weight on the one
      unpriced holding (Guepardo), with TAEE3's market-based share (62.92%) diverging from its cost-based
      share; Trading 212/ETF ISA (nothing valuable) shows every holding's weight and market value null;
      `GetPortfolioAssetsSummary_ActiveScope_IncomeYieldPercentagesStayOnCost_...` confirms yield-on-cost
      unaffected; both grids already render weight at 2 dp (R8/Increment 7)

**Checkpoint**: Allocation reflects market value; unvaluable holdings never render as 0%; yield-on-cost
is unaffected.

---

## Phase 10: User Story 7 - Knowing what the application cannot calculate (Priority: P7)

**Goal**: A report names every holding the application cannot fully calculate — sales exceeding
purchases with shortfall, open holdings with no price, unclassified holdings split by scope, historic
holdings still carrying a quantity — without writing anything, and without inferring a classification.

**Independent Test**: Run the report against a copy of the data; confirm the four named categories and
counts; confirm two runs produce a byte-identical file; classify a holding and confirm it drops from the
report without a restart.

### Increment 13 — Data-quality report (logic in Application, thin `Tools/` printer)

- [ ] T075 [P] [US7] Write Application tests for the report logic: names the 3 sales-exceed-purchases
      holdings with shortfall (FR-054), the 7 unpriced open holdings (FR-055), the 90 unclassified split
      3 active/87 historic (FR-056), the 4 historic holdings still carrying a quantity (FR-074); treats
      Bitcoin/BOVA11/IVVB11 as classified (FR-057); links an unclassified holding's missing price to its
      missing classification for bonds/crypto (FR-073); never writes to the repository (FR-059); running
      twice produces the same result
- [ ] T076 [US7] Implement the report logic in a new
      `Financial.Investment.Application/Services/DataQualityReportService.cs`, reusing
      `SaleCoverageRule`/`TransactionReplayOrder` for FR-054, never inferring, guessing or writing an
      asset class or local type code (FR-053, FR-058, FR-071; R15)
- [ ] T077 [US7] Create `Tools/InvestmentDataQualityReport/InvestmentDataQualityReport.csproj` as a
      thin `Program.cs` resolving the Application service and printing the report, following the
      `Tools/InvestmentSpreadsheetImport` shape; register it in `Financial.slnx`
- [ ] T078 [US7] Add `Tests/Financial.InvestmentDataQualityReport.Tests` (plain `net10.0` xUnit,
      one `ProjectReference`, hand-written stubs, no mocking framework), register it in `Financial.slnx`,
      and add its explicit assembly entry + rationale comment to `coverlet.runsettings` (R15 — `Tools/*`
      is not wildcard-excluded and still needs tests even where excluded from the coverage gate)
- [ ] T079 [US7] Make correcting a holding's country and local type code through the asset-admin edit
      path re-derive its class — today only creation does this — so the report reflects a classification
      immediately with no restart (FR-072)
- [ ] T080 [US7] Extend the asset-admin edit tests for class re-derivation on edit; run quickstart
      Scenario 7 against the temp copy: run the report twice and diff against the original
      (byte-identical); classify a holding through the app and confirm it drops from the report without a
      restart

**Checkpoint**: The data-quality report is available, accurate against current data, and writes nothing.

---

## Phase 11: Polish & Cross-Cutting Concerns

- [ ] T081 [P] Run `docker-compose up` and confirm the app starts under production config with every
      increment merged (Principle VIII)
- [ ] T082 [P] Run `cd Financial.Web && npm run smoke-test`; it asserts a CashFlow figure, so treat a
      green run as evidence the app boots, not that this feature works
- [ ] T083 Walk `docs/ui/review-checklist.md` against every touched view — `AssetSummaryTab`,
      `PortfolioSummaryTab`, `AggregatedSummaryTab`, `PortfolioAssetSummaryRowViewModel`,
      `AssetDetailsViewModel`, `TransactionsTabViewModel` — in both front ends, per `docs/rules/ui.md`
      §Scope of compliance
- [ ] T084 [P] Run `dotnet test --settings coverlet.runsettings --results-directory TestResults` and
      `cd Financial.Web && npm run test:coverage`; confirm every job's coverage band is amber (≥90%) or
      better; note any yellow band's uncovered lines in the relevant PR body
- [ ] T085 Re-run the full quickstart scenario list (1–7) end to end against the temp copy as a final
      check before closing the feature; confirm `main` was deployable after every individual increment

---

## Dependencies & Execution Order

### Phase Dependencies (non-negotiable — see plan.md §Increment sequence)

- **Setup (Phase 1)**: no dependencies
- **US1 (Phase 3)**: no dependency on another story; everything else depends on it (the basis every
  later figure reads)
- **US2 (Phase 4)**: depends on US1 ("how many units held on a date" is undefined before date-ordered
  replay)
- **US3 (Phase 5)**: depends on US1 (cost of units held is derived from the replayed quantity/average
  price)
- **US6 part 1 (Phase 6)**: depends on US3 landing first is *not* required by itself, but must land
  before US6 part 2 (Phase 9) and, per plan.md, is sequenced after US3
- **US4 (Phase 7)**: depends on US1 (basis) and US3 (invested figure it subtracts against)
- **US5 (Phase 8)**: depends on US4 (sums per-holding valuations) and, transitively, US3 (`5 before 10`)
- **US6 part 2 (Phase 9)**: depends on US3 (`5 before 12`) and US6 part 1 (`7 before 12`)
- **US7 (Phase 10)**: no dependency on another story's *output*, but reuses `SaleCoverageRule` /
  `TransactionReplayOrder` from US2 and is sequenced last because it can be deferred without stranding
  anything
- **Polish (Phase 11)**: depends on every story above being complete

### Within Each Increment

- Domain tests before Domain implementation (TDD)
- Domain rules before the Application services that consume them
- Application services before DTO wiring
- DTO wiring before an OpenAPI snapshot regeneration
- Snapshot regeneration before front-end type regeneration
- Both front ends' implementation before their tests are updated
- Quickstart scenario run last, as the increment's own acceptance gate

### Parallel Opportunities

- T008 and T009 (different new test files, no shared dependency)
- T020 and T024 are `[P]` within their increment where they touch different files
- T041 and T043 (different test files)
- T057 (helper tests) can start before T058's implementation is finished, but not before T057 itself
- T081, T082, T084 in Polish (independent verification passes)
- Across stories: **do not** parallelize phases — the ordering above is load-bearing, not a suggestion

---

## Parallel Example: Increment 3 (User Story 2)

```bash
# Launch the two new Domain test files together — different files, no shared dependency:
Task: "Write Domain tests in Tests/Financial.Investment.Domain.Tests/Domain/TransactionReplayOrderTests.cs"
Task: "Write Domain tests in Tests/Financial.Investment.Domain.Tests/Domain/SaleCoverageRuleTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1: Setup
2. Complete Phase 3: User Story 1 (Increments 1–2)
3. **STOP and VALIDATE**: run quickstart Scenario 1 against the temp copy; confirm exactly Bitcoin and
   AGNC (AGNC) 2 ISA move and the other 158 holdings don't
4. This alone is deployable and already fixes the silent-corruption defect the spec names as the
   highest-priority story

### Incremental Delivery

Follow the phases in order — 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10 — one PR per increment (13 PRs total, each
≤8 non-test code files per plan.md), validating with the matching quickstart scenario before moving on.
Do not reorder: the dependency table above is derived from the plan's stated non-negotiable constraints
(`5 before 10`, `5 before 12`, `7 before 12`, `8 before 10`, `3 and 4 together`), not from convenience.

### Solo Developer Strategy

This feature does not support a parallel-team split the way the template's default does — the stories
are a dependency chain, not independent slices. Work Phase 3 through Phase 10 in order; the only
in-phase parallelism worth taking is between independent new test files within the same increment.

---

## Notes

- [P] tasks = different files, no dependency on an incomplete task
- [Story] label maps every task to its spec.md user story for traceability
- Every "Repoint …" task in Increment 3/5/8/10/12 is also where an existing test file gets extended —
  do not skip the test extension in favour of only the production edit
- Every DTO change task is immediately followed by a snapshot/type-regeneration task — do not batch
  these across increments, or `openapiFreshness.test.ts` fails on the wrong PR
- Never run any of the quickstart scenarios or the report against `data/data-investment.json` directly
  — always against the temp copy from T001
- Commit after each task or logical group; stop at each phase checkpoint to validate the story
  independently before starting the next
