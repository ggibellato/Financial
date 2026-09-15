# Implementation Plan: F05. WPF — Disposals and Cost Basis

**Prerequisites:**
- F01, F02, F03, F04 merged (confirmed on `main`).
- .NET SDK matching the repo's existing target framework; no new NuGet packages — WPF-UI's `Expander`
  and `DataGrid` are already part of the adopted control set.

**PR structure:** per `docs/rules/design.md`'s vertical-slice-per-PR rule and the 8-non-test-file
guideline, this feature ships as three PRs — no API/backend slice is needed (unlike F04) since F01–F03
already expose everything through the Application layer that `Financial.App` composes in-process. Each
stage below is one PR; stop after each for review/merge before starting the next.

### Stage 1 (PR 1): WPF Disposals Tab

**1. Disposals Row and View-State Models** - Add `DisposalRecordRowViewModel` (wraps one `Active`
`DisposalRecordDTO` plus its resolved `SupersededHistory` chain, built by grouping on `TransactionId`
and following `SupersededByRecordId`, same algorithm F04's `useDisposals` already uses) and
`DisposalsViewState` (per-asset selected tax year), mirroring `PriceHistoryViewState`'s shape.

**2. Disposals Tab ViewModel** - Add `DisposalsTabViewModel`: `Load(contextKey, disposalRecords)`
derives the sorted-newest-first `Active` rows and the tax-year filter options actually present via
`SelectableOptionGroup<string>`; `Clear` resets state; per-context-key view-state persistence mirrors
`PriceHistoryTabViewModel`.

**3. Disposals View and Tab Registration** - Add `DisposalsView.xaml`/`.xaml.cs`: tax-year filter row, a
`DataGrid` (date/quantity/method/proceeds/cost basis/gain-loss/tax year columns) with a
`RowDetailsTemplate` `Expander` per row for the superseded audit trail, and initial/loading/empty/
filtered-empty state presentation. Wire `AssetDetailsViewModel.Disposals` and register the new
"Disposals" `TabItem` (index 4, asset-only visibility) in `NavigationView.xaml`.

### Stage 2 (PR 2): Admin Broker Cost Basis Method Field

**4. Broker Form Dialog Field** - Add `CostBasisMethod` (string) and the `CostBasisMethods` static array
to `BrokerFormDialogViewModel`; add the "Cost Basis Method" `ComboBox` and explanatory text to
`BrokerFormDialog.xaml`, mirroring the existing `Currency` field's pattern.

**5. Broker Create/Edit Wiring** - `BrokersViewModel.CreateBrokerAsync` passes the selected method into
`BrokerCreateDTO`; `EditBrokerAsync` calls `IBrokerService.SetCostBasisMethodAsync` after a successful
rename/currency update only when the method actually changed, surfacing any failure as a "saved, but..."
message without discarding the rename (mirrors F04 Stage 5's `handleFormSubmit`).

### Stage 3 (PR 3): SpecificId Lot Allocation on Sell/Redemption Entry

**6. Lot Allocation Calculator and Row Model** - Add `LotAllocationCalculator` (tolerance-based
`SumAllocations`/`IsAllocationExact`/`IsLotOverAllocated`, mirroring
`Financial.Web/src/utils/lotAllocation.ts`) and `LotAllocationRowViewModel` (one open lot: source
transaction, date, remaining quantity, unit cost, editable quantity).

**7. Transaction Dialog Lot Allocation State** - Extend `TransactionDialogViewModel` with
`RequiresLotAllocation`, the lot-row collection, `AllocatedTotal`, and a `RetryOpenLotsCommand`; fold
allocation validity into `Validate()`/`CanConfirm()` so Confirm disables until the allocation exactly
matches the sale quantity and no lot is over-allocated.

**8. Open Lots Fetch and Submission** - Give `TransactionsTabViewModel` an optional `INavigationService?`
dependency; fetch (and retry) open lots when the Add form opens for Sell/Redemption against a
`SpecificId` broker; include the allocation as `TransactionCreateDTO.SpecificLotAllocations` on submit.

**9. Lot Allocation View** - Add `LotAllocationView.xaml`/`.xaml.cs` (editable-Quantity-column
`DataGrid`, running allocated/remaining summary, inline over-allocation error, loading/error/retry
states) and host it inside `TransactionFormView.xaml`, visible when `RequiresLotAllocation` is true.
