# Implementation Plan: F01. Cost Basis Method Strategy

**Prerequisites:**
- .NET SDK matching the repo's existing `Financial.Investment.Domain`/`.Tests` target framework
- No new NuGet packages, no configuration changes

### Stage 1: Cost Basis Method on Broker

**1. Cost Basis Method Enum** - Add the `CostBasisMethod` enum (`AverageCost`, `FIFO`,
`SpecificId`) to the Investment domain, following the existing `ValuationMethod` enum's placement
and style.

**2. Broker Cost Basis Method Setting** - Add a `CostBasisMethod` property to `Broker`, defaulting
to `AverageCost` for both a newly created broker and any broker already in the data file, plus a
method to change it. Confirm the existing JSON persistence plumbing round-trips the new field with
no changes needed, per the spec's Data Model section.

**3. Tests for the Method Setting** - Add unit tests for the default and the setter in
`BrokerTests.cs`, and JSON default/round-trip tests in `InvestmentTypeInfoResolverTests.cs`, per
the spec's Testing Strategy.

### Stage 2: Open Lot Tracking

**4. Open Lot Rule** - Add the `OpenLot` record and the stateless `OpenLotTracker` rule that
replays an asset's transactions (using the existing replay ordering) and returns its currently
open purchase lots: oldest-first ordering with the existing same-date tie-break, lot splitting
when a disposal exceeds a lot's remaining quantity, and TransferOut depleting a lot's quantity
without producing any gain/loss.

**5. Tests for Open Lot Tracking** - Add unit tests covering ordering, the same-date tie-break,
splitting across multiple lots, TransferOut depletion, full lot consumption, and the no-transactions
case, per the spec's Testing Strategy. Extend (do not duplicate) the existing `AverageCost` tests
in `TransactionsTests.cs`/`AssetTests.cs` to confirm those figures are unaffected by this feature.

### Stage 3: Documentation

**6. PRD Checklist and Traceability** - Check off F01's Section 9 acceptance criteria in the P50
PRD as each is satisfied by the tests above, and record the auto-accepted assumptions from the
spec in a place a reviewer will see them.
