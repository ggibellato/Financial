# Implementation Plan: Transaction and Income Event Vocabulary

**Branch**: `004-transaction-income-vocabulary` | **Date**: 2026-09-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-transaction-income-vocabulary/spec.md`

## Summary

Widen `Transaction.TransactionType` from `{Buy, Sell}` to eight types (`+Fee, Redemption,
TransferIn, TransferOut, CapitalCall, ReturnOfCapital`) and `Credit` (today's income event) from a
single positive `Value` to a gross/withheld/net money block with a corrected income-kind vocabulary
(`Rent` → `SecuritiesLendingIncome`, `+Coupon`), each new type declaring its quantity/cash effect as
data rather than code branches. Rebuild the cash-flow builder over the widened vocabulary to produce
distinct gross and net-of-tax return series, in both front ends. Every existing transaction/income
row must display identically after migration (additive fields default to zero/none; the one
non-additive change — the `Rent` → `SecuritiesLendingIncome` rename — requires a raw-JSON string
rewrite migration, because the string is what `JsonStringEnumConverter` persists).

The technical approach generalizes code that already exists rather than introducing new machinery:
`Transactions.Apply`'s Buy/Sell switch becomes a 2-axis (`QuantityEffect`/`CashEffect`) rule table;
`Transaction.TotalPrice` becomes a type-aware `NetCash`; `AssetCashFlowBuilder` gains a Net-of-tax
build mode alongside its existing Gross one; `SaleCoverageRule` extends to any quantity-decreasing
type. See `research.md` for the design decisions behind each of these.

## Technical Context

**Language/Version**: C# / .NET 10 (`Financial.Investment.*`, `Financial.Api`, `Financial.App`,
`Tools/InvestmentTransactionIncomeVocabularyMigration`); TypeScript 5 / React 18 (`Financial.Web`)

**Primary Dependencies**: ASP.NET Core (API controllers); `System.Text.Json` with a custom
`DefaultJsonTypeInfoResolver` (`InvestmentTypeInfoResolver`) for private-setter persistence; xUnit +
FluentAssertions (no mocking framework — hand-written fakes in `Financial.TestUtilities`); Vite +
Vitest + React Testing Library + Playwright (`Financial.Web`)

**Storage**: Single JSON document `data/data-investment.json`, `LocalJson`/`GoogleDrive` provider
(unchanged), loaded once at process startup — a migration requires a full process restart afterward,
never just a file edit

**Testing**: `dotnet test --settings coverlet.runsettings` (unit + `WebApplicationFactory`
integration + `OpenApiContractTests` snapshot); `npm run lint && npm test && npm run build`
(`Financial.Web`); `npm run smoke-test` (Playwright, CI-gated)

**Target Platform**: Docker/Linux container (API + built SPA, `docker-compose up`); Windows desktop
(`Financial.App`, in-process against the same Application/Domain layers, not an HTTP client)

**Project Type**: Web application (ASP.NET Core API + React SPA) plus a WPF desktop client sharing
backend layers in-process — matches the existing repository structure exactly (no new project
needed; this feature widens existing Domain/Application/Infrastructure/Api/App/Web code)

**Performance Goals**: N/A — single-user, self-hosted tool (Constitution Principle IV); the
migration must complete in well under a second against the current data volume, consistent with
every migration already in this codebase

**Constraints**: JSON loaded once at startup (restart required after migration, never sufficient on
its own — Constitution: Technology & Persistence Constraints); full-document rewrite on every save;
OpenAPI snapshot + generated TS types MUST be regenerated in the same PR as any DTO shape change
(`Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json`,
`Financial.Web/src/api/generated/openapi.ts`); both front ends MUST ship each user story's
increment together, not sequentially (Wave 0 precedent, restated in spec.md User Story 4 and FR-021)

**Scale/Scope**: ~899 transactions / ~1,485 credits at spec-writing time (illustrative, will have
grown — never treated as an exact figure to build a rule on, per the roadmap's own stated
volatility warning); 8 transaction types total (2 existing + 6 new); 4 income kinds total (Dividend,
SecuritiesLendingIncome, JCP, Coupon); 52 existing `Rent`-typed rows confirmed against live data to
require the rename migration (re-count at implementation time)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Assessment |
|---|---|
| I. Clean Architecture, Strictly Layered | **Pass.** `TransactionTypeEffects`, widened `Transaction`/`Credit`, `SaleCoverageRule` extension live in Domain; `AssetCashFlowBuilder` Gross/Net split, DTO changes, migration orchestration live in Application/Infrastructure/Tools; `Financial.Api` controllers, `Financial.Web`, `Financial.App` are presentation-only consumers. No layer reaches backward. |
| II. Bounded Context Isolation | **Pass.** Entirely within Investment. The migration tool *pattern* from `Tools/CashFlowSpreadsheetImport` is reused as a shape (backup-then-rewrite, summary counters), not as shared code — Investment's tool defines its own summary type rather than referencing `Financial.CashFlow.Infrastructure` (research.md #6). |
| III. WPF/Web Feature Parity | **Pass, with the same deliberate deviation Wave 0 recorded**: each user story's front-end slice (React + WPF) must land in the same increment, not React-then-WPF sequentially — shipping one without the other would show two different figures/vocabularies for the same holding, which is a parity regression, not an unfinished increment. `Financial.App` continues resolving Application interfaces in-process. |
| IV. Right-Sized Engineering | **Pass.** Eight transaction types (not the full, absent "seventeen" from the external research brief) per spec.md's own Assumptions; `TransactionTypeEffects` is a single `switch` expression, not a plugin/rule-engine abstraction. |
| V. Test-Backed Changes | **Pass, to be detailed in `/speckit-tasks`.** xUnit + FluentAssertions, no new mocking framework; extends existing `SaleCoverageRuleTests`/`TransactionsTests`/`AssetCashFlowBuilderTests` patterns; API round-trip tests via `WebApplicationFactory`; Vitest/RTL for the widened entry forms; WPF ViewModel tests through existing conventions. |
| VI. Evidence-Based, Spec-Driven Change | **Pass, exemplified during this feature's own clarification session**: the roadmap's G12 claim ("Rent is an FII/REIT naming leak") was checked against `data/data-investment.json` and found false — every `Rent` credit sits on non-RealEstate holdings (BBAS3/BOVA11/GOLD11/IVVB11), while every RealEstate holding already uses `Dividend`. The spec was corrected to `SecuritiesLendingIncome` before this plan was written, rather than building on the roadmap's unverified claim. |
| VII. Incremental Vertical Delivery | **Pass.** `/speckit-tasks` slices by the spec's own priority order (US1 → US2 → US3 → US4), each a complete, independently testable/deployable increment per the spec's own "Independent Test" for each story. |
| VIII. Production Deployability After Every Merge | **Conditional pass — one explicit deployment step required.** The `Rent` → `SecuritiesLendingIncome` enum rename is **not safely deployable on its own** without the raw-JSON migration having already run against `data/data-investment.json`, because `JsonStringEnumConverter` throws on an unrecognized stored string (research.md #5). The PR that ships the rename MUST document "run the migration tool against the production data file, then restart" as a required deployment step — the same pattern `CLAUDE.md` already establishes for every schema-widening change in this codebase ("container restart != migration run"), not a new kind of risk. |

No unjustified violations — Complexity Tracking is not needed.

## Project Structure

### Documentation (this feature)

```text
specs/004-transaction-income-vocabulary/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md         # Phase 1 output
├── quickstart.md         # Phase 1 output
├── contracts/
│   └── api-contract.md   # Phase 1 output
└── tasks.md              # Phase 2 output (/speckit-tasks — not created by this command)
```

### Source Code (repository root)

```text
Financial.Investment.Domain/
├── Entities/
│   ├── Transaction.cs                 # widen TransactionType, Quantity/UnitPrice validation, NetCash
│   └── Credit.cs                      # widen CreditType (Rent→SecuritiesLendingIncome, +Coupon), Withheld, NetAmount
├── Rules/
│   ├── TransactionTypeEffects.cs      # NEW — QuantityEffect/CashEffect declaration table (data-model.md)
│   ├── TransactionReplayOrder.cs      # extend tie-break to the widened decrease/increase effect, not just Sell
│   └── SaleCoverageRule.cs            # extend "sale" coverage check to any QuantityEffect.Decrease type

Financial.Investment.Application/
├── Services/
│   ├── AssetCashFlowBuilder.cs        # add Net-of-tax build mode (research.md #4)
│   ├── HoldingValuationService.cs     # add TotalReturnNetOfTax alongside TotalReturn
│   └── SummaryService.cs              # add AggregatedSummaryDTO.TotalReturnNetOfTax
├── DTOs/
│   ├── Transaction*.cs                # +Withheld, TotalPrice→NetCash (contracts/api-contract.md)
│   └── Credit*.cs                     # +Withheld, +NetAmount

Financial.Investment.Infrastructure/
└── Persistence/
    └── InvestmentTypeInfoResolver.cs  # exclude new computed properties (NetCash, NetAmount)

Tools/InvestmentTransactionIncomeVocabularyMigration/   # NEW project — Program.cs, migrator + summary (research.md #6)

Financial.Api/Controllers/
├── TransactionsController.cs          # +GET /transactions/type-effects
└── CreditsController.cs               # (DTO shape only, no new routes)

Financial.Web/src/
├── components/TransactionsTab.tsx, CreditsTab.tsx     # new type/kind options, gross/withheld/net display
├── hooks/useTransactions.ts, useCredits.ts            # widened DTO shapes, type-effects fetch
└── api/generated/openapi.ts, api/types.ts             # regenerated (contracts/api-contract.md)

Financial.App/ViewModels/Investment/
├── TransactionDialogViewModel.cs, TransactionDialogValidation.cs
└── CreditDialogViewModel.cs, CreditDialogValidation.cs

Tests/
├── Financial.Investment.Domain.Tests/          # TransactionTypeEffects, widened Transactions/SaleCoverageRule/Credit
├── Financial.Investment.Application.Tests/     # AssetCashFlowBuilder Net mode, Summary/HoldingValuation NetOfTax
├── Financial.Api.Tests/                        # round-trip + OpenAPI contract snapshot
├── Financial.InvestmentTransactionIncomeVocabularyMigration.Tests/ # migration tool
├── Financial.Presentation.Tests/               # WPF ViewModel tests
└── Financial.Web (Vitest, co-located)
```

**Structure Decision**: This feature widens existing files in the established Investment
bounded-context layout (Domain → Application → Infrastructure → Api/App/Web), plus **one new
project**: `Tools/InvestmentTransactionIncomeVocabularyMigration`, a small standalone console tool
matching the shape of the existing `Tools/InvestmentDataQualityReport` (Wave 0's precedent for a
one-purpose Investment console tool with a top-level `Program.cs`) — `Tools/InvestmentSpreadsheetImport`
was considered but rejected as the host for this migration once inspection showed it is a class
library with no entry point of its own, consumed only by the WPF GUI `Tools/ImportGoogleSpreadSheets`
(research.md #6).

## Complexity Tracking

*(Not needed — no unjustified Constitution Check violations.)*
