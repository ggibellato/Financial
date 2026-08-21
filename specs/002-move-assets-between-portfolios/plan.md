# Implementation Plan: Move Assets Between Portfolios

**Branch**: `002-move-assets-between-portfolios` | **Date**: 2026-08-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-move-assets-between-portfolios/spec.md`

## Summary

Let the user relocate an asset from one portfolio to another — into an existing portfolio or one
named during the move — from both front ends, by dialog and by dragging it in the navigation tree.
A fully closed asset (quantity exactly zero) can additionally cross from Active Investments into
Historic Investments. A portfolio left empty by a move can be deleted, by an offer made as the move
finishes and by a standalone action at any time.

The technical shape is deliberately small: **the asset object is relocated between two `List<Asset>`
positions in the in-memory graph, inside a single `ApplyAndSaveAsync` delegate.** Nothing is copied,
re-keyed, or recomputed — `Asset` already derives quantity, average price and realised gain from the
transactions and credits it carries, so moving the object moves every figure with it. That single
fact is what makes FR-002, FR-003 and FR-009 fall out of the design instead of needing machinery.

Two Domain operations own every rule: `Broker.MoveAsset` for the same-broker move (used unchanged in
both scopes) and `Investments.ArchiveAsset` for the Active → Historic crossing. Rejections travel as
exceptions carrying a plain-language reason, which is what lets the API and the in-process WPF client
show the user identical wording from one source.

**No persisted shape changes, so no migration** — which matters in a system where restarting the app
never runs one.

## Technical Context

**Language/Version**: C# / .NET 10 (API, WPF, all backend tests); TypeScript + React on Vite
(`Financial.Web`)

**Primary Dependencies**: ASP.NET Core, WPF, React + Vite. **No new dependency is introduced** —
drag-and-drop uses WPF's built-in `DragDrop` and the browser's native HTML5 drag events
(`research.md` §D7, §D8)

**Storage**: one JSON document, `data-investment.json`, via `Financial.Shared.Infrastructure`. Loaded
once at process startup, held in memory, rewritten whole on every save. Provider is `LocalJson` or
`GoogleDrive` per context

**Testing**: xUnit + FluentAssertions, no mocking library — extend the hand-written fakes in
`Financial.TestUtilities`; Vitest + React Testing Library for Web; Playwright for the CI smoke test

**Target Platform**: Windows desktop (`Financial.App`) and a single Linux container serving API + SPA
on port 8080 (`Financial.Api`)

**Project Type**: two DDD bounded contexts (Domain → Application → Infrastructure) plus two
independent front ends at feature parity. Only the **Investment** context is touched

**Performance Goals**: a move or deletion completes in under 2 s (SC-007). Measured scale: the data
file is 648 KB — 4 Active broker records / 10 portfolios / ~30 assets and 3 Historic broker records /
14 portfolios / ~129 assets. The move itself is two list operations; the budget is spent almost
entirely on the full-document write, and under the `GoogleDrive` provider on the upload

**Constraints**: no database, no authentication, single user. `ApplyAndSaveAsync` is the only write
path and its exclusion is **not reentrant** — a nested call deadlocks the process, so the
move-then-delete flow must be two client-driven calls (`research.md` §D4). Application DTOs *are* the
wire format; a DTO change is a contract change for `Financial.Web`

**Scale/Scope**: ~159 assets across 24 portfolios and 7 broker records. Single user, one install

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Verdict | Evidence |
|---|---|---|---|
| I | Clean Architecture, strictly layered | **PASS** | Every rule sits in Domain (`data-model.md` §3). Application orchestrates and persists; Infrastructure only returns the root it already holds; Presentation carries pointer plumbing and dialogs, no rules. One deliberate widening — see Complexity Tracking |
| II | Bounded context isolation | **PASS** | Investment only. CashFlow is untouched; nothing shared is added |
| III | WPF/Web parity, WPF as UX source of truth | **PASS** | Both front ends are in scope in every increment that ships user-visible behaviour (FR-040). WPF consumes `IAssetMoveService` in-process, so the Application contract was designed against both consumers |
| IV | Right-sized engineering | **PASS** | No new dependency, no new project, no migration, no feature flag. Drag-and-drop is built on platform primitives rather than a library |
| V | Test-backed changes | **PASS** | Five existing test projects, existing conventions, no mocking library. `StubInvestmentRepository` is extended, not replaced (`research.md` §D10) |
| VI | Evidence-based, spec-driven change | **PASS, with one amendment raised** | Every decision cites verified code or data. The one discovered conflict with the spec is surfaced below rather than silently resolved |
| VII | Incremental vertical delivery | **PASS** | Seven increments, each a working reviewable slice — see Delivery Increments |
| VIII | Production deployability after every merge | **PASS** | No increment leaves a partially wired capability; each is verified with `docker-compose up` per `quickstart.md` §6 |

**Post-Phase-1 re-check**: still PASS. The design added one repository method and one middleware
clause; neither changes the verdicts above. The `internal` visibility of `Portfolio.RemoveAsset`
tightened Principle I rather than loosening it — only `Broker` and `Investments` can relocate an
asset.

## Spec Amendments

One conflict between the approved spec and the verified data, raised rather than silently decided
(Constitution VI):

**The spec assumes "this feature never creates or deletes a broker".** That is too strong. Brokers
are stored as two independent lists, and they are not mirrors — `Coinbase` exists in `ActiveBrokers`
with no `HistoricBrokers` counterpart. Archiving a closed Coinbase holding is impossible unless the
Historic broker record is created as part of the move.

**Proposed amendment**, to apply before implementation begins:

- Amend the *No broker lifecycle* assumption to: this feature never creates a broker the user would
  recognise as new. When a closed asset is archived and its broker has no Historic record yet, that
  record is created automatically, copying the Active broker's name and currency — the same
  real-world broker appearing in the historic view for the first time. Brokers are never deleted.
- Add **FR-043**: When an asset is archived into Historic Investments and its broker has no Historic
  record, the system MUST create one carrying the same name and currency as the Active broker, and
  MUST place the destination portfolio under it. The user MUST NOT be asked to confirm this.

Rationale and evidence: `research.md` §D2.

## Project Structure

### Documentation (this feature)

```text
specs/002-move-assets-between-portfolios/
├── plan.md                             # This file
├── spec.md                             # Approved specification
├── research.md                         # Phase 0 — 10 decisions, all unknowns resolved
├── data-model.md                       # Phase 1 — entities, added members, rule enforcement map
├── quickstart.md                       # Phase 1 — validation guide
├── contracts/
│   ├── rest-api.md                     # HTTP surface
│   └── application-services.md         # In-process surface (WPF consumes this directly)
├── checklists/
│   └── requirements.md                 # Spec quality checklist — passing
└── tasks.md                            # Phase 2 — created by /speckit-tasks, not by this command
```

### Source Code (repository root)

Only the paths below are touched. Project folders sit flat at the repo root.

```text
Financial.Investment.Domain/
└── Entities/
    ├── Portfolio.cs                    # + IsEmpty, FindAsset, internal RemoveAsset
    ├── Broker.cs                       # + FindPortfolio, MoveAsset, RemoveEmptyPortfolio
    └── Investments.cs                  # + FindActiveBroker, FindHistoricBroker, ArchiveAsset

Financial.Investment.Application/
├── DTOs/
│   └── MoveAssetRequestDTO.cs          # new — wire format (the response reuses AssetDetailsDTO)
├── Interfaces/
│   ├── IAssetMoveService.cs            # new
│   └── IInvestmentRepository.cs        # + GetInvestments()
├── Services/
│   └── AssetMoveService.cs             # new — orchestration inside ApplyAndSaveAsync
└── DependencyInjection/
    └── InvestmentApplicationServiceCollectionExtensions.cs   # register the service

Financial.Investment.Infrastructure/
└── Repositories/
    └── InvestmentJsonRepository.cs     # + GetInvestments() — returns the field it already holds

Financial.Api/
├── Controllers/
│   ├── AssetsController.cs             # + POST /assets/move
│   └── PortfoliosController.cs         # new — DELETE /portfolios/{broker}/{portfolio}
└── Middleware/
    └── DomainExceptionMappingMiddleware.cs   # + InvestmentRuleViolationException -> 409

Financial.App/                          # WPF
├── MoveAssetDialog.xaml(.cs)           # new — follows TransactionDialog/CreditDialog pattern
├── Components/NavigationView.xaml      # AllowDrop + drop-target trigger on ItemContainerStyle
├── Behaviors/TreeViewDragDropBehavior.cs     # new — pointer plumbing only
└── ViewModels/Investment/
    ├── MainNavigationViewModelBase.cs  # move/delete commands, reload, reselection
    └── TreeNodeViewModel.cs            # + IsDropTarget, CanAccept predicate

Financial.Web/src/
├── api/
│   ├── financialApiClient.ts           # + moveAsset, deleteEmptyPortfolio
│   └── types.ts                        # + MoveAssetRequestDto
├── components/
│   ├── InvestmentTree.tsx(.css)        # drag source + drop targets + highlighting
│   └── MoveAssetDialog.tsx(.css)       # new — dialog route and archive
└── context/SelectedNodeContext.tsx     # + reload()

Tests/
├── Financial.Investment.Domain.Tests/          # every rule in data-model.md §3
├── Financial.Investment.Application.Tests/     # orchestration; rejected move never writes
├── Financial.Api.Tests/                        # 200/400/404/409 round trips
├── Financial.Presentation.Tests/               # drop-target rules, reselection
└── Financial.TestUtilities/StubInvestmentRepository.cs   # scope-aware + GetInvestments()

Financial.Web/src/**/__tests__/                 # dialog, drag handlers, api client
```

**Structure Decision**: the existing two-bounded-context layout is used unchanged. No new project is
created — a feature this size does not warrant one (Constitution IV), and the Investment context
already has every layer this feature needs. `Financial.CashFlow.*` and `Financial.Shared.*` are not
touched at all.

## Delivery Increments

Seven increments, each a complete working slice with its own tests and its own PR (Constitution VII).
Each must leave `main` deployable (Constitution VIII) and record its verification in the PR body.

| # | Increment | Stories | Ships | Review boundary |
|---|---|---|---|---|
| 1 | Same-scope move: Domain → Application → API | US1, US2 | Domain members, `IAssetMoveService`, DTOs, `POST /assets/move`, `InvalidOperationException → 409` | Every move rule provable in the Domain suite; API round-trips 200/400/404/409 |
| 2 | WPF move dialog | US1, US2 | `MoveAssetDialog`, VM commands, tree reload + reselection | A user can move an asset in the WPF app |
| 3 | Web move dialog | US1, US2 | `MoveAssetDialog.tsx`, api client, `types.ts`, `reload()` | A user can move an asset in the browser; parity with increment 2 including rejection wording |
| 4 | Archive Active → Historic | US3 | `Investments.ArchiveAsset`, `GetInvestments()`, scope pair validation, FR-043 broker creation, both front ends' scope handling | A zero-quantity asset archives; a non-zero one is refused with a reason |
| 5 | Delete an empty portfolio | US5 | `Broker.RemoveEmptyPortfolio`, `PortfoliosController`, post-move offer + standalone action in both front ends | Empty deletes, non-empty refuses, survives restart |
| 6 | Drag and drop — WPF | US4 | Attached behaviour, `IsDropTarget`, drop handling, broker-drop name prompt | Every US4 scenario in the WPF app |
| 7 | Drag and drop — Web | US4 | Drag source and drop targets in `InvestmentTree`, highlighting, broker-drop name prompt | Every US4 scenario in the browser; parity with increment 6 |

**Ordering rationale**: 1–3 deliver the P1/P2 capability end to end before anything is layered on it.
4 and 5 are independent of each other and both depend only on 1. 6 and 7 come last because a drop is
a second route to the move that 1–5 already provide — built earlier they would have nothing to call
(and the broker-drop path specifically needs increment 1's create-on-new-name behaviour).

**Increment 1 is the one to review hardest.** It fixes the rule set, the exception-to-status mapping,
and the wire format that the remaining six consume.

## Complexity Tracking

> Filled only for the one place the design widens an existing boundary.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| `IInvestmentRepository.GetInvestments()` exposes the aggregate root to the Application layer, where every other method returns a scoped projection | Archiving may have to add a broker to `HistoricBrokers` — verified necessary, since `Coinbase` exists only in `ActiveBrokers`. `GetBrokerList(scope)` returns a read-only collection and cannot add to it | *Deriving it from `GetBrokerList`* — sufficient for every read and every within-scope mutation, but structurally incapable of adding a broker. *A repository method `EnsureHistoricBroker(name, currency)`* — moves the decision of *when* a Historic broker should exist into Infrastructure, breaking Principle I for a rule that belongs in Domain. Returning the aggregate root is standard repository design and narrower than it looks: the repository already hands out `Broker` and `Asset`, and `InvestmentJsonRepository` already holds the root in a field |

No other exception is taken. Notably **not** required: a new project, a mocking library, a
drag-and-drop package, a data migration, a feature flag, an audit log, or any change to
`Financial.CashFlow.*` or `Financial.Shared.*`.
