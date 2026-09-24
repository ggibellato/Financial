# Implementation Plan: F06. WPF Parity

**Prerequisites:**
- No new tools/libraries — uses the existing WPF-UI (Fluent, ADR-004), `RelayCommand`, `BoolToVisibilityConverter`, and shared `ElementStyle` resources already established in `Financial.App`.
- No configuration or environment variables.
- No backend change of any kind — F01-F04 already shipped the full Application-layer surface this feature consumes, and `Financial.App` already composes it in-process via `services.AddFinancialApplication()`; no new DI registration is needed for `ICorporateActionService`/`ICorporateActionQueryService` themselves.

**PR slicing note:** Mirrors F05's proven 5-PR structure (same feature, second platform), each a complete, independently-mergeable vertical increment, each under `docs/rules/design.md`'s 8-non-test-file guideline:
- **PR1** ships the tab, the ViewModel, and the Split form end-to-end — proves out the composition/inline-form/history-grid plumbing every later PR builds on.
- **PR2** adds the shared `TargetAssetPickerViewModel`/control Merger and Spin-off both need before either form can be built — independently unit-tested before PR3 wires it in, same as F05's Stage 2.
- **PR3** adds Merger: its field group, the `IsConfirmStep` two-step flow, the history grid's Tax Status column.
- **PR4** adds Spin-off, reusing PR2/PR3's picker and the confirm-step visibility-toggle mechanism — only needs its own field group and live preview.
- **PR5** adds the dashboard warning category and click-through focus, which depends on the history grid (PR1) already existing to focus a row inside.

Each PR ships passing `Financial.Presentation.Tests` and `dotnet build --configuration Release`, per `docs/rules/ui.md`'s completion requirement, and is manually verified side-by-side against the equivalent already-shipped React view before merge (same seeded data, both apps open together — P52's established WPF-parity verification standard, since this is presentation-only work a unit test cannot fully confirm looks/behaves right).

### Stage 1: Foundation and Split (PR1)

**1. `CorporateActionsTabViewModel`** — Add the tab-container ViewModel (spec.md §5): history collection rebuilt from `AssetDetailsDTO.CorporateActions` on load, inline-form open/edit/cancel state, Edit/Delete row commands against `ICorporateActionService`, mirroring `TransactionsTabViewModel`'s existing shape.

**2. `CorporateActionFormViewModel` (Split fields only)** — Add the inline form ViewModel scoped to Split for this PR: type selector (initially offering only Split until Stages 3-4 add Merger/Spin-off), the Split field group (Effective Date, N/M ratio inputs computing the decimal factor client-side, Note), combined `ValidationMessage`, `ConfirmCommand`/`CancelCommand`/`CloseRequested` matching `TransactionDialogViewModel`'s shape.

**3. `CorporateActionsView`/`CorporateActionFormView` (XAML)** — Add the tab's view (trigger, inline form host, history `DataGrid` with Date/Type/Affected-Linked-Asset/Resulting-Change/Actions columns using the shared column `ElementStyle`s) and the Split form's view (4-column `Grid` per ADR-002).

**4. `AssetDetailsViewModel`/`IAssetDetailsViewModel` wiring** — Compose `CorporateActions` alongside the existing `Transactions`/`Credits`/`PriceHistory`/`Disposals` construction; wire the new `TabItem` into `NavigationView.xaml`, gated on `IsAssetView` like Price History/Disposals; no deep-link focus entry point yet (Stage 5 adds that).

### Stage 2: Shared Target-Asset Infrastructure (PR2)

**5. `TargetAssetPickerViewModel`/`TargetAssetPickerControl`** — Add the shared search-or-create widget (editable `ComboBox` over a filtered candidate list from the existing admin-asset query, inline identity fields — ISIN/Exchange/Ticker/Country/Class — shown when the typed name matches nothing, surfacing the server's name-collision message through the combined `ValidationMessage` mechanism) per spec.md §4/§5. Covered by its own unit tests in this PR, independent of any consumer.

### Stage 3: Merger (PR3)

**6. `CorporateActionFormViewModel` (Merger fields + confirm step)** — Extend the type selector with Merger, add its field group (Effective Date, read-only Source Asset, `TargetAssetPickerViewModel`, Exchange Ratio, optional Cash-in-Lieu, Note), and the `IsConfirmStep` two-state flow with the same summary-sentence content F05 already shipped and "Confirm & Save"/"Back" actions (Back preserves every entered value).

**7. `CorporateActionsView` (Tax Status column)** — Add the history grid's Tax Status column, reusing `TaxWorkbookEntryRowViewModel`'s existing `ResolveStatus` resolver, populated from each row's `CalculationStatus` (present for Merger/Spin-off receiving-side records, absent for Split).

### Stage 4: Spin-off (PR4)

**8. `CorporateActionFormViewModel` (Spin-off fields + live preview)** — Extend the type selector with Spin-off, add its field group (Effective Date, read-only Parent Asset, `TargetAssetPickerViewModel` reused for the New Asset field, Quantity Received, Allocation % with a help affordance) and the live currency-split preview text computed from the already-loaded asset's quantity/cost-basis fields and the typed percentage.

**9. `CorporateActionsView` (Spin-off history rendering)** — Extend the grid row's "Resulting Change" formatting to cover Spin-off's parent/new-side fields alongside the existing Split/Merger formatting already added in Stages 1/3.

### Stage 5: Dashboard Integration and Focus (PR5)

**10. `DataQualityCategory`/`WarningHoldingRef`** — Add the `CorporateActionAwaitingTaxReview` enum value and the optional `CorporateActionId` field on `WarningHoldingRef`.

**11. `DataQualityWarningsViewModel` new category** — Add the `BuildCategories` branch mapping `report.CorporateActionsAwaitingTaxReview` to a `WarningCategoryViewModel`, secondary text matching F05's `"{type}, tax year {taxYear}"` format and its humanized (not raw-enum) type label.

**12. `IAssetDetailsViewModel.FocusCorporateAction`** — Add the focus entry point: selects the Corporate Actions tab index and hands the target record's id to `CorporateActionsTabViewModel` to scroll/select/focus the matching grid row.

**13. `DashboardViewModel` click-through wiring** — Extend `TrySelect` to call `FocusCorporateAction` on whichever tree's `AssetDetails` successfully resolved the holding, only when the selected `WarningHoldingRef` carries a `CorporateActionId`.
