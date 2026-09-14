# Implementation Plan: F03. Disposal Recalculation Policy

**Prerequisites:**
- F01 (Cost Basis Method Strategy) and F02 (Disposal Record) merged.
- .NET SDK matching the repo's existing `Financial.Investment.*` target framework.
- No new NuGet packages, no configuration changes.

### Stage 1: Supersede Transition and Regeneration Rule

**1. Disposal Record Supersede Transition** - Add `DisposalRecord.Supersede(Guid
supersededByRecordId)`, throwing if the record is already `Superseded`.

**2. Disposal Record Regenerator** - Add `DisposalRecordRegenerator` with `RegenerateAsset(Asset,
CostBasisMethod, DateTime anchor)`: walks the asset's transactions in replay order, and for every
disposing transaction at or after `anchor`, supersedes its current `Active` record (if one exists,
reconstructing a SpecificId allocation from its `LotsConsumed` when applicable) and computes+appends
a new `Active` record. Add `RegenerateBroker(Broker)` calling `RegenerateAsset` for every asset under
the broker with `DateTime.MinValue` as the anchor.

**3. Tests for Stage 1** - Unit tests for `Supersede`'s transition/guard and
`DisposalRecordRegenerator`'s asset-level and broker-level regeneration, chain-following, and
SpecificId-replay/failure behavior, per the spec's Testing Strategy.

### Stage 2: Trigger Wiring

**4. Backdated Transaction Wiring** - Extend `Asset.ReviseTransaction`, `Asset.RetractTransaction`,
and `Asset.RecordTransaction` to compute the regeneration anchor and call
`DisposalRecordRegenerator.RegenerateAsset` when that anchor is at or before the asset's latest
`Active` `DisposalRecord.Date`.

**5. Cost Basis Method Change Entry Point** - Add `BrokerService.SetCostBasisMethodAsync` that sets
the broker's method and calls `DisposalRecordRegenerator.RegenerateBroker` inside one
`ApplyAndSaveAsync`, so a failure leaves nothing changed.

**6. Tests for Stage 2** - Extend `AssetTests.cs` for the three wiring points (forward-dated changes
skip regeneration; backdated ones don't) and `BrokerServiceTests.cs` for the new entry point's
all-or-nothing behavior, per the spec's Testing Strategy.

### Stage 3: Documentation

**7. PRD Checklist and Traceability** - Check off F03's Section 9 acceptance criteria in the P50 PRD
as each is satisfied by the tests above, and record this feature's auto-accepted assumptions where a
reviewer will see them.
