---

description: "Task list for Move Assets Between Portfolios"
---

# Tasks: Move Assets Between Portfolios

**Input**: Design documents from `/specs/002-move-assets-between-portfolios/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: Test tasks are included and are **not optional**. Constitution V ("Test-Backed Changes")
makes them a condition of done for every increment: xUnit + FluentAssertions with no mocking library
(extend the hand-written fakes in `Tests/Financial.TestUtilities`), Vitest + React Testing Library
for Web. They are listed alongside the implementation they cover rather than as a separate TDD phase,
because this project does not mandate test-first — it mandates test-complete.

**Organization**: Tasks are grouped by user story. Each phase maps to one or more of the seven
delivery increments in [plan.md](./plan.md) §Delivery Increments — the increment number is the PR
boundary.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1–US5)
- Every task names its exact file path

## Path Conventions

Project folders sit flat at the repository root (no `DDD/`/`Presentation/` grouping folders):

- **Backend**: `Financial.Investment.{Domain,Application,Infrastructure}/`, `Financial.Api/`
- **WPF**: `Financial.App/`
- **Web**: `Financial.Web/src/`
- **Tests**: `Tests/<project>/`, and `Financial.Web/src/**/__tests__/`

---

## Phase 1: Setup

**Purpose**: Establish a known-good baseline. No project initialization is needed — this feature adds
no project and no dependency.

- [X] T001 Confirm the branch `002-move-assets-between-portfolios` is checked out and `git status` is clean apart from `specs/002-move-assets-between-portfolios/`
- [X] T002 Establish the baseline: run `dotnet build --configuration Release` and `dotnet test` from the repository root, and record that all test projects pass before any change
- [X] T003 [P] Establish the Web baseline: run `npm install`, `npm run lint`, `npm test`, and `npm run build` in `Financial.Web/`
- [X] T004 [P] Create a scratch data file for manual verification per `quickstart.md` §Prerequisites — copy `data/data-investment.example.json` to `%TEMP%\financial-move-test\data-investment.json`. **Never target `data/data-investment.json`.**

**Checkpoint**: Baseline green; scratch data ready.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The Domain primitives, the error-to-status mapping, and the test fake that **every**
user story depends on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T005 [P] Add `bool IsEmpty` (derived from `Assets.Count`, never stored) and `Asset? FindAsset(string name)` to `Financial.Investment.Domain/Entities/Portfolio.cs`
- [X] T006 Add `internal bool RemoveAsset(string name)` to `Financial.Investment.Domain/Entities/Portfolio.cs` — `internal` so that only `Broker` and `Investments` can relocate an asset (depends on T005)
- [X] T007 [P] Add `Portfolio? FindPortfolio(string name)` to `Financial.Investment.Domain/Entities/Broker.cs`
- [X] T008 [P] Add tests for `IsEmpty`, `FindAsset`, and `RemoveAsset` to `Tests/Financial.Investment.Domain.Tests/Domain/PortfolioTests.cs`
- [X] T009 [P] Add tests for `FindPortfolio` to `Tests/Financial.Investment.Domain.Tests/Domain/BrokerTests.cs`
- [X] T010 Add `InvestmentRuleViolationException` in `Financial.Investment.Domain/Exceptions/` and map it to `409 Conflict` in `Financial.Api/Middleware/DomainExceptionMappingMiddleware.cs`. **Revised from `research.md` §D5**, which proposed mapping `InvalidOperationException` itself: `YahooFinanceService`, `AssetPriceService` and `CryptocurrencyAssetPriceFetcher` already throw that for genuine upstream faults, so a global mapping would relabel real defects as client conflicts. A dedicated type follows the existing `OverdraftConfirmationRequiredException` precedent
- [X] T011 Add tests asserting `InvestmentRuleViolationException` yields 409 with the reason in `ProblemDetails.detail` while the asset and portfolio names stay out of the log, and that `InvalidOperationException` still propagates unmapped, in `Tests/Financial.Api.Tests/DomainExceptionLoggingTests.cs` (depends on T010)
- [X] T012 Extend `Tests/Financial.TestUtilities/StubInvestmentRepository.cs` so `GetAsset`, `GetAssetsByBroker`, and `GetAssetsByBrokerPortfolio` honour the `scope` argument instead of ignoring it, backed by scope-aware broker collections. Keep `WriteCallCount` semantics — it must still distinguish "entered the delegate" from "actually persisted"

**Checkpoint**: Domain primitives in place, rejections can carry a reason to the client, the fake can
model both scopes. User stories can begin.

---

## Phase 3: User Story 1 — Move an asset into another existing portfolio (Priority: P1) 🎯 MVP

**Goal**: Relocate an asset into an existing portfolio of the same broker, carrying its complete
transaction, credit, and price history unchanged, from both front ends.

**Independent Test**: Move an asset that has transactions and credits into a second portfolio of the
same broker; it is listed under the destination, absent from the source, and its quantity, average
price, transaction count, credit count, and price history are identical to before the move.

**Increment 1 of 7 — Domain → Application → API (PR boundary)**

- [X] T013 [US1] Implement `void MoveAsset(string sourcePortfolioName, string assetName, string destinationPortfolioName)` in `Financial.Investment.Domain/Entities/Broker.cs`: resolve both ends, detach from source, attach to destination. Throw `KeyNotFoundException` when the source portfolio or asset is missing (FR-011), `InvestmentRuleViolationException` when source and destination are the same portfolio (FR-006) or the destination already holds an asset with that name (FR-008). Reuse the existing get-or-create `AddPortfolio` for the destination (depends on T006, T007)
- [X] T014 [US1] Add Domain tests to `Tests/Financial.Investment.Domain.Tests/Domain/BrokerTests.cs` covering: the asset arrives with the same `Transactions`, `Credits`, and `PriceHistory` object contents and identical `Quantity`/`AveragePrice`/`RealizedGainLoss` (FR-002, FR-003); it is gone from the source (FR-005); same-portfolio destination throws; duplicate asset name in the destination throws; unknown asset and unknown portfolio throw `KeyNotFoundException` (depends on T013)
- [X] T015 [P] [US1] Create `MoveAssetRequestDTO` in `Financial.Investment.Application/DTOs/MoveAssetRequestDTO.cs` with `BrokerName`, `Scope`, `SourcePortfolioName`, `AssetName`, `DestinationPortfolioName`. **Revised from `data-model.md` §5**: one `Scope` rather than a source/destination pair, so the cross-scope move is unrequestable rather than requestable-and-refused. No "create the portfolio" flag: the name alone determines the outcome
- [X] T016 [P] [US1] ~~Create `AssetMoveResultDTO`~~ **Not built**: the move returns the existing `AssetDetailsDTO`, matching its neighbours. Every field of the proposed wrapper served the deletion increment and had no reader here; these DTOs are the wire format, so publishing them early would freeze a contract for undesigned behaviour
- [X] T017 [US1] Create `IAssetMoveService` in `Financial.Investment.Application/Interfaces/IAssetMoveService.cs` with `MoveAssetAsync` and `DeleteEmptyPortfolioAsync`, per `contracts/application-services.md`. Document why it throws instead of returning null, since the neighbouring services return `AssetDetailsDTO?` (depends on T015, T016)
- [X] T018 [US1] Implement `AssetMoveService` in `Financial.Investment.Application/Services/AssetMoveService.cs`. Resolve the broker within `Scope`, then run the mutation **inside** `repository.ApplyAndSaveAsync(...)` — never validate first and mutate after, and never nest a second `ApplyAndSaveAsync` call, because the exclusion is not reentrant and would deadlock (`research.md` §D4). **Every public method must follow the mandatory span convention** from `docs/rules/implementation.md`: `StartServiceSpan("Investment", nameof(AssetMoveService), operation, EntityType)` via a private `StartSpan` helper that also logs `"{Operation} started"`, then `span.MarkSuccess()` + `"{Operation} completed"`, with `catch (Exception ex) { span.MarkFailed(ex); throw; }` and **no logging in the rethrow** — domain messages embed financial values and must never reach the log (depends on T017)
- [X] T019 [US1] Register `IAssetMoveService` in `Financial.Investment.Application/DependencyInjection/InvestmentApplicationServiceCollectionExtensions.cs` (depends on T018)
- [X] T020 [US1] Add `Tests/Financial.Investment.Application.Tests/Services/AssetMoveServiceTests.cs`: the mutation runs inside `ApplyAndSaveAsync`; a rejected move leaves `WriteCallCount` at zero (FR-009, SC-003); `SourcePortfolioIsEmpty` is true only when the source is emptied; cross-broker and `historic → active` are refused. Use `RecordingTelemetryTracer` and `RecordingLogger<T>` from `Financial.TestUtilities` to assert the span is marked failed on rejection and that **no exception message is logged** (depends on T018, T012)
- [X] T021 [US1] Add `POST /assets/move` to `Financial.Api/Controllers/AssetsController.cs`, returning `AssetDetailsDTO`. Let the Domain exceptions propagate to the middleware rather than catching them (depends on T018, T010)
- [X] T022 [US1] Add round-trip tests to `Tests/Financial.Api.Tests/AssetEndpointsTests.cs`: 200 with the moved asset, 404 for an unknown asset, 409 for a same-portfolio destination and for a duplicate asset name in the destination (depends on T021)

**Increment 2 of 7 — WPF (PR boundary)**

- [X] T023 [US1] Create `Financial.App/MoveAssetDialog.xaml` and `.xaml.cs` following the existing `TransactionDialog` / `CreditDialog` / `PriceDialog` pattern: choose an existing destination portfolio, or type a new name. Right-align any numeric column shown (app-wide convention)
- [X] T024 [US1] Add the move command to `Financial.App/ViewModels/Investment/MainNavigationViewModelBase.cs`: open the dialog, call `IAssetMoveService`, catch `KeyNotFoundException`/`ArgumentException`/`InvalidOperationException` and surface `ex.Message` verbatim so the wording matches Web (FR-040), then call the existing `LoadNavigationTreeAsync()` and reselect the moved asset (FR-030, FR-037) (depends on T023, T018)
- [X] T025 [US1] Add tests to `Tests/Financial.Presentation.Tests/ViewModels/MainNavigationViewModelBaseTests.cs` for the move command: tree reloaded, moved asset selected, rejection message surfaced unchanged (depends on T024)
- [X] T026 [P] [US1] Add `Tests/Financial.Presentation.Tests/ViewModels/MoveAssetDialogViewModelTests.cs` covering destination selection and validation state, mirroring `CreditDialogValidationTests.cs`

**Increment 3 of 7 — Web (PR boundary)**

- [X] T027 [P] [US1] Add `MoveAssetRequestDto` and `AssetMoveResultDto` to `Financial.Web/src/api/types.ts` — these mirror the C# DTOs exactly, because Application DTOs are the literal wire format (depends on T015, T016)
- [X] T028 [US1] Add `moveAsset` to `Financial.Web/src/api/financialApiClient.ts` (depends on T027)
- [X] T029 [US1] Add `reload()` to `Financial.Web/src/context/SelectedNodeContext.tsx` and consume it in `Financial.Web/src/components/InvestmentTree.tsx`, promoting the existing `retryCount` re-fetch into an explicit reload (FR-030)
- [X] T030 [US1] Create `Financial.Web/src/components/MoveAssetDialog.tsx` and `.css`: pick an existing destination or type a new name; on failure show the `detail` carried by `ApiError` so the wording matches WPF (depends on T028)
- [X] T031 [US1] Wire the move action into the asset header in `Financial.Web/src/components/DetailPanel.tsx` (shown only when `isAsset`), and reselect the moved asset after a successful move (depends on T029, T030)
- [X] T032 [P] [US1] Add `Financial.Web/src/components/__tests__/MoveAssetDialog.test.tsx` covering submit, the rejection message, and the reload-and-reselect behaviour (depends on T030, T031)
- [X] T033 [P] [US1] Add `moveAsset` coverage to `Financial.Web/src/api/__tests__/financialApiClient.test.ts` — request shape and error mapping (depends on T028)

**Checkpoint**: An asset can be moved into an existing portfolio from both front ends, with identical
behaviour and identical rejection wording. This is the MVP.

---

## Phase 4: User Story 2 — Move into a portfolio created during the move (Priority: P2)

**Goal**: Name a portfolio that does not exist yet as the destination; it is created and receives the
asset in one confirmed action.

**Independent Test**: Move an asset to a portfolio name that does not exist under the broker; a
portfolio with that name now exists holding exactly that asset and appears in the navigation tree.

**Rides on increments 1–3** — the same files as US1, so these tasks ship with or immediately after
them.

- [ ] T034 [US2] Extend `Broker.MoveAsset` in `Financial.Investment.Domain/Entities/Broker.cs`: trim the destination name (FR-014); throw `ArgumentException` when it is blank or whitespace (FR-012); throw `InvalidOperationException` when a *new* name duplicates an existing portfolio ignoring case and padding (FR-013), while leaving lookup of an *existing* destination exact so current behaviour is unchanged (depends on T013)
- [ ] T035 [US2] Add Domain tests to `Tests/Financial.Investment.Domain.Tests/Domain/BrokerTests.cs`: a new name creates the portfolio holding only that asset; `"  SIPP  "` is stored as `"SIPP"`; blank and whitespace names throw `ArgumentException`; `"isa"` against an existing `"ISA"` throws and creates nothing (depends on T034)
- [ ] T036 [US2] Add API tests to `Tests/Financial.Api.Tests/AssetEndpointsTests.cs`: 200 creating a new destination portfolio, 400 for a blank name, 409 for a case-only duplicate — and assert nothing was created in the rejected cases (depends on T034, T021)
- [ ] T037 [P] [US2] Allow a new portfolio name to be typed in `Financial.App/MoveAssetDialog.xaml`(`.cs`) and surface the rejection reasons (depends on T023)
- [ ] T038 [P] [US2] Allow a new portfolio name to be typed in `Financial.Web/src/components/MoveAssetDialog.tsx` and surface the rejection reasons (depends on T030)
- [ ] T039 [P] [US2] Add new-portfolio cases to `Financial.Web/src/components/__tests__/MoveAssetDialog.test.tsx` (depends on T038)
- [ ] T040 [P] [US2] Add new-portfolio validation cases to `Tests/Financial.Presentation.Tests/ViewModels/MoveAssetDialogViewModelTests.cs` (depends on T037)

**Checkpoint**: Both destination kinds work from both front ends. Portfolios can now be created
without importing data — which was previously impossible from either UI.

---

## Phase 5: User Story 3 — Archive a closed asset from Active to Historic (Priority: P3)

**Goal**: Move a zero-quantity asset out of Active Investments into a Historic Investments portfolio,
with its history intact.

**Independent Test**: Take an asset whose buys and sells net to zero, move it from an Active portfolio
to a Historic one; it appears in Historic Investments with its full history and is gone from Active.

**Increment 4 of 7 (PR boundary)**

- [ ] T041 [US3] Add `Investments GetInvestments()` to `Financial.Investment.Application/Interfaces/IInvestmentRepository.cs`, documenting why the aggregate root is needed (archiving may have to add a broker to the Historic collection, which no scoped query can do — `plan.md` §Complexity Tracking)
- [ ] T042 [US3] Implement `GetInvestments()` in `Financial.Investment.Infrastructure/Repositories/InvestmentJsonRepository.cs` by returning the `Investments` field it already holds (depends on T041)
- [ ] T043 [US3] Back `GetInvestments()` in `Tests/Financial.TestUtilities/StubInvestmentRepository.cs` with a real `Investments` root so archiving is exercisable in Application tests (depends on T041, T012)
- [ ] T044 [P] [US3] Add `Broker? FindActiveBroker(string name)` and `Broker? FindHistoricBroker(string name)` to `Financial.Investment.Domain/Entities/Investments.cs`
- [ ] T045 [US3] Implement `void ArchiveAsset(string brokerName, string sourcePortfolioName, string assetName, string destinationPortfolioName)` in `Financial.Investment.Domain/Entities/Investments.cs`: throw `InvalidOperationException` unless `asset.Quantity == 0` — positive or negative alike (FR-017); create the broker's Historic record copying `Name` and `Currency` when absent (FR-043); then transfer, reusing the same detach/attach and destination-name rules as `Broker.MoveAsset` (depends on T044, T034)
- [ ] T046 [US3] Add Domain tests to `Tests/Financial.Investment.Domain.Tests/Domain/InvestmentsTests.cs`: a zero-quantity asset archives with full history; a positive quantity throws; a **negative** quantity throws for the same reason (US3 scenario 5); a broker with no Historic record gains one with the same name and currency (US3 scenario 6, FR-043); the asset is absent from Active afterwards (FR-020) (depends on T045)
- [ ] T047 [US3] Route `active → historic` requests to `Investments.ArchiveAsset` in `Financial.Investment.Application/Services/AssetMoveService.cs`, still inside the single `ApplyAndSaveAsync` delegate (depends on T045, T018, T041)
- [ ] T048 [US3] Add Application tests for the archive path to `Tests/Financial.Investment.Application.Tests/Services/AssetMoveServiceTests.cs`, including that a refused archive never writes (depends on T047, T043)
- [ ] T049 [US3] Add API tests to `Tests/Financial.Api.Tests/AssetEndpointsTests.cs`: 200 archiving a zero-quantity asset, 409 for a non-zero quantity, 409 for `historic → active` (FR-019) (depends on T047)
- [ ] T050 [P] [US3] Offer Historic destinations in `Financial.App/MoveAssetDialog.xaml`(`.cs`) when the asset is in Active and its quantity is zero; never offer Active destinations from Historic (FR-019) (depends on T037)
- [ ] T051 [P] [US3] Offer Historic destinations in `Financial.Web/src/components/MoveAssetDialog.tsx` on the same terms, sourcing them from the existing `GET /navigation/brokers?scope=historic` (depends on T038)
- [ ] T052 [P] [US3] Add archive cases to `Financial.Web/src/components/__tests__/MoveAssetDialog.test.tsx` (depends on T051)
- [ ] T053 [P] [US3] Add archive cases to `Tests/Financial.Presentation.Tests/ViewModels/MoveAssetDialogViewModelTests.cs` (depends on T050)

**Checkpoint**: A closed holding can be retired into Historic Investments without editing the data
file — the reason this feature exists.

---

## Phase 6: User Story 5 — Remove a portfolio left empty by a move (Priority: P5)

**Goal**: Delete a portfolio holding no assets — offered as a move finishes, and available standalone
at any time.

**Independent Test**: Move the last asset out of a portfolio, then delete the emptied source; it
disappears from the tree and from broker portfolio counts while the moved asset is untouched. Repeat
via the standalone action on a portfolio emptied earlier.

**Increment 5 of 7 (PR boundary)**

> **Sequenced before User Story 4 despite the lower priority.** US4 scenario 8 requires a drop that
> empties a portfolio to raise the deletion offer (FR-039), so the deletion must exist first. This
> matches `plan.md` §Delivery Increments, where deletion is increment 5 and the drag increments are
> 6 and 7.

- [ ] T054 [US5] Implement `bool RemoveEmptyPortfolio(string name)` in `Financial.Investment.Domain/Entities/Broker.cs`: throw `InvalidOperationException` when the portfolio still holds at least one asset (FR-022), `KeyNotFoundException` when it does not exist (depends on T007)
- [ ] T055 [US5] Add Domain tests to `Tests/Financial.Investment.Domain.Tests/Domain/BrokerTests.cs`: an empty portfolio is removed and the count drops; a populated one throws and is left with its assets intact (depends on T054)
- [ ] T056 [US5] Implement `DeleteEmptyPortfolioAsync` in `Financial.Investment.Application/Services/AssetMoveService.cs`, running inside its own `ApplyAndSaveAsync` call — a separate call from the move, never nested (depends on T054, T018)
- [ ] T057 [US5] Add Application tests for deletion to `Tests/Financial.Investment.Application.Tests/Services/AssetMoveServiceTests.cs`, including that a refused deletion never writes (depends on T056)
- [ ] T058 [US5] Create `Financial.Api/Controllers/PortfoliosController.cs` with `DELETE /portfolios/{brokerName}/{portfolioName}?scope=`, returning 204/404/409 per `contracts/rest-api.md`, using the existing `InvestmentScopeParser.ParseOrDefault` for the scope (depends on T056)
- [ ] T059 [US5] Add `Tests/Financial.Api.Tests/PortfolioEndpointsTests.cs`: 204 for empty, 409 for populated, 404 for unknown, and persistence across a repository reload (depends on T058)
- [ ] T060 [US5] Add the post-move deletion offer and a standalone delete command to `Financial.App/ViewModels/Investment/MainNavigationViewModelBase.cs`, driven by `SourcePortfolioIsEmpty`. Declining must leave the move applied (FR-024); after deletion, recover the selection to a valid node (FR-027) (depends on T056, T024)
- [ ] T061 [US5] Add tests to `Tests/Financial.Presentation.Tests/ViewModels/MainNavigationViewModelBaseTests.cs`: the offer appears only when the source is emptied; declining keeps the empty portfolio and the applied move; selection recovers after deleting the selected node (depends on T060)
- [ ] T062 [P] [US5] Add `deleteEmptyPortfolio` to `Financial.Web/src/api/financialApiClient.ts` (depends on T058)
- [ ] T063 [US5] Add the post-move offer and a standalone delete action to `Financial.Web/src/components/InvestmentTree.tsx` (and the dialog flow), recovering the selection after deleting the selected node (depends on T062, T029)
- [ ] T064 [P] [US5] Add deletion cases to `Financial.Web/src/components/__tests__/InvestmentTree.test.tsx` — offer shown only when emptied, decline keeps the portfolio, selection recovery (depends on T063)
- [ ] T065 [P] [US5] Add `deleteEmptyPortfolio` coverage to `Financial.Web/src/api/__tests__/financialApiClient.test.ts` (depends on T062)

**Checkpoint**: Emptied portfolios can be tidied away, or kept, from both front ends.

---

## Phase 7: User Story 4 — Move an asset by dragging it in the tree (Priority: P4)

**Goal**: Drag an asset onto a portfolio to move it there, or onto its broker to be asked for a new
portfolio name — in both front ends, applying exactly the same rules as the dialog route.

**Independent Test**: Drag an asset onto a sibling portfolio and confirm the same outcome as the
dialog; drag another onto its broker, name a new portfolio, and confirm it was created holding that
asset; confirm invalid targets refuse the drop and change nothing.

**Increments 6 and 7 of 7 (two PR boundaries: WPF, then Web)**

> Dragging adds **no** new rule and **no** new endpoint — it is a second route to
> `POST /assets/move` (FR-028). Any rule enforced only in a drag handler is a defect: the server must
> still refuse (FR-034).

**Increment 6 — WPF**

- [ ] T066 [US4] Add `bool IsDropTarget` and a `CanAccept(asset)` predicate to `Financial.App/ViewModels/Investment/TreeNodeViewModel.cs`. This state must live on the view model, not on the container: the `TreeView` uses `VirtualizingPanel.VirtualizationMode="Recycling"`, so a recycled `TreeViewItem` would otherwise carry another node's highlight (`research.md` §D7)
- [ ] T067 [US4] Create `Financial.App/Behaviors/TreeViewDragDropBehavior.cs` — pointer plumbing only: record the press point on `PreviewMouseLeftButtonDown`, start `DragDrop.DoDragDrop` past the drag threshold on `MouseMove`, set `e.Effects` on `DragOver`, invoke a command on `Drop`. No business logic (Constitution I) (depends on T066)
- [ ] T068 [US4] Add `AllowDrop="True"` and an `IsDropTarget` style trigger to the `TreeView` `ItemContainerStyle` in `Financial.App/Components/NavigationView.xaml`, and attach the behaviour (depends on T067)
- [ ] T069 [US4] Handle drops in `Financial.App/ViewModels/Investment/MainNavigationViewModelBase.cs`: a drop on a Portfolio node moves into it (FR-029); a drop on a Broker node prompts for a new portfolio name, then moves (FR-030, FR-031) — valid even though the asset already sits under that broker; cancelling the prompt changes nothing (FR-032); a drop that empties the source raises the US5 offer (FR-039) (depends on T068, T060)
- [ ] T070 [US4] Add `Tests/Financial.Presentation.Tests/ViewModels/TreeNodeDropTargetTests.cs` covering the FR-034 invalid set: the asset's own portfolio, any node of a different broker, another asset node, and the tree root (depends on T066)
- [ ] T071 [US4] Add drop-handling tests to `Tests/Financial.Presentation.Tests/ViewModels/MainNavigationViewModelBaseTests.cs`: portfolio drop moves; broker drop prompts then moves; cancelled prompt changes nothing; a rule rejection reports the same message as the dialog route (depends on T069)

**Increment 7 — Web**

- [ ] T072 [US4] Make asset rows draggable in `Financial.Web/src/components/InvestmentTree.tsx`: set `draggable` and `onDragStart` on the asset's `<li>` wrapper — not on the inner `<button>`, so its click and keyboard behaviour stay intact — carrying broker, portfolio, and asset names in `dataTransfer` with `effectAllowed = 'move'`
- [ ] T073 [US4] Add drop handling to `PortfolioNode` and `BrokerNode` in `Financial.Web/src/components/InvestmentTree.tsx`: call `preventDefault()` in `onDragOver` **only** for a valid target, so an invalid one genuinely refuses the drop (FR-034); handle `onDragEnter`/`onDragLeave` for highlighting; `onDrop` performs the move, and a broker drop prompts for a new portfolio name first (depends on T072, T028)
- [ ] T074 [US4] Add drop-target and drag-state styling to `Financial.Web/src/components/InvestmentTree.css`, visibly distinguishing a valid target under the pointer from an invalid one (FR-033, SC-010) (depends on T073)
- [ ] T075 [US4] Ensure a collapsed portfolio node still accepts a drop and an abandoned drag clears all highlight state, in `Financial.Web/src/components/InvestmentTree.tsx` (Edge Cases) (depends on T073)
- [ ] T076 [US4] Add drag tests to `Financial.Web/src/components/__tests__/InvestmentTree.test.tsx` using `fireEvent.dragStart`/`dragOver`/`drop` with a stub `dataTransfer`: a portfolio drop moves; a broker drop prompts then moves; a cancelled prompt changes nothing; **each invalid target leaves `preventDefault` uncalled**; releasing outside the tree cancels silently (depends on T073)

**Checkpoint**: Every acceptance scenario in User Story 4 passes in both front ends.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [ ] T077 Verify parity end to end: walk `quickstart.md` §3 and §4 in both front ends and confirm every rejection reads **identically** — the message originates in the Domain, so any difference in wording is a defect, not a styling choice (SC-005, FR-040)
- [ ] T078 Run the rejection sweep in `quickstart.md` §2d and confirm the data file hash is unchanged afterwards, with no `500` anywhere (SC-003)
- [ ] T079 Confirm SC-002 by hand per `quickstart.md` §2a: capture an asset's quantity, average price, realised gain, transaction count, credit count, and price history from `GET /assets/{broker}/{portfolio}/{asset}` before and after a move; they must be identical
- [ ] T080 [P] Run `dotnet test` across all test projects and `npm run lint && npm test && npm run build` in `Financial.Web/` — `npm run build`, not just `vitest`, because a DTO field missed in `types.ts` fails only at `tsc -b` and would break the Docker build
- [ ] T081 Check `netstat` for a listener on 8080 before running `npm run smoke-test` in `Financial.Web/` — the deployed Docker app binds that port, and starting a second process on it silently targets the live deployment
- [ ] T082 Verify deployability per Constitution VIII: `docker-compose up --build`, confirm the app starts on 8080 and the navigation tree, asset details, transactions, credits and prices still work. Record the commands in each PR body
- [ ] T083 [P] Grep `docs/baseline/04-wpf-app.md`, `docs/baseline/02-architecture.md`, and `context.md` for claims that the navigation tree is read-only or that portfolios can only be created by import, and correct any that this feature makes stale (Constitution VI)
- [ ] T084 Check off the acceptance-criteria boxes in `spec.md` as their work lands — per increment, in its own commit, never batched retroactively

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies
- **Foundational (Phase 2)**: depends on Setup — **blocks every user story**
- **US1 (Phase 3)**: depends on Foundational. The MVP
- **US2 (Phase 4)**: depends on US1 — extends the same `Broker.MoveAsset` and the same dialogs
- **US3 (Phase 5)**: depends on US2 (reuses its destination-name rules)
- **US5 (Phase 6)**: depends on US1 only; independent of US3
- **US4 (Phase 7)**: depends on US2 (broker-drop needs create-on-new-name) and on US5 (FR-039's offer)
- **Polish (Phase 8)**: depends on all shipped stories

### Delivery order (the seven PRs)

```
1. Increment 1  T013–T022   US1 backend      ← review hardest: fixes rules, status mapping, wire format
2. Increment 2  T023–T026   US1 WPF
3. Increment 3  T027–T033   US1 Web
   (US2 tasks T034–T040 ship with or immediately after increments 1–3)
4. Increment 4  T041–T053   US3 archive
5. Increment 5  T054–T065   US5 empty-portfolio deletion
6. Increment 6  T066–T071   US4 drag, WPF
7. Increment 7  T072–T076   US4 drag, Web
```

Each PR must build, pass CI's three jobs, and start under `docker-compose up` (Constitution VIII).
Target roughly 8 non-test code files per PR.

### Parallel Opportunities

- **Phase 1**: T003, T004 in parallel with T002
- **Phase 2**: T005, T007, T008, T009 in parallel; T010/T011 in parallel with the Domain work; T012 in parallel with everything
- **Phase 3**: T015 and T016 in parallel (different DTO files). Once increment 1 merges, increments 2 (WPF) and 3 (Web) are fully independent of each other
- **Phase 4**: T037/T038 in parallel (different front ends), then T039/T040 in parallel
- **Phase 5**: T044 in parallel with T041–T043; T050/T051 in parallel, then T052/T053
- **Phase 6**: T062 in parallel with the WPF tasks T060/T061
- **Phase 7**: increments 6 and 7 are independent once US5 has merged
- **Phase 8**: T080 and T083 in parallel

### Within Each User Story

Domain → Application → API → front ends. Tests accompany the code they cover and must pass before
the PR opens — existing tests must still pass, and a change that needs an existing test loosened is a
signal to re-examine the change (Constitution V).

---

## Parallel Example: Phase 2 (Foundational)

```bash
Task: "Add IsEmpty and FindAsset to Financial.Investment.Domain/Entities/Portfolio.cs"      # T005
Task: "Add FindPortfolio to Financial.Investment.Domain/Entities/Broker.cs"                 # T007
Task: "Map InvalidOperationException to 409 in DomainExceptionMappingMiddleware.cs"         # T010
Task: "Make StubInvestmentRepository scope-aware in Tests/Financial.TestUtilities/"         # T012
```

## Parallel Example: Phase 3, after increment 1 merges

```bash
# Two front ends, no shared files:
Task: "Create Financial.App/MoveAssetDialog.xaml(.cs)"                                      # T023
Task: "Add MoveAssetRequestDto/AssetMoveResultDto to Financial.Web/src/api/types.ts"        # T027
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 Setup → Phase 2 Foundational (blocks everything)
2. Phase 3, increment 1: the move works over HTTP with every rule enforced and every rejection
   explained
3. **STOP and VALIDATE**: `quickstart.md` §2a and §2d. If a rejection returns 500, T010 was missed
4. Increments 2 and 3 put it in front of the user in both front ends

### Incremental Delivery

Each increment is independently valuable and independently deployable:

- After increment 3 — misfiled assets can be corrected from either front end (the MVP)
- After US2 — portfolios can be created without importing data, which neither UI could do before
- After increment 4 — closed holdings can be archived, the reason the feature was asked for
- After increment 5 — emptied portfolios can be tidied away
- After increments 6 and 7 — the everyday interaction: drag it where it belongs

### Risk Notes

- **The non-reentrant write gate.** `ApplyAndSaveAsync` uses a `SemaphoreSlim(1,1)`; a nested call
  deadlocks the process rather than throwing. The move and the deletion are two client-driven calls,
  never one service method calling the other (T018, T056).
- **Validate inside the delegate, never before it.** Checking first and mutating second lets a
  concurrent write invalidate the check.
- **DTO drift.** Application DTOs are the literal wire format. A field added in C# and missed in
  `Financial.Web/src/api/types.ts` passes `vitest` and fails the Docker build — T080 is what catches
  it.
- **Never target `data/data-investment.json`.** Every manual check runs against the scratch copy from
  T004.
- **No migration is needed**, and none would run on restart anyway. This feature changes no persisted
  shape, so files stay readable by both the old and the new build.

---

## Notes

- `[P]` = different files, no dependency on incomplete work
- `[Story]` labels map tasks to `spec.md` user stories for traceability
- Commit per task or per logical group; branch per increment, PR when CI is green, never commit to
  `main`
- PR titles follow Conventional Commits (`feat|fix|docs|chore|refactor|test|perf|ci|build`), enforced
  by `semantic-pr.yml`
- PR bodies carry the per-criterion acceptance checklist, not a generic test-plan summary
- Stop at any checkpoint to validate a story independently
