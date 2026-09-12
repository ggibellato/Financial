---
description: "Task list for Transaction and Income Event Vocabulary"
---

# Tasks: Transaction and Income Event Vocabulary

**Input**: Design documents from `/specs/004-transaction-income-vocabulary/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contract.md, quickstart.md

**Tests**: Included — Constitution Principle V ("No feature is complete without tests... unit tests
required, integration tests where applicable") makes tests mandatory in this codebase, not optional.

**Organization**: Tasks are grouped by user story. Per spec.md's own "Why this priority" narrative,
the four stories are **strictly sequential** (US2 depends on US1's `Transaction` widening; US3
depends on US1+US2's `NetCash`/`NetAmount`; US4 depends on US1–US3 existing before there is anything
to render) — this is not the usual "mostly independent, parallelizable" spec-kit default, and the
Dependencies section below states this explicitly.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1–US4, matching spec.md's priorities. Setup/Foundational/Polish tasks carry no story label.

## Path Conventions

Existing repository layout (no new path convention introduced): `.NET` projects flat at repo root
(`Financial.Investment.{Domain,Application,Infrastructure}`, `Financial.Api`, `Financial.App`,
`Tools/*`), `Financial.Web/src/`, one test project per layer under `Tests/`.

---

## Phase 1: Setup

**Purpose**: Confirm a clean baseline before any change — this feature only widens existing files,
no new solution structure is needed at this stage (one new tool project is added in Phase 4, where
it becomes necessary).

- [ ] T001 Verify baseline is green on branch `004-transaction-income-vocabulary`: `dotnet build --configuration Release` and `dotnet test --settings coverlet.runsettings --results-directory TestResults` both pass before any change in this feature begins

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Widen `Transaction` and its supporting Domain rules — every user story depends on this
existing first. (`Credit` widening is scoped to US2, where it is the subject matter, not here.)

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T002 [P] Create `TransactionTypeEffects` in `Financial.Investment.Domain/Rules/TransactionTypeEffects.cs`: `QuantityEffect` enum (`Increase, Decrease, None`), `CashEffect` enum (`In, Out, None`), `TransactionTypeEffect` record, and `TransactionTypeEffects.For(Transaction.TransactionType)` mapping every type per data-model.md's table
- [ ] T003 Widen `Financial.Investment.Domain/Entities/Transaction.cs`: extend `TransactionType` enum with `Fee, Redemption, TransferIn, TransferOut, CapitalCall, ReturnOfCapital`; relax `ValidateQuantity`/`ValidateUnitPrice` to require `> 0` only when `TransactionTypeEffects.For(type).Quantity != QuantityEffect.None`, else require exactly `0`; add `Withheld` property (floored at 0, same pattern as `Fees`); replace `TotalPrice` with a type-aware `NetCash` computed property per research.md #2 (`CashEffect.Out` → `-(Gross+Fees+Withheld)`, `.In` → `Gross-Fees-Withheld`, `.None` → `-Fees`) (depends on T002)
- [ ] T004 Generalize `Apply`/`Recompute` in `Financial.Investment.Domain/Entities/Transactions.cs` per research.md #1: `QuantityEffect.Increase` types feed `AveragePrice` via `Gross+Fees` the way `Buy` does today; `QuantityEffect.Decrease` + `CashEffect != None` types (Sell, Redemption) realize gain/loss against `NetCash` proceeds; `QuantityEffect.Decrease` + `CashEffect.None` (TransferOut) realizes zero gain/loss, reducing quantity at the existing `AveragePrice`; `QuantityEffect.None` types have no effect on `AveragePrice`/`RealizedCapitalGain` (depends on T002, T003)
- [ ] T005 [P] Extend `Financial.Investment.Domain/Rules/SaleCoverageRule.cs`'s `FindFirstUncoveredSale` to treat any `QuantityEffect.Decrease` type as a covered "sale" (not only `Sell`), so `Redemption`/`TransferOut` share the oversell-refusal rule per FR-004 (depends on T002)
- [ ] T006 [P] Extend the tie-break in `Financial.Investment.Domain/Rules/TransactionReplayOrder.cs`'s `Sort`/`IsInOrder` from `Type == Sell` to `TransactionTypeEffects.For(type).Quantity == QuantityEffect.Decrease` (depends on T002)
- [ ] T007 Fix `Financial.Investment.Domain/Rules/AssetTotalsCalculator.cs`'s `CalculateTotals` per research.md #6a: bucket by `CashEffect` (`.Out` → `totalBought`, `.In` → `totalSold`, `.None` → neither) instead of `Type == Buy ? ... : ...` — the current `else` branch would otherwise silently misclassify every new type as a sale (depends on T002, T003)
- [ ] T008 Exclude `Transaction.NetCash` from JSON serialization in `Financial.Investment.Infrastructure/Persistence/InvestmentTypeInfoResolver.cs`'s `ExcludedProperties` set, replacing the existing `Transaction.TotalPrice` entry (depends on T003)
- [ ] T009 [P] Create `Tests/Financial.Investment.Domain.Tests/Domain/TransactionTypeEffectsTests.cs`: one case per `TransactionType` asserting its declared `QuantityEffect`/`CashEffect` (depends on T002)
- [ ] T010 [P] Extend `Tests/Financial.Investment.Domain.Tests/Domain/TransactionTests.cs` for the widened enum, the relaxed Quantity/UnitPrice validation per type, and `NetCash` (replacing every `TotalPrice` assertion) (depends on T003)
- [ ] T011 [P] Extend `Tests/Financial.Investment.Domain.Tests/Domain/TransactionsTests.cs` for average-price/realized-gain behavior of every new type, including the zero-gain-loss `TransferOut` case (depends on T004)
- [ ] T012 [P] Extend `Tests/Financial.Investment.Domain.Tests/Domain/SaleCoverageRuleTests.cs` for `Redemption`/`TransferOut` oversell coverage (depends on T005)
- [ ] T013 [P] Extend `Tests/Financial.Investment.Domain.Tests/Domain/TransactionReplayOrderTests.cs` for the widened tie-break (depends on T006)
- [ ] T014 [P] Extend `Tests/Financial.Investment.Domain.Tests/Domain/AssetTotalsCalculatorTests.cs` for the `CashEffect`-based bucketing, including a `TransferIn`/`TransferOut` case landing in neither bucket (depends on T007)
- [ ] T015 [P] Extend `Tests/Financial.Investment.Domain.Tests/Domain/AssetTests.cs`'s `RecordTransaction`/`ReviseTransaction`/`RetractTransaction` tests against the new types, including the oversell-refusal message for `Redemption`/`TransferOut` (depends on T004, T005)

**Checkpoint**: `Financial.Investment.Domain` builds; every Domain test passes; `Transaction` and its
supporting rules are fully widened and independently verified. No user story can proceed before this.

---

## Phase 3: User Story 1 - Recording a cash event that isn't a buy or a sell (Priority: P1) 🎯 MVP

**Goal**: Every new transaction type can be recorded and queried through the Application/API layer,
each with the correct quantity/cash effect; existing Buy/Sell behavior is unaffected.

**Independent Test**: Record one of each new transaction type against a real holding via the API;
confirm each appears in the transaction list with its correct type, correct effect on quantity (or
none), and correct effect on cash (in, out, or none); confirm an existing Buy/Sell entry is
unaffected.

### Implementation for User Story 1

- [ ] T016 [P] [US1] Update `Financial.Investment.Application/DTOs/TransactionDTO.cs`: remove `TotalPrice`, add `NetCash`
- [ ] T017 [P] [US1] Update `Financial.Investment.Application/DTOs/TransactionCreateDTO.cs` and `TransactionUpdateDTO.cs`: add `Withheld`
- [ ] T018 [US1] Update `Financial.Investment.Application/DTOs/TransactionSummaryItemDTO.cs`: rename `TotalPrice` to `NetCash` (depends on T016)
- [ ] T019 [US1] Update `Financial.Investment.Application/Services/NavigationMapper.cs`'s two transaction-mapping methods (`MapTransaction`, `MapTransactionSummaryItem`) to map `NetCash` instead of `TotalPrice` (depends on T016, T018)
- [ ] T020 [US1] Update `Financial.Investment.Application/Services/AssetCashFlowBuilder.cs`'s `BuildFromTransactions` to use `transaction.NetCash` instead of the `Buy`/`Sell` sign switch, so every type (including `Fee`/`CapitalCall`/etc.) contributes correctly to the existing (Gross) cash-flow series (depends on T016)

### Tests for User Story 1

- [ ] T021 [P] [US1] Extend `Tests/Financial.Investment.Application.Tests/Services/TransactionServiceMutationTests.cs`: add each new transaction type end-to-end through `TransactionService.AddTransactionAsync`, including a rejected oversell for `Redemption`/`TransferOut` (depends on T016, T017)
- [ ] T022 [P] [US1] Extend `Tests/Financial.Investment.Application.Tests/Services/TransactionServiceQueryTests.cs` for `NetCash` in query results (depends on T018, T019)
- [ ] T023 [P] [US1] Extend `Tests/Financial.Investment.Application.Tests/Services/AssetCashFlowBuilderTests.cs` for the `NetCash`-based amount calculation across every type (depends on T020)
- [ ] T024 [P] [US1] Extend `Tests/Financial.Api.Tests/TransactionEndpointsTests.cs`: POST each new type and assert the response's quantity/`NetCash`/type; assert `400` with the shortfall message for an oversold `Redemption`/`TransferOut` (depends on T016, T017)
- [ ] T025 [US1] Regenerate the OpenAPI snapshot (`UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests`, review the diff, unset the env var) for the widened `Type` enum and `TransactionDTO` shape; regenerate `Financial.Web`'s types (`npm run generate-api-types`) (depends on T024)

**Checkpoint**: US1 is independently testable via the API — every new transaction type records and
queries correctly; oversell parity holds; Buy/Sell is unaffected.

---

## Phase 4: User Story 2 - Seeing what was actually paid, withheld, and received (Priority: P2)

**Goal**: Gross/fees/withheld/net are visible for both transactions (built in US1) and income
events; the income-kind vocabulary is corrected (`Rent` → `SecuritiesLendingIncome`, `+Coupon`,
`+negative-value correction`), with the required migration for existing data.

**Independent Test**: Record an income payment with tax withheld at source; confirm gross, withheld,
and net all display distinctly and gross − withheld = net; confirm every existing income record
still displays correctly with withheld reported as none, and that every existing `Rent` record now
reads `SecuritiesLendingIncome` with an unchanged value.

### Implementation for User Story 2

- [ ] T026 [US2] Widen `Financial.Investment.Domain/Entities/Credit.cs`: rename `CreditType.Rent` to `SecuritiesLendingIncome`, add `Coupon`; relax `ValidateValue` from "invalid if `<= 0`" to "invalid only if `== 0`" (permits a negative correction, FR-015); add `Withheld` (same floor-at-magnitude posture as `Value`'s sign); add `NetAmount` computed property (`Value - Withheld`)
- [ ] T027 [P] [US2] Exclude `Credit.NetAmount` from JSON serialization in `Financial.Investment.Infrastructure/Persistence/InvestmentTypeInfoResolver.cs`'s `ExcludedProperties` set (depends on T026)
- [ ] T028 [US2] Create new console project `Tools/InvestmentTransactionIncomeVocabularyMigration/InvestmentTransactionIncomeVocabularyMigration.csproj` (`OutputType=Exe`, referencing `Financial.Investment.Domain/.Application/.Infrastructure`, `Financial.Shared.Abstractions`, `Financial.Shared.Infrastructure` — matching `Tools/InvestmentDataQualityReport.csproj`'s reference list, research.md #6) and register it in `Financial.slnx` (depends on T026)
- [ ] T029 [US2] Implement `TransactionIncomeVocabularyMigrator.cs` + `TransactionIncomeVocabularyMigrationSummary.cs` in the new project: raw-`JsonElement`/`Utf8JsonWriter` rewrite of every `Credit.Type` string `"Rent"` → `"SecuritiesLendingIncome"` across `ActiveBrokers` and `HistoricBrokers` (operating before the normal typed deserialization, since that would already fail on the strings this migration exists to fix), backing up the file first (depends on T028)
- [ ] T030 [US2] Implement `Program.cs` in the new project: data-file path as `args[0]` (default `data/data-investment.json`, matching `Tools/InvestmentDataQualityReport/Program.cs`'s convention), invoke the migrator, print the summary, non-zero exit on failure (depends on T029)
- [ ] T031 [US2] Update `Financial.Investment.Application/Services/AssetCashFlowBuilder.cs`'s credit-side amount (`c.Value`) to use `c.NetAmount`, so a withheld income is already reflected correctly in the existing (Gross-so-far) cash-flow series before US3 adds the explicit Gross/Net split (depends on T026)
- [ ] T032 [P] [US2] Update `Financial.Investment.Application/DTOs/CreditDTO.cs`: add `Withheld`, `NetAmount` (depends on T026)
- [ ] T033 [P] [US2] Update `Financial.Investment.Application/DTOs/CreditCreateDTO.cs` and `CreditUpdateDTO.cs`: add `Withheld` (depends on T026)
- [ ] T034 [US2] Update `Financial.Investment.Application/Services/NavigationMapper.cs`'s `MapCredit` to map `Withheld`/`NetAmount` (depends on T032)

### Tests for User Story 2

- [ ] T035 [P] [US2] Create `Tests/Financial.InvestmentTransactionIncomeVocabularyMigration.Tests/` (new test project, registered in `Financial.slnx`) with `TransactionIncomeVocabularyMigratorTests.cs`: rewrites every `Rent` row to `SecuritiesLendingIncome`, leaves every other field/value byte-for-byte unchanged, is idempotent on a second run against its own output (depends on T029)
- [ ] T036 [P] [US2] Extend `Tests/Financial.Investment.Domain.Tests/Domain/CreditTests.cs` for the renamed/added kinds, negative `Value` (correction), `Withheld`, `NetAmount`, and the "invalid only if zero" validation (depends on T026)
- [ ] T037 [P] [US2] Extend both `Tests/Financial.Investment.Application.Tests/Services/CreditServiceTests.cs` and `Tests/Financial.Investment.Infrastructure.Tests/Services/CreditServiceTests.cs` for `SecuritiesLendingIncome`, `Coupon`, a negative-value correction, and `Withheld`/`NetAmount` (depends on T032, T033)
- [ ] T038 [P] [US2] Extend `Tests/Financial.Api.Tests/CreditEndpointsTests.cs`: POST `SecuritiesLendingIncome`, `Coupon`, and a negative-value correction; assert `Withheld`/`NetAmount` in the response (depends on T032, T033)
- [ ] T039 [US2] Regenerate the OpenAPI snapshot for the widened `CreditDTO`/`Type` enum (same procedure as T025); regenerate `Financial.Web`'s types (depends on T038)
- [ ] T040 [US2] Run the migration tool against a temp copy of `data/data-investment.json` per `quickstart.md` §2 (verify the rewritten-row count and that no other field changed), then run it against the real file and **restart every process that reads it** (API, `Financial.App` if running) — a restart alone never runs the migration (depends on T029, T030, T035)

**Checkpoint**: US2 is independently testable — every income kind (including the corrected
vocabulary) and every transaction shows gross/withheld/net; the existing ~899 transactions / ~1,485
credits display unchanged (SC-003); the app is safely deployable post-migration (closes the
Constitution Principle VIII conditional from plan.md).

---

## Phase 5: User Story 3 - Seeing return with tax already taken into account (Priority: P3)

**Goal**: A distinct gross return and net-of-tax return are both visible at asset, portfolio, and
broker level.

**Independent Test**: Take a holding with at least one instance of tax withheld; confirm its
net-of-tax return differs from its gross return by an amount attributable exactly to the withheld
tax; confirm a holding with zero withholding shows the same figure for both (FR-018).

### Implementation for User Story 3

- [ ] T041 [US3] Add a net-of-tax build mode to `Financial.Investment.Application/Services/AssetCashFlowBuilder.cs` (e.g. `BuildNetOfTaxWithCredits`/`ConcatenateNetOfTaxWithCredits`) per research.md #4 — transaction amount = `NetCash` (already the case since T020), income amount = `NetAmount` (already the case since T031); the existing `BuildWithCredits`/`ConcatenateWithCredits` become the **Gross** series by dropping `Withheld` from the calculation (amount = `Gross ± Fees` / `Gross`) (depends on T020, T031)
- [ ] T042 [US3] Add `TotalReturnNetOfTax` to the `HoldingValuation` record in `Financial.Investment.Domain/Rules/HoldingValuationCalculator.cs`, and compute it in `Financial.Investment.Application/Services/HoldingValuationService.cs`'s `GetValuation` using T041's net series alongside the existing `TotalReturn` (depends on T041)
- [ ] T043 [US3] Add `TotalReturnNetOfTax` to `AggregatedSummaryDTO` and compute it in `Financial.Investment.Application/Services/SummaryService.cs`'s `Aggregate`, using T041's net series and Wave 0's existing null-when-incomplete rule (depends on T041)

### Tests for User Story 3

- [ ] T044 [P] [US3] Extend `Tests/Financial.Investment.Application.Tests/Services/AssetCashFlowBuilderTests.cs` for the Gross/Net split, including a case with `Withheld > 0` where the two series differ (depends on T041)
- [ ] T045 [P] [US3] Extend `Tests/Financial.Investment.Application.Tests/Services/HoldingValuationServiceTests.cs` for `TotalReturnNetOfTax`, including the "equal to `TotalReturn` when never withheld" case (FR-018) (depends on T042)
- [ ] T046 [P] [US3] Extend `Tests/Financial.Investment.Application.Tests/Services/SummaryServiceTests.cs` for `TotalReturnNetOfTax`, including the FR-018 equality case and the "null when a valuation is missing" case matching `TotalReturn`'s existing rule (depends on T043)
- [ ] T047 [US3] Confirm the exact DTO currently surfacing `HoldingValuation.TotalReturn` to `Financial.Api` (per contracts/api-contract.md's note — none was found as of the plan) and add `TotalReturnNetOfTax` alongside it there (depends on T042)
- [ ] T048 [US3] Regenerate the OpenAPI snapshot for `AggregatedSummaryDTO` and the asset-level valuation surface from T047 (same procedure as T025) (depends on T043, T047)
- [ ] T049 [P] [US3] Add an integration test in `Tests/Financial.Api.Tests` confirming a broker/portfolio summary endpoint returns equal `totalReturn`/`totalReturnNetOfTax` with no withholding, and differing values once a withheld income exists (depends on T048)

**Checkpoint**: US3 is independently testable — gross and net-of-tax return are both visible via the
API at every level Wave 0 established (FR-017/FR-018).

---

## Phase 6: User Story 4 - Recording and viewing the new event types from either front end (Priority: P4)

**Goal**: `Financial.Web` and `Financial.App` record/display every new transaction type, income
kind, and the gross/withheld/net breakdown identically — matching terminology, field order, and
figures (FR-021, SC-005). Per Constitution Principle III, the Web and WPF work below ships as one
increment, not sequentially — shipping one without the other shows two different figures for the
same holding, which is a parity regression, not an unfinished increment.

**Independent Test**: Record one new transaction type and one income event with withholding from the
web application; confirm the desktop application shows identical figures, terminology, and field
order for the same holding, and vice versa.

### Implementation for User Story 4 — shared backend surface

- [ ] T050 [US4] Add `GET /transactions/type-effects` to `Financial.Api/Controllers/TransactionsController.cs`, backed by a small query returning `TransactionTypeEffects`' table as a DTO list (contracts/api-contract.md) (depends on T002)
- [ ] T051 [P] [US4] Add an integration test for the new endpoint in `Tests/Financial.Api.Tests` (depends on T050)
- [ ] T052 [US4] Regenerate the OpenAPI snapshot + `Financial.Web`'s generated types for the new endpoint (same procedure as T025) (depends on T025, T039, T048, T050)

### Implementation for User Story 4 — Web (`Financial.Web`)

- [ ] T053 [P] [US4] Update `Financial.Web/src/hooks/useTransactions.ts`: widened `Type` union, `Withheld` field, `NetCash` replacing `totalPrice`, fetch the type-effects list (depends on T052)
- [ ] T054 [P] [US4] Update `Financial.Web/src/hooks/useCredits.ts`: widened `Type` union (`SecuritiesLendingIncome`, `Coupon`), `Withheld`/`NetAmount` (depends on T052)
- [ ] T055 [US4] Update `Financial.Web/src/components/TransactionsTab.tsx`: new type options in the entry form; hide Quantity/UnitPrice as required when the selected type's quantity effect is `None` (FR-022); display Gross/Fees/Withheld/Net columns (depends on T053)
- [ ] T056 [US4] Update `Financial.Web/src/components/CreditsTab.tsx`: renamed/added income-kind options ("Securities Lending Income", "Coupon"); allow a negative `Value` entry for a correction; display Gross/Withheld/Net (depends on T054)
- [ ] T057 [US4] Surface `totalReturn`/`totalReturnNetOfTax` with a distinct label (FR-020, never position/order alone) wherever the existing return figure is shown in `Financial.Web` (depends on T052)
- [ ] T058 [P] [US4] Add/extend Vitest component and hook tests for T053–T057 (co-located `__tests__`, matching existing convention) (depends on T053, T054, T055, T056, T057)

### Implementation for User Story 4 — WPF (`Financial.App`)

- [ ] T059 [P] [US4] Update `Financial.App/ViewModels/Investment/TransactionDialogViewModel.cs` and `TransactionDialogValidation.cs`: new type options, hide Quantity/UnitPrice when not applicable, `Withheld` field (depends on T002, T016, T017)
- [ ] T060 [P] [US4] Update `Financial.App/ViewModels/Investment/CreditDialogViewModel.cs` and `CreditDialogValidation.cs`: renamed/added income-kind options, negative-value correction entry, `Withheld` field (depends on T026, T032, T033)
- [ ] T061 [US4] Update `Financial.App/ViewModels/Investment/TransactionsTabViewModel.cs`: rename `TotalPrice` to `NetCash` in the aggregate-view tuple and bound columns (depends on T018)
- [ ] T062 [US4] Surface `TotalReturnNetOfTax` with a distinct label alongside the existing return figure in the relevant WPF views (portfolio/asset summary) (depends on T042, T043, T047)
- [ ] T063 [P] [US4] Extend `Tests/Financial.Presentation.Tests/ViewModels/TransactionDialogViewModelTests.cs`, `CreditDialogViewModelTests.cs`, and `CreditDialogValidationTests.cs` for T059–T062 (depends on T059, T060, T061, T062)

**Checkpoint**: US4 is independently testable — recording a new type or a withheld income from
either front end shows identically in the other (FR-021, SC-005).

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Whole-feature verification and documentation cleanup — depends on all four stories
being complete.

- [ ] T064 [P] Run `npm run lint && npm run build` in `Financial.Web` — `tsc -b` (inside `build`) is what catches any remaining call site reading the removed `totalPrice`/`Value`-as-always-positive assumptions
- [ ] T065 [P] Run `npm run smoke-test` (Playwright) against a locally published build
- [ ] T066 Execute all seven sections of `quickstart.md` end-to-end against a full local run, including the §7 before/after diff (SC-003 regression guard)
- [ ] T067 Correct `docs/investment-performance-roadmap.md` §3 (G12): the "Rent is an FII/REIT naming leak" claim was disproven against live data during this feature's clarification session (spec.md's Clarifications section) — update it to record the actual finding (share-lending income)
- [ ] T068 Self-review the full diff against `docs/rules/implementation.md`'s Definition of Done, and `docs/ui/review-checklist.md` in full for the US4 changes (per `docs/rules/ui.md`'s scope-of-compliance rule, not only the items tied to this feature's original trigger)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup. **Blocks every user story.**
- **User Story 1 (Phase 3)**: Depends on Foundational only.
- **User Story 2 (Phase 4)**: Depends on Foundational **and User Story 1** (T031 reads the `NetCash`/`AssetCashFlowBuilder` shape T020 established; the Credit-rename migration is independent of US1 but is sequenced here because it is US2's own subject matter).
- **User Story 3 (Phase 5)**: Depends on User Story 1 (`NetCash`, T020) **and** User Story 2 (`NetAmount`, T031) — it builds the Gross/Net split directly on top of both.
- **User Story 4 (Phase 6)**: Depends on User Stories 1–3 — there is nothing to render in either front end until the underlying vocabulary and return figures exist (spec.md's own stated ordering).
- **Polish (Phase 7)**: Depends on all four user stories.

Unlike the typical spec-kit default, **these stories are not independently parallelizable by
different developers** — spec.md's own "Why this priority" sections establish a strict dependency
chain (US1 → US2 → US3 → US4). Parallelism exists *within* each phase (tasks marked `[P]`), not
*across* phases.

### Within Each Phase

- Domain/Application implementation before its own tests where a test needs the production code to
  exist to compile against (all tests here are written test-and-implementation-together, not
  strict TDD — Principle V requires tests, not a red-green-refactor order).
- DTO changes before the services/mappers that consume them.
- Backend (Domain → Application → Infrastructure → Api) before Web/WPF (US4) — US4 has nothing to
  consume otherwise.

### Parallel Opportunities

- Foundational: T002 first (everything else in the phase depends on it); T005, T006 are parallel to
  each other and to T003→T004 once T002 lands; T009–T015 (tests) are parallel to each other once
  their respective production-code task lands.
- US1: T016, T017 in parallel; T021–T024 in parallel once their DTO dependencies land.
- US2: T032, T033 in parallel; T036–T038 in parallel once their dependencies land; T028→T029→T030
  (new project) is strictly sequential, but runs in parallel with T032–T034 (DTO track).
- US3: T044–T046 in parallel once T042/T043 land.
- US4: T053, T054 (Web hooks) and T059, T060 (WPF dialogs) are all parallel to each other; T058 and
  T063 (tests) run in parallel once their respective implementation tasks land.

---

## Parallel Example: Foundational Phase

```bash
# After T002 (TransactionTypeEffects) lands, these three can proceed together:
Task: "Extend SaleCoverageRule.FindFirstUncoveredSale for any QuantityEffect.Decrease type (Financial.Investment.Domain/Rules/SaleCoverageRule.cs)"
Task: "Extend TransactionReplayOrder's tie-break to QuantityEffect.Decrease (Financial.Investment.Domain/Rules/TransactionReplayOrder.cs)"
Task: "Widen Transaction entity: enum, validation, Withheld, NetCash (Financial.Investment.Domain/Entities/Transaction.cs)"
```

## Parallel Example: User Story 4

```bash
# Web and WPF tracks are independent of each other (both depend only on US1-3's backend surface):
Task: "Update useTransactions.ts: widened Type union, Withheld, NetCash, type-effects fetch (Financial.Web/src/hooks/useTransactions.ts)"
Task: "Update TransactionDialogViewModel.cs: new type options, conditional Quantity/UnitPrice, Withheld (Financial.App/ViewModels/Investment/TransactionDialogViewModel.cs)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (Setup) and Phase 2 (Foundational — blocking).
2. Complete Phase 3 (User Story 1).
3. **STOP and VALIDATE**: run `quickstart.md` §3 against a running API; confirm every new
   transaction type records correctly and Buy/Sell is unaffected.
4. This is a real, demoable increment (every new transaction type is recordable via the API) even
   though it ships with no UI yet — consistent with Constitution Principle VII's "complete working
   slice," since it is independently testable and reviewable via the API/tests alone.

### Incremental Delivery

1. Setup + Foundational → backend foundation ready.
2. Add User Story 1 → validate via API/tests → mergeable increment (new transaction types
   recordable).
3. Add User Story 2 → validate via API/tests + migration dry run → mergeable increment (income
   vocabulary corrected, gross/withheld/net visible).
4. Add User Story 3 → validate via API/tests → mergeable increment (gross vs net-of-tax return).
5. Add User Story 4 (Web + WPF together) → validate via `quickstart.md` §6 → mergeable increment
   (both front ends expose everything built in US1–US3).
6. Polish → whole-feature regression guard, roadmap doc correction, self-review.

Given the strict cross-story dependency chain, "parallel team strategy" (multiple developers on
different stories at once) does not apply here the way the generic template describes — stories
must land in order.
