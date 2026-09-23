# Implementation Plan: F02. Merger

**Prerequisites:**
- No new tools/libraries — uses the existing .NET 8 / System.Text.Json / FluentAssertions / xUnit stack already in `Financial.Investment.*`
- No configuration or environment variables
- Builds directly on F01's shipped `CorporateAction`/`CorporateActionReplay`/replay-consumer wiring — no re-work of that pipeline, only extension

**PR slicing note:** This feature is heavier than F01 (a two-asset operation touching four existing replay call sites plus a new one `SaleCoverageRule` F01's spec did not enumerate), so it splits into three PRs instead of two, each under CLAUDE.md's 8-non-test-file guideline:
- PR1 = Phase 1 (6 non-test files): the `CorporateAction` entity shape, the shared replay/lot math, and tax classification support — self-contained, unit-tested in isolation, not yet reachable from `Asset` or any API.
- PR2 = Phase 2 (5 non-test files): wires the new math into `Transactions`, `SaleCoverageRule`, `OpenLotTracker`, `DisposalRecordCalculator`, and the `Asset` aggregate's merger-aware `Record`/`Revise`/`RetractCorporateAction` — a complete, independently-testable Domain capability, still unreachable from any API.
- PR3 = Phase 3 (6 non-test files): the Application-layer two-asset orchestration, new DTOs, and the controller routes — completes the vertical slice end-to-end.

### Phase 1: Corporate Action Entity, Replay Math, and Tax Classification Support

**1. Merger Shape on `CorporateAction`** - Add `CorporateActionType.Merger`, the nested `MergerRole` enum, and the merger-specific nullable fields (correlation id, linked asset name, exchange ratio, cash-in-lieu, converted quantity, carried cost basis), plus `CreateMergerSource`/`CreateMergerTarget` factories (and their `WithId` variants) with the validation rules from spec.md §4 — exchange ratio `> 0` (no "≠ 1.0" rule, unlike Split), cash-in-lieu `≥ 0` when present. `RatioFactor` becomes nullable.

**2. Position and Lot Dispatch** - Add `CorporateActionReplay.ApplyToPosition`, dispatching on a corporate action's type/role to the existing Split rescale, a merger source's unconditional close, or a new merger target's blended-average receive math. This is the single new entry point every position-facing replay consumer will call in Phase 2.

**3. Tax Classification for Corporate Actions** - Add `SourceType.CorporateAction`, `EventCategory.CorporateAction`, `TaxClassification.CreateForCorporateAction`, and `TaxClassificationCalculator.CalculateForCorporateAction` (jurisdiction from a supplied `Currency`, status `RequiresReview` by default, `Final` when a matching `TaxRule` exists — the PRD's explicit departure from Disposal/Credit's `Incomplete` default).

### Phase 2: Replay Integration and Aggregate Wiring

**4. Position Replay Integration** - Update `Transactions.Recompute` and `SaleCoverageRule.FindFirstUncoveredSale` to call the new `ApplyToPosition` dispatcher instead of their existing inline Split-only rescale call, so both position tracking and pre-mutation sale-coverage validation see a merger's close/receive effect correctly.

**5. Open Lot Dispatch** - Update `OpenLotTracker.GetOpenLots` so a merger source clears every open lot and a merger target gains one new lot at the carried unit cost, alongside the existing Split rescale case.

**6. Disposal Calculation Integration** - Update `DisposalRecordCalculator.BuildAverageCostLot`'s replay branch to use the new dispatcher; confirm (by test, not by new code) that `BuildAutoConsumedLots`/`BuildSpecificIdLots` need no change since they already delegate to `OpenLotTracker`.

**7. Asset Aggregate Wiring** - Add `Asset.PositionAsOf(DateTime)` (generalizing F01's existing zero-quantity replay-and-stop loop into a reusable as-of-date preview), and extend `RecordCorporateAction`/`ReviseCorporateAction`/`RetractCorporateAction` to be merger-aware: the zero-quantity guard runs only for Split and merger-source records; a merger-target record additionally builds and appends its linked `TaxClassification` (needs the new optional `Currency?` parameter); retracting a merger-target record supersedes that classification via the existing supersede policy.

### Phase 3: Application Orchestration, Contracts, and API

**8. Merger DTOs** - Add `CorporateActionMergerCreateDTO`, `CorporateActionMergerUpdateDTO`, `CorporateActionMergerResultDTO` (a paired `Source`/`Target` `AssetDetailsDTO` response) per spec.md §5.

**9. Service Orchestration** - Extend `ICorporateActionService`/`CorporateActionService` with `AddMergerAsync`/`UpdateMergerAsync`: resolve the source asset and its broker's currency, resolve or inline-create the target asset (reusing `Asset.Create` + `Portfolio.RegisterAsset`, atomically inside the same `ApplyAndSaveAsync` closure), compute the source's as-of-date position via `PositionAsOf`, record the linked source/target `CorporateAction` pair with an outer rollback if the target step fails. Rename `DeleteSplitAsync` to `DeleteCorporateActionAsync` and teach it to dispatch by the found action's type — single-asset retraction for Split, both-linked-records retraction for Merger.

**10. API Endpoint** - Add `POST`/`PUT corporate-actions/merger` to the existing `CorporateActionsController`, update the controller's delete call to the renamed service method, and refresh the OpenAPI contract snapshot (and generated frontend types) for the new routes and DTOs.
