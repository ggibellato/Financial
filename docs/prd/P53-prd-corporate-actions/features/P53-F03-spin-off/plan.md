# Implementation Plan: F03. Spin-off

**Prerequisites:**
- No new tools/libraries — uses the existing .NET 8 / System.Text.Json / FluentAssertions / xUnit stack already in `Financial.Investment.*`
- No configuration or environment variables
- Builds directly on F01's shipped `CorporateAction`/`CorporateActionReplay`/replay-consumer wiring and F02's shipped two-linked-records/two-asset-orchestration pattern — no re-work of either, only extension

**PR slicing note:** F03 is structurally lighter than F02: reading the current code confirms `Transactions.cs`, `SaleCoverageRule.cs`, `DisposalRecordCalculator.cs` and `DisposalRecordRegenerator.cs` already dispatch through the generic `CorporateActionReplay.ApplyToPosition`/`OpenLotTracker.GetOpenLots` entry points F02 built, with no `CorporateActionType`-specific branching left in any of them — so, unlike F02 (which had to fix four separate call sites plus one F01's own spec had missed), F03 only touches the two dispatch points themselves plus the aggregate/tax/service layers. This keeps the whole feature to two PRs, both under CLAUDE.md's 8-non-test-file guideline:
- PR1 = Phase 1 (5 non-test files): the `CorporateAction` entity shape (including the `MergerRole` → `CorporateActionRole` generalization), the two new replay/lot dispatch cases, the `Asset` aggregate's spin-off-aware tax-classification hook and delete-guard message, and the widened `TaxClassificationCalculator` guard — a complete, independently-testable Domain capability, still unreachable from any API.
- PR2 = Phase 2 (6 non-test files): the Application-layer two-asset orchestration (including generalizing Merger's inline-asset-creation helper for reuse), new DTOs, and the controller routes — completes the vertical slice end-to-end.

### Phase 1: Corporate Action Entity, Replay/Lot Dispatch, and Aggregate Wiring

**1. Spin-off Shape on `CorporateAction`** - Add `CorporateActionType.SpinOff`, rename the nested `MergerRole` enum to `CorporateActionRole` with two new members (`Parent`, `New`), add the new `AllocationPercentage` field, and add `CreateSpinOffParent`/`CreateSpinOffNew` factories (and their `WithId` variants) with the validation rules from spec.md §4 — allocation percentage `0`–`100` inclusive (validated only on the `Parent` factory, mirroring Merger's `Source`-only validation), quantity received `> 0`.

**2. Position and Lot Dispatch** - Extend `CorporateActionReplay.ApplyToPosition` with `SpinOff`/`Parent` (quantity unchanged, average price scaled by `1 − allocation%`) and `SpinOff`/`New` (rename `ReceiveMerger` to `ReceiveIntoPosition` and reuse it for both `Merger`/`Target` and `SpinOff`/`New`). Extend `OpenLotTracker.ApplyCorporateAction` with `SpinOff`/`Parent` (a new `ReduceLotCostBasis` helper — unit cost scaled, quantity untouched) and `SpinOff`/`New` (extract the existing `Merger`/`Target` lot-append into a shared `AppendCarriedLot` helper, reused by both).

**3. Tax Classification and Aggregate Wiring** - Widen `TaxClassificationCalculator.CalculateForCorporateAction`'s guard to accept `SpinOff`/`New` alongside `Merger`/`Target` (still reusing `EventCategory.CorporateAction`, no new category). Generalize `Asset`'s private `IsMergerTarget`/`CanClassifyMergerTarget`/`AppendMergerTargetTaxClassification`/`ReviseMergerTargetTaxClassification`/`SupersedeMergerTargetTaxClassification` helpers to cover either "receiving" role (`Merger`/`Target` or `SpinOff`/`New`), and add the third branch to `RetractCorporateAction`'s delete-guard rejection message. Confirm (by test, not by new code) that `EnsureNonZeroPositionAt`'s existing condition already excludes `SpinOff` with no change needed.

### Phase 2: Application Orchestration, Contracts, and API

**4. Spin-off DTOs** - Add `CorporateActionSpinOffCreateDTO`, `CorporateActionSpinOffUpdateDTO`, `CorporateActionSpinOffResultDTO` (a paired `Parent`/`New` `AssetDetailsDTO` response) per spec.md §5.

**5. Shared Linked-Asset Resolution** - Generalize `CorporateActionService.ResolveOrCreateTargetAsset` (currently typed to `CorporateActionMergerCreateDTO`) into a DTO-agnostic `ResolveOrCreateLinkedAsset` taking explicit identity parameters, and update `AddMergerAsync` to call the generalized version — a small, behavior-preserving signature change that lets Spin-off reuse it instead of duplicating the inline-asset-creation branch.

**6. Service Orchestration** - Extend `ICorporateActionService`/`CorporateActionService` with `AddSpinOffAsync`/`UpdateSpinOffAsync`, mirroring `AddMergerAsync`/`UpdateMergerAsync`'s shape: resolve the parent asset and its broker's currency, compute the parent's as-of-date position via `PositionAsOf`, compute the carried cost basis (`allocationPercentage / 100 × prior total cost basis`) from the user-entered allocation, resolve or inline-create the new asset via the now-shared `ResolveOrCreateLinkedAsset`, record the linked parent/new `CorporateAction` pair with an outer rollback if the new-asset step fails. Confirm (by test, not by new code) that `DeleteCorporateActionFromAsset`'s existing type-agnostic branch already handles a spin-off's two-linked-record retraction correctly.

**7. API Endpoint** - Add `POST`/`PUT corporate-actions/spin-off` to the existing `CorporateActionsController`, and refresh the OpenAPI contract snapshot (and generated frontend types) for the new routes and DTOs.
