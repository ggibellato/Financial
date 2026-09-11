# Implementation Plan: Investment Calculation Core

**Branch**: `003-investment-calculation-core` | **Date**: 2026-09-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-investment-calculation-core/spec.md`

## Summary

Make the Investment context's existing figures correct and server-owned before the wider programme
widens any schema. Seven prioritised stories: date-ordered position replay with a stated same-date
rule; refusing sales of units not held without breaking the three holdings that already do; one
definition of "amount invested" per scope; one owner for valuation consumed by both front ends;
portfolio- and broker-level return; market-based allocation; and a data-quality report that writes
nothing.

The technical shape is set by one constraint the roadmap got wrong and the constitution confirms:
**`Financial.App` is not an HTTP client**, so the "single owner" cannot be an endpoint. It is a
Domain rule behind an Application service — the shape `ProfitCalculator` → `ProfitCalculationService`
already has — with a REST endpoint as an *additional* surface for the browser. No stored data shape
changes; the only persistence change is removing a derived field that is currently written.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`); TypeScript 6.0 + React 19.2 (Vite 8)

**Primary Dependencies**: ASP.NET Core (API + SPA host), WPF (desktop), System.Text.Json with a
custom `IJsonTypeInfoResolver`, Fluent UI React v9 (`@fluentui/react-components` 9.74), WPF-UI

**Storage**: One JSON document per bounded context (`data/data-investment.json`), loaded once at
process start and held in memory for the process lifetime; writes are full-document rewrites. No
database, no file locking, no cross-process write coordination between `Financial.Api` and
`Financial.App`. Provider is `LocalJson` (default) or `GoogleDrive`.

**Testing**: xUnit 2.9 + FluentAssertions 6.12, **no mocking framework** — hand-written fakes in
`Tests/Financial.TestUtilities` (`StubInvestmentRepository`, `RecordingLogger<T>`,
`RecordingTelemetryTracer`, `FakeTimeProvider`). Vitest 4 + React Testing Library 16 for the web.
Playwright 1.61 drives `npm run smoke-test`.

**Target Platform**: Windows desktop (WPF) and a single Linux container serving API + SPA on 8080

**Project Type**: DDD-layered solution, two bounded contexts, **two independent presentation clients**
against the same Application layer — one over HTTP, one in-process

**Performance Goals**: Interactive, single-user. Every operation here is over 160 holdings / 899
transactions held in memory, so a full position replay per edit costs microseconds; no performance
requirement in the spec turns on it.

**Constraints**: Application DTOs are the literal wire format — any DTO change is a wire-format
change requiring an OpenAPI snapshot regeneration and `npm run generate-api-types` in the same PR.
Max **8 non-test code files per PR**. Coverage is banded per CI job (backend / wpf / web): green at
100%, yellow 95–99.99%, amber 90–94.99%, **red below 90% — and only red fails the job**. The
merge-blocking floor is therefore 90%, not 95%; 95–99.99% is an accepted band that asks the PR to note
what is uncovered. Every increment must leave `main` deployable on its own.

**Scale/Scope**: 160 holdings (28 active, 132 historic), 899 transactions, 1,485 income records,
~62 price snapshots, 25 portfolios, 4 brokers. Single user, no auth.

## Constitution Check

*GATE: evaluated before Phase 0; re-checked after Phase 1 design.*

| # | Principle | Status | Notes |
|---|---|---|---|
| I | Clean Architecture, strictly layered | **PASS** | Valuation lands as a Domain rule + Application service. No Domain→Application/Infrastructure edge. Mechanically checked by `InvestmentDependencyRuleTests` and `PresentationDependencyRuleTests`. |
| II | Bounded context isolation | **PASS** | Investment only. Nothing here touches CashFlow, and no shared vocabulary is merged. |
| III | WPF/Web parity | **PASS, with a recorded conflict** | The constitution names WPF as UX source of truth; `docs/rules/ui.md`, `CLAUDE.md` and `docs/ui/review-checklist.md` ("React-led WPF parity") name React. Every requirement in the spec is written as *both front ends agreeing*, so the plan is correct under either reading. The conflict is real and needs resolving via `/speckit-constitution`, but nothing here depends on the answer. Principle III's second clause — `Financial.App` is not an HTTP client — is load-bearing and is what shapes the design. |
| IV | Right-sized engineering | **PASS** | No auth, no multi-tenancy, no feature flags, no back-compat shims. Removing the persisted `PositionType` is a direct change, not a shimmed migration. |
| V | Test-backed changes | **PASS, with one item to watch** | xUnit + FluentAssertions, no mocking framework, existing fakes reused. **Watch**: FR-002 changes two holdings' figures, so any test asserting those values must be *corrected*, not loosened. Phase 0 identifies whether such tests exist. |
| VI | Evidence-based, spec-driven | **PASS, with a deviation** | The spec classifies every assumption as confirmed / observed / inferred and was fact-checked against code and data (20 corrections). **Deviation**: it lives at `specs/003-…` rather than `docs/prd/P<NN>-…`; user-confirmed, recorded in Complexity Tracking. |
| VII | Incremental vertical delivery | **PASS, with a deviation** | Every increment is a working slice. **Deviation**: the two front ends switch in the *same* increment rather than separate ones. Justified in Complexity Tracking. |
| VIII | Deployability after every merge | **PASS** | The load path stays unvalidated (FR-013/FR-014) precisely so the three already-breaching holdings cannot stop startup. The Playwright smoke test asserts a **CashFlow** figure (a seeded expense average), so Investment valuation changes cannot break it. |

**Technology & persistence constraints**: no schema widening in this feature; the only storage change
is *removing* a derived field. DTO changes are wire-format changes and are sequenced accordingly.

**Gate result (pre-Phase 0): PASS.** Two deviations recorded below with justification.

### Re-check after Phase 1 design

Design changed three of the assessments above, all in the direction of more evidence rather than less:

| # | Principle | Post-design | What changed |
|---|---|---|---|
| I | Clean Architecture | **PASS, reinforced** | Research settled the layer for each new type against `implementation.md` §Domain rules rather than by intuition: `HoldingValuationCalculator`, `SaleCoverageRule`, `TransactionReplayOrder` and `OpenPositionCostCalculator` all meet both extraction criteria and are named to avoid the `*Service`/`*Policy`/`*Specification` ban. Cash-flow aggregation was *rejected* for Domain — it fails both criteria and would invert a layer, since the flows are an Application DTO. |
| III | WPF/Web parity | **PASS, with a defect to fix in-flight** | `TransactionsTabViewModel` has no `try`/`catch` in Add/Update/Delete and `App.xaml.cs` registers no `DispatcherUnhandledException` handler — so the refusal this feature introduces would **crash the desktop app**. Pre-existing and latent; this feature makes it reachable, so FR-062 requires fixing it here. Web already surfaces the message correctly. |
| V | Test-backed changes | **PASS, and the watch item cleared** | No existing test asserts the two changed holdings' figures — every `Bitcoin`/`AGNC` hit in the test tree is a synthetic fixture, and no test project reads the live data file. So nothing must be loosened. `TransactionsTests` needs **zero** changes, which is the Principle V signal worth having: an all-green existing suite means the change is additive rather than a redefinition. The corollary is that **FR-006/SC-002 is currently unverified by anything** and needs a new test. |

**Correction to the Technical Context above**: the coverage floor was stated as "95% enforced"
earlier in this work. It is **90%** — 95–99.99% is an accepted yellow band. The wrong figure would
have ruled out designs the repository actually accepts. Corrected in Technical Context and in
`research.md` §R15.

**Gate result (post-design): PASS.** No new deviation; the two below still stand.

## Project Structure

### Documentation (this feature)

```text
specs/003-investment-calculation-core/
├── spec.md              # Feature specification (/speckit-specify, /speckit-clarify)
├── plan.md              # This file (/speckit-plan)
├── research.md          # Phase 0 output (/speckit-plan)
├── data-model.md        # Phase 1 output (/speckit-plan)
├── quickstart.md        # Phase 1 output (/speckit-plan)
├── contracts/           # Phase 1 output (/speckit-plan)
├── checklists/
│   └── requirements.md  # Spec quality checklist + decision log
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source code (repository root)

Only the projects this feature touches are listed; the solution is larger.

```text
Financial.Investment.Domain/
├── Entities/
│   ├── Transactions.cs          # date-ordered replay, same-date rule (FR-001..FR-008)
│   ├── Transaction.cs
│   └── Asset.cs                 # as-of price lookup (FR-028), price precedence (FR-033)
└── Rules/
    ├── XirrCalculator.cs        # unchanged (FR-044)
    ├── ProfitCalculator.cs      # the precedent shape for the new rule
    └── HoldingValuation.cs      # NEW — the single owner (FR-026, FR-036)

Financial.Investment.Application/
├── DTOs/                        # wire format — changes require snapshot + type regeneration
├── Interfaces/                  # in-process surface consumed directly by Financial.App
└── Services/
    ├── AssetInvestedAmountSelector.cs      # invested definition (FR-019, FR-020)
    ├── PortfolioAssetSummaryBuilder.cs     # weight basis / yield denominator split (FR-025)
    ├── PortfolioAssetSummaryService.cs
    ├── BrokerBreakdownService.cs           # allocation chart inclusion (FR-070)
    ├── SummaryService.cs                   # totals reconcile to rows (FR-022)
    └── AssetCashFlowBuilder.cs             # per-asset flows; aggregation added (FR-042, FR-067)

Financial.Investment.Infrastructure/
└── Persistence/InvestmentTypeInfoResolver.cs   # stop persisting a derived field (FR-005)

Financial.Api/Controllers/       # additional REST surface for the browser only

Financial.App/ViewModels/Investment/          # consumes the Application services in-process
├── AssetDetailsViewModel.cs
└── PortfolioAssetSummaryRowViewModel.cs

Financial.Web/src/
├── api/                         # generated types + client
├── hooks/                       # useAssetSummary, usePortfolioAssetSummary
└── components/                  # AssetSummaryTab, PortfolioSummaryTab

Tools/                           # data-quality report (FR-053..FR-059) — reads, never writes

Tests/
├── Financial.Investment.Domain.Tests/
├── Financial.Investment.Application.Tests/
├── Financial.Api.Tests/         # incl. Contract/openapi-v1.snapshot.json
├── Financial.Presentation.Tests/
└── Financial.Web/src/**/__tests__/
```

**Structure Decision**: no new projects except a `Tools/` console for the data-quality report, which
follows `Tools/InvestmentSpreadsheetImport` and ships with its own test project in the same PR (the
coverage gate aggregates per job). Everything else extends projects that already exist, in their
established layers.

## Complexity Tracking

| Deviation | Why needed | Simpler alternative rejected because |
|---|---|---|
| **Both front ends switch to server-computed figures in the same PR**, deviating from Principle VI/VII's `… → API → WPF → Web → tests` one-slice-per-PR order | Switching one front end while the other still derives its own values makes the same holding show two different numbers on the same kind of screen. Under Principle III that is a parity regression and under Principle VIII a regression of existing functionality — so the intermediate state is not deployable, which is exactly what the slice order exists to guarantee. | Splitting by front end produces a PR that is individually reviewable but leaves `main` in a state the constitution forbids. Splitting by *screen* instead (asset view in one PR, portfolio grid in the next) preserves both rules and is what the plan does — each PR covers one screen across both clients. |
| **The feature spec lives at `specs/003-investment-calculation-core/` rather than `docs/prd/P46-…`**, which Principle VI names | User-confirmed at the start of this work: the Spec Kit chain (`/speckit-plan`, `/speckit-tasks`, `/speckit-implement`) reads `.specify/feature.json`, and pointing it at a `docs/prd` folder would break that chain. | Writing the spec twice — once as a PRD, once as a Spec Kit spec — creates two sources of truth for one feature, which is the drift Principle VI exists to prevent. |

## Phase outputs

| Phase | Artifact | Contents |
|---|---|---|
| 0 | [research.md](./research.md) | R1–R16. Every design decision with rationale and rejected alternatives, verified against code and data. |
| 1 | [data-model.md](./data-model.md) | Entity changes, new Domain and Application types, wire DTOs, state tables. |
| 1 | [contracts/README.md](./contracts/README.md) | The two surfaces, the error contract, and the contract change no tooling catches. |
| 1 | [quickstart.md](./quickstart.md) | Seven runnable validation scenarios with the expected figures. |

## Increment sequence

Derived from the research; `/speckit-tasks` turns these into tasks. Each row is one PR, each leaves
`main` deployable, and none exceeds 8 non-test code files.

| # | Story | Scope | Files |
|---|---|---|---|
| 1 | US1 | Date-ordered replay + same-date rule + the Buy-branch zero guard | 1 |
| 2 | US1 | Stop persisting the two derived fields | 1 |
| 3 | US2 | `SaleCoverageRule`, `TransactionReplayOrder`, strict `Asset` methods, `TransactionService` repointed | 4–5 |
| 4 | US2 | Both front ends surface the refusal — **includes the WPF crash fix** | 2–3 |
| 5 | US3 | Separate the three figures; correct invested; `SummaryService` reconciles | 8 |
| 6 | US3 | Front-end footers read the server total | 3 |
| 7 | US6 | Compatibility boundary: weight nullable + one format, value still never null | 7 |
| 8 | US4 | `HoldingValuationCalculator` + service + holding-level DTO fields | 5–6 |
| 9 | US4 | Both front ends switch to server-computed valuation, incl. refresh-after-fetch | 4–6 |
| 10 | US5 | Level aggregation + `AggregatedSummaryDTO` fields | 4 |
| 11 | US5 | Both front ends render level totals and returns | 3–4 |
| 12 | US6 | Weight basis → market value + shortfall disclosure | 6–8 |
| 13 | US7 | Data-quality report (logic in Application, thin `Tools/` printer) | 4–5 |

**Ordering constraints that are not negotiable:**

- **5 before 12.** The three figures must be separated before the weight basis moves, or yield-on-cost
  silently becomes yield-on-market for all 28 active holdings.
- **7 before 12.** Weight must be nullable in both front ends *before* it can be null, or unvaluable
  holdings render `0.0%` — which FR-047 calls "the opposite of true" for holdings that include the
  largest position in two portfolios.
- **5 before 10.** `SummaryService`'s `Quantity != 0` filter changes `HoldingCount`; publishing the
  count first would make the two disagree in the intermediate state.
- **8 before 10.** Level totals sum per-holding valuations.
- **3 and 4 together in sequence.** The rule is reachable from the desktop the moment it exists, and
  WPF crashes on it until 4 lands.

**PR count: 13**, against the roadmap's original 7 — the undercount this planning was expected to
find, and worth carrying back into the estimate for P47–P53.
