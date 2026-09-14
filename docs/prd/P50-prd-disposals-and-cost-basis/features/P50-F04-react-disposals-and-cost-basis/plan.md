# Implementation Plan: F04. React — Disposals and Cost Basis

**Prerequisites:**
- F01, F02, F03 merged (confirmed on `main`).
- .NET SDK matching the repo's existing target framework; Node/npm matching `Financial.Web`'s existing
  toolchain.
- No new NuGet or npm packages — Fluent UI's `Accordion` family is already part of
  `@fluentui/react-components`.

**PR structure:** per `docs/rules/design.md`'s vertical-slice-per-PR rule and the 8-non-test-file
guideline, this feature ships as six PRs instead of one — three API slices, then three Web slices, in
that order. Each stage below is one PR; stop after each for review/merge before starting the next.

### Stage 1 (PR 1 — API): Disposal Record and Cost Basis Method Read Surface

**1. Disposal Record DTOs** - Add `DisposalRecordDTO` and `DisposalLotConsumptionDTO`, matching the
domain entity's fields one-for-one.

**2. Asset Details Extension** - Add `DisposalRecords` and `CostBasisMethod` to `AssetDetailsDTO`, and
map both in `NavigationService.GetAssetDetails` (resolving the parent broker the same way
`BrokerService` already does, Active then Historic).

**3. Tests and Contract Regen** - Extend `NavigationServiceTests` for the new mapping, regenerate the
OpenAPI snapshot and `Financial.Web`'s generated types, and add the two new type aliases to `types.ts`.

### Stage 2 (PR 2 — API): Open Lots Endpoint

**4. Open Lot DTO and Query** - Add `OpenLotDTO` and `INavigationService.GetOpenLots`, implemented via
the existing `OpenLotTracker.GetOpenLots`.

**5. Controller Route** - Add `GET {brokerName}/{portfolioName}/{assetName}/open-lots` to
`AssetsController`.

**6. Tests and Contract Regen** - Add controller/integration tests for the new route, regenerate the
OpenAPI snapshot and generated types, and add the `OpenLotDto` alias.

### Stage 3 (PR 3 — API): Broker Cost Basis Method Write Path

**7. Broker DTOs** - Add `CostBasisMethod` to `BrokerDTO`, an optional `CostBasisMethod` to
`BrokerCreateDTO`, and a new `SetCostBasisMethodRequestDTO`.

**8. Controller Route and Service Wiring** - Add `PUT {name}/cost-basis-method` to `BrokersController`
calling the existing `IBrokerService.SetCostBasisMethodAsync`; update `BrokerService.ToDto` and
`CreateBrokerAsync` to read/apply the method.

**9. Tests and Contract Regen** - Extend `BrokerServiceTests`/`BrokersControllerTests`, regenerate the
OpenAPI snapshot and generated types, and add the `CostBasisMethod`/`DisposalRecordStatus` enum
aliases.

### Stage 4 (PR 4 — Web): Disposals Tab

**10. Disposals Hook** - Add `useDisposals`, fetching the same asset-details call `useCredits`/
`useTransactions` already make, deriving the `Active` list (newest first), the tax years actually
present, and superseded chains grouped by `transactionId`.

**11. Disposals Tab Component** - Add `DisposalsTab`: the list (date/quantity/method/proceeds/cost
basis/gain-loss/tax year), a `FilterTabList` tax-year filter, and a per-row `Accordion` for the
superseded audit trail, covering the initial/loading/empty/filtered-empty states from the spec.

**12. Detail Panel Wiring** - Register the `disposals` tab in `DetailPanel` alongside the existing
asset-only tabs.

**13. Tests** - Hook and component tests per the spec's Testing Strategy.

### Stage 5 (PR 5 — Web): Admin Broker Cost Basis Method Field

**14. Broker Form Field** - Add a `CostBasisMethod` select to `BrokerFormDialog`, defaulting to
AverageCost for a new broker.

**15. Broker Submit Wiring** - Pass the selected method through broker create/update, calling the new
`cost-basis-method` endpoint when it changes on an existing broker.

**16. Tests** - Component tests for the new field and its submit wiring.

### Stage 6 (PR 6 — Web): SpecificId Lot Allocation on Sell/Redemption Entry

**17. Open Lots Hook** - Add `useOpenLots`, fetching on demand only while the lot picker is shown.

**18. Lot Allocation Picker** - Add `LotAllocationPicker`: one row per open lot with a quantity input
and a running allocated/remaining summary, rejecting over-allocation inline.

**19. Transaction Entry Wiring** - Extend `useTransactions` and `TransactionsTab` to show the picker for
a Sell/Redemption against a `SpecificId` broker, block save until the allocation matches the sale
quantity exactly, and include it in `specificLotAllocations` on create — preserving every entered field
(including the allocation) if the server rejects the sale.

**20. Tests and Documentation** - Hook/component tests per the spec's Testing Strategy; check off F04's
Section 9 acceptance criteria in the P50 PRD as each is satisfied; update `README.md` if the Disposals
workflow or Cost Basis Method setting needs a mention there.
