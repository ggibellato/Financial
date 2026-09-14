# Implementation Plan: F02. Disposal Record

**Prerequisites:**
- F01 (Cost Basis Method Strategy) merged — `Broker.CostBasisMethod` and `OpenLotTracker` exist.
- .NET SDK matching the repo's existing `Financial.Investment.*` target framework.
- No new NuGet packages, no configuration changes.

### Stage 1: Supporting Types and the Shared Weighted-Average Replay

**1. Disposal Record Types** - Add `DisposalRecordStatus`, `DisposalLotConsumption`, and the
immutable `DisposalRecord` entity (Id, TransactionId, Date, Method, LotsConsumed,
QuantityDisposed, Proceeds, CostBasis, GainLoss, Currency, TaxYear, Status, SupersededByRecordId,
CreatedAt) per the spec's Component Overview, with `Create`/`CreateWithId` factories.

**2. Tax Year Calculator** - Add `TaxYearCalculator.Calculate(date, currency)`, returning the BR
calendar year for `"BRL"` and the UK Apr 6–Apr 5 tax year for every other currency, per the spec's
Data Model example format.

**3. Extract the Shared Weighted-Average Replay** - Pull `Transactions.Apply`'s Increase-branch
formula into a new stateless `AverageCostReplay` rule and have `Transactions.Apply` call it instead
of inlining the formula. Confirm every existing `AveragePrice`/`RealizedCapitalGain` test still
passes unchanged — this step must not alter any observable behavior.

**4. Tests for Stage 1** - Unit tests for `DisposalRecord`'s derived `CostBasis`/`GainLoss`,
`TaxYearCalculator`'s BR/UK boundary, and a regression test proving `AverageCostReplay`'s
extraction left `Transactions.AveragePrice` byte-for-byte unchanged for existing fixtures.

### Stage 2: Disposal Record Calculator

**5. Specific Lot Allocation Type** - Add the `SpecificLotAllocation` record (caller's chosen
lot/quantity pairs for a SpecificId sale).

**6. Disposal Record Calculator** - Add `DisposalRecordCalculator.Calculate(...)`: AverageCost
builds a single synthetic `LotsConsumed` entry from `AverageCostReplay`'s running value;
FIFO auto-consumes `OpenLotTracker`'s oldest lots; SpecificId consumes exactly the caller's
`allocation`, rejecting a sum mismatch or an over-requested lot per the spec's Error Handling.

**7. Tests for Stage 2** - Unit tests per method (AverageCost, FIFO, SpecificId success and both
SpecificId rejection cases), per the spec's Testing Strategy table.

### Stage 3: Asset Integration

**8. Asset Disposal Records Collection** - Add `Asset.DisposalRecords` as a nested collection
persisted the same way as `Credits`/`PriceSnapshots`.

**9. Record Transaction Produces a Disposal Record** - Extend `Asset.RecordTransaction` to accept
`CostBasisMethod` and an optional `SpecificLotAllocation` list, validate a SpecificId allocation
before anything is created, compute and append exactly one `DisposalRecord` for a disposing
transaction (none for any other type), and refresh `Transactions`' realized-gain figure via the
new internal setter.

**10. Realized Gain/Loss Sourced From Disposal Records** - Update `Asset.RealizedGainLoss` to sum
`Active` `DisposalRecords` plus `Credits`, replacing its dependency on `Transactions`' own
accumulation.

**11. Tests for Stage 3** - Extend `AssetTests.cs`: one record per Sell/Redemption, none for other
types, `RealizedGainLoss` matches existing AverageCost fixtures' pre-feature figures exactly.

### Stage 4: On-Load Backfill

**12. Disposal Record Backfill** - Add `DisposalRecordBackfill.Apply(Investments)`: for every asset
under every broker (Active + Historic), append an `Active` AverageCost `DisposalRecord` to every
Sell/Redemption transaction with no matching record yet; log and skip only the affected transaction
on a single computation failure, per the spec's Error Handling.

**13. Wire the Backfill Into Load** - Call `DisposalRecordBackfill.Apply(investments)` from
`InvestmentLoader.LoadSync`, right after deserialization.

**14. Tests for Stage 4** - Unit tests for the backfill rule (creates records for every historic
disposal, idempotent on re-run, skip-and-log on a single failure) and an integration test loading a
legacy fixture with no `DisposalRecords` key to confirm records appear after load.

### Stage 5: Application Wiring and Documentation

**15. Transaction Service Resolves the Broker's Method** - Add `SpecificLotAllocationDTO` and
`TransactionCreateDTO.SpecificLotAllocations`; update `TransactionService.AddTransactionAsync` to
resolve the target broker (Active, else Historic) and pass its `CostBasisMethod` plus the mapped
allocation into `RecordTransaction`.

**16. Tests for Stage 5** - Extend `TransactionServiceTests.cs` to confirm the resolved broker's
method and allocation reach `RecordTransaction` correctly.

**17. PRD Checklist and Traceability** - Check off F02's Section 9 acceptance criteria in the P50
PRD as each is satisfied by the tests above, and record this stage's auto-accepted assumptions
where a reviewer will see them.
