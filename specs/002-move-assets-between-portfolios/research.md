# Phase 0 Research: Move Assets Between Portfolios

**Feature**: `specs/002-move-assets-between-portfolios` | **Date**: 2026-08-21

All Technical Context unknowns are resolved below. Each decision records what the codebase actually
does today (verified, with file references) so the plan is not built on assumption.

---

## D1. Where the move rules live

**Decision**: Two Domain operations, no new Domain enum.

- `Broker.MoveAsset(sourcePortfolioName, assetName, destinationPortfolioName)` — the whole
  same-broker move, used identically for Active and for Historic.
- `Investments.ArchiveAsset(brokerName, sourcePortfolioName, assetName, destinationPortfolioName)` —
  the Active → Historic crossing, on the aggregate root because it spans both broker collections.

Supporting Domain members: `Portfolio.FindAsset(name)`, `Portfolio.RemoveAsset(name)`,
`Portfolio.IsEmpty`, `Broker.FindPortfolio(name)`, `Broker.RemoveEmptyPortfolio(name)`.

**Rationale**: every rule in the spec is a statement about the object graph — the destination must
not already hold that asset name (FR-008), source and destination must differ (FR-006), the new
portfolio name must be non-blank and unique within the broker (FR-012, FR-013), only empty
portfolios delete (FR-022), quantity must be zero to leave Active (FR-017). Constitution I puts
those in Domain, where they are unit-testable with no repository, no API, and no UI. `Broker` already
owns `Portfolios` and already has `AddPortfolio` with get-or-create semantics
(`Financial.Investment.Domain/Entities/Broker.cs`), so it is the natural owner.

**Alternatives considered**:
- *Rules in an Application service.* Rejected — it puts domain invariants where they cannot be
  tested without a repository stub, and Constitution I forbids it.
- *A single `Investments.MoveAsset(...)` covering both cases, taking a scope enum.* Rejected —
  `InvestmentScope` lives in `Financial.Investment.Application.Enums`, and Domain must not reference
  Application. Duplicating the enum into Domain, or relocating it and rewriting ~30 usages across
  API, WPF and Web, is churn this feature does not need: Application already resolves the right
  broker from the right scope before calling Domain, so within-scope moves never need to name a
  scope at all.
- *A `bool requireClosedPosition` flag on one shared method.* Rejected — a flag parameter that
  switches a business rule is exactly the smell Clean Code calls out, and it would leak scope
  knowledge into a scope-free method.

---

## D2. Archiving may have to create the broker's Historic counterpart

**Decision**: `Investments.ArchiveAsset` creates a Historic `Broker` with the same name and currency
as the Active one when no Historic counterpart exists, then places the asset in it.

**Evidence** — the live data file (`data/data-investment.json`, inspected read-only) stores brokers
as two independent lists, with the same broker name repeated across them:

| Scope | Brokers |
|-------|---------|
| `ActiveBrokers` | Trading 212 (GBP), XPI (BRL), Coinbase (GBP), FreeTrade (GBP) |
| `HistoricBrokers` | Trading 212 (GBP), XPI (BRL), FreeTrade (GBP) |

**Coinbase has no Historic counterpart.** Archiving a closed Coinbase holding is therefore impossible
unless the Historic broker record is created as part of the move.

**Rationale**: this is not "creating a new broker" in the user's terms — it is the same real-world
broker appearing in the historic view for the first time. The spec's assumption *"this feature never
creates or deletes a broker"* was written before the two-list structure was verified and is too
strong. **The spec needs a small amendment**; it is listed as an open item in `plan.md` §Spec
Amendments and must be applied before implementation starts (Constitution VI: surface the
disagreement, do not silently pick a side).

**Alternatives considered**:
- *Refuse to archive when the broker has no Historic counterpart.* Rejected — it makes the feature's
  headline use case fail for one of four brokers today, for a reason the user cannot act on.
- *Ask the user to confirm creating the Historic broker.* Rejected as over-engineering for a
  single-user tool (Constitution IV); the broker name and currency are copied, so there is nothing
  to decide.

---

## D3. Reaching the aggregate root from Application

**Decision**: add one method to `IInvestmentRepository`:

```csharp
Investments GetInvestments();
```

**Rationale**: `Investments` holds the two broker collections and is the only object that can add a
Historic broker (`AddHistoricBroker`, already public). D2 makes that necessary. The repository
already hands `Broker` and `Asset` domain entities to Application
(`Financial.Investment.Application/Interfaces/IInvestmentRepository.cs`), and
`InvestmentJsonRepository` already holds the root in a field, so returning the aggregate root is a
narrowing of an existing pattern rather than a new kind of exposure — it is also textbook repository
design (a repository serves aggregate roots).

**Alternatives considered**:
- *`EnsureHistoricBroker(name, currency)` on the repository.* Rejected — the decision of *when* a
  Historic broker should exist is a domain rule; putting it behind a repository method moves it into
  Infrastructure, violating Constitution I.
- *Derive everything from `GetBrokerList(scope)`.* Works for reads and for every within-scope
  mutation (the returned `Broker` objects are the live graph), but cannot add a broker to a list it
  only exposes read-only. Insufficient for D2 alone.

Recorded in `plan.md` §Complexity Tracking as the feature's one deliberate widening.

---

## D4. Mutations must run inside `ApplyAndSaveAsync`

**Decision**: every move and every deletion executes as the delegate passed to
`IInvestmentRepository.ApplyAndSaveAsync(Func<bool>)`, never before it.

**Rationale**: this is a load-bearing, already-established rule with a documented failure mode. The
interface's own remarks state that saving re-serializes the whole graph, so a mutation applied
outside the exclusion can be walked half-applied; `AssetMutationHelper` follows exactly this shape
and says so in a comment (`Financial.Investment.Application/Services/AssetMutationHelper.cs`). It is
also what makes FR-009 (all-or-nothing) achievable with no new machinery: the delegate throws before
returning `true`, so nothing is serialized and nothing is written.

**Consequence for validation**: rules must be evaluated *inside* the delegate. Validating first and
mutating second would let a concurrent write invalidate the check.

**Constraint to respect**: the exclusion is a non-reentrant `SemaphoreSlim`
(`InvestmentJsonRepository._writeGate`) — a nested `ApplyAndSaveAsync` deadlocks the process rather
than throwing. The move-then-offer-deletion flow (FR-024) is therefore **two separate calls driven
by the front end**, never one service method that calls the other.

---

## D5. How a rejection reaches the user with its reason

**Decision**: Domain throws; Application lets it propagate; the API maps it to a status code; both
front ends show the exception message.

| Condition | Exception | HTTP |
|---|---|---|
| Asset / portfolio / broker not found (FR-011) | `KeyNotFoundException` | 404 |
| Blank new portfolio name (FR-012) | `ArgumentException` | 400 |
| Rule violation: same portfolio (FR-006), cross-broker (FR-007), duplicate asset in destination (FR-008), non-zero quantity to Historic (FR-017), Historic → Active (FR-019), duplicate portfolio name (FR-013), non-empty portfolio deletion (FR-022) | `InvalidOperationException` | 409 |

**Required change**: `DomainExceptionMappingMiddleware` maps `KeyNotFoundException` → 404 and
`ArgumentException` → 400 today, but has nothing for a rule violation, which would surface as a 500.
One `catch` clause must be added (`Financial.Api/Middleware/DomainExceptionMappingMiddleware.cs`).

> **Revised during implementation.** This section originally proposed mapping
> `InvalidOperationException` itself. That is wrong: `YahooFinanceService`, `AssetPriceService` and
> `CryptocurrencyAssetPriceFetcher` already throw it for genuine upstream faults, so a global mapping
> would relabel real defects as client conflicts and hide them behind a 409. A dedicated
> `InvestmentRuleViolationException` in `Financial.Investment.Domain/Exceptions/` is mapped instead,
> following the existing `OverdraftConfirmationRequiredException` precedent. An existing test pinning
> `InvalidOperationException` as *unmapped* proves the distinction still holds.

**Rationale**: FR-041 requires the user to be told which rule blocked the move, in the same words in
both front ends. The existing `OkOrBadRequest(null)` convention used by `TransactionsController`
cannot carry a reason — a null return collapses every distinct failure into an unexplained 400.
Because `Financial.App` calls the Application services in-process (Constitution III), a thrown
exception carrying the message is the *only* mechanism that gives both front ends identical wording
for free. The middleware already writes the message into `ProblemDetails.Detail` while keeping it out
of the logs, which the Web client can read directly.

**Alternatives considered**:
- *Return a result object with a reason code.* Rejected — it would need parallel handling in both
  front ends plus a new mapping layer, for wording the exception already carries.
- *Reuse `ArgumentException` for rule violations.* Rejected — 400 misdescribes "your request was
  well-formed but the data says no", and it would collapse the distinction the Web client needs to
  decide whether to re-fetch.

---

## D6. API contract shape

**Decision**: one move endpoint carrying both scopes, and one deletion endpoint.

```
POST   /api/v1/financial/assets/move
DELETE /api/v1/financial/portfolios/{brokerName}/{portfolioName}?scope=active
```

The move request names `sourceScope` and `destinationScope` explicitly. Within-scope moves set them
equal; archiving sets `active` → `historic`; `historic` → `active` is rejected by the server
(FR-019) rather than merely hidden by the UI.

**Rationale**: one endpoint keeps a single place where FR-019 is enforced and avoids two nearly
identical contracts. Scope as a query string on the delete matches the existing convention —
`AssetsController` and `NavigationController` both take `[FromQuery] string? scope` parsed by
`InvestmentScopeParser.ParseOrDefault`.

**Response**: the move returns the moved asset's `AssetDetailsDTO`, matching the neighbouring
mutation endpoints.

> **Revised during implementation.** This section originally specified an `AssetMoveResultDTO`
> wrapper carrying `sourcePortfolioIsEmpty` for the FR-024 offer, and a `sourceScope`/
> `destinationScope` pair on the request. Both were built for increments that do not exist yet:
> nothing read the flag, and the only legal `destinationScope` was whatever `sourceScope` said.
> Application DTOs are the wire format, so publishing them early would freeze a contract for
> undesigned behaviour (Constitution IV). The request now names a single `scope` — which makes the
> Active→Historic crossing *unrequestable* rather than requestable-and-refused, a stronger reading of
> FR-019 — and the wrapper arrives with the deletion increment that reads it.

**Note on the wire format**: Application DTOs *are* the wire format — there is no separate API
contract layer (Constitution, Technology & Persistence Constraints). New DTOs are therefore a
public contract change for `Financial.Web` and must be mirrored in
`Financial.Web/src/api/types.ts`.

---

## D7. Drag and drop in WPF

**Decision**: an attached behaviour supplies the mouse plumbing; the decision logic lives on
`TreeNodeViewModel` and the navigation view model, where it is unit-testable.

- `Financial.App/Components/NavigationView.xaml` hosts a single `TreeView` bound to `RootNodes`, with
  a `HierarchicalDataTemplate` and an `ItemContainerStyle` — verified.
- The behaviour records the press point on `PreviewMouseLeftButtonDown`, starts
  `DragDrop.DoDragDrop` once the drag threshold is passed on `MouseMove`, sets `e.Effects` in
  `DragOver`, and invokes a command on `Drop`. `AllowDrop="True"` is added to the existing
  `ItemContainerStyle`.
- Drop-target highlighting (FR-033) is a `bool IsDropTarget` on `TreeNodeViewModel` driven by a
  `Style.Trigger`, alongside the existing `IsExpanded` / `IsSelected` two-way bindings.

**Rationale**: WPF has no MVVM-native drag-and-drop, so the pointer handling has to be code-behind or
an attached property. Keeping only the pointer plumbing there and putting "can this node accept this
asset?" on the view model satisfies Constitution I's "no business logic in Presentation" and lets
`Financial.Presentation.Tests` cover the rules without a UI thread.

**Caution**: the `TreeView` sets `VirtualizingPanel.IsVirtualizing="True"` with recycling. Container
recycling means visual drop-target state must live on the view model, not on the container — a
recycled `TreeViewItem` would otherwise carry another node's highlight.

**Alternatives considered**: a third-party drag-drop package (e.g. `gong-wpf-dragdrop`). Rejected —
Constitution IV; roughly 60 lines of attached behaviour covers a single tree.

---

## D8. Drag and drop in Web

**Decision**: native HTML5 drag-and-drop.

- Set `draggable` and `onDragStart` on the asset row in `InvestmentTree.tsx`; `dataTransfer` carries
  the broker, portfolio, and asset names as JSON with `effectAllowed = 'move'`.
- `PortfolioNode` and `BrokerNode` handle `onDragOver` (call `preventDefault()` only when the target
  is valid, so an invalid target genuinely refuses the drop per FR-034), `onDragEnter`/`onDragLeave`
  for highlighting, and `onDrop`.
- Tree rows are `<button>` elements today; `draggable` is set on the asset's `<li>` wrapper so the
  button keeps its click and keyboard behaviour intact.

**Rationale**: no dependency, and it is directly testable — Vitest + React Testing Library drive
`fireEvent.dragStart` / `dragOver` / `drop` with a stub `dataTransfer`, so FR-034's "invalid targets
refuse the drop" is assertable by checking `preventDefault` was not called.

**Alternatives considered**: `react-dnd` or `dnd-kit`. Rejected — a production dependency and a
provider wrapper for one tree, against Constitution IV.

---

## D9. Refreshing the views after a move

**Decision**: re-fetch the affected tree and reselect the moved asset (FR-030, FR-037).

- **Web**: `InvestmentTree` already re-fetches when its `retryCount` state changes; the same
  mechanism is promoted to a `reload()` exposed through `SelectedNodeContext`
  (`Financial.Web/src/context/SelectedNodeContext.tsx`), which the move already depends on for the
  current scope. After a successful move the tree reloads and `setSelectedNode` points at the asset
  under its new portfolio.
- **WPF**: `MainNavigationViewModelBase.LoadNavigationTreeAsync()` already rebuilds `RootNodes`;
  it is called after a successful move, then the moved asset's node is selected.

**Rationale**: the tree is built from a snapshot DTO graph (`NavigationMapper`), not from live domain
objects, so it does not observe the mutation. A full re-fetch is correct and, at this data size
(see Scale below), imperceptible.

---

## D10. Test approach

**Decision**: follow the existing conventions exactly — no new frameworks.

| Layer | Project | Covers |
|---|---|---|
| Domain | `Tests/Financial.Investment.Domain.Tests` | Every rule in D1: move, archive, name validation, empty-portfolio deletion, history preserved intact |
| Application | `Tests/Financial.Investment.Application.Tests` | Orchestration, `ApplyAndSaveAsync` usage, `WriteCallCount` proving a rejected move never writes |
| API | `Tests/Financial.Api.Tests` | Round-trip via `WebApplicationFactory`; status codes from D5 |
| WPF | `Tests/Financial.Presentation.Tests` | Drop-target rules on the view model, post-move selection |
| Web | `Financial.Web/src/**/__tests__` | Vitest + RTL for the dialog, the drag handlers, and the api client |

`StubInvestmentRepository` (`Tests/Financial.TestUtilities/`) currently ignores the `scope` argument
in `GetAsset`/`GetAssetsByBrokerPortfolio` and has no `Investments` root, so it **must be extended**
with scope-aware brokers and a `GetInvestments()` backing to cover archiving. Its `WriteCallCount`
already distinguishes "entered the delegate" from "actually persisted", which is exactly the
assertion FR-009 and SC-003 need.

Constitution V forbids adding Moq/NSubstitute — extend the hand-written fake.

---

## Scale (measured, not estimated)

`data/data-investment.json` is 648 KB: 4 Active broker records / 10 portfolios / ~30 assets, and
3 Historic broker records / 14 portfolios / ~129 assets. A move touches two list positions in an
in-memory graph and rewrites one 648 KB document. SC-007's 2-second budget is dominated entirely by
the storage write (and, under the `GoogleDrive` provider, by the upload), not by the move itself —
no indexing, batching, or optimisation is warranted.
