# Implementation Plan: F05. React Entry and Display

**Prerequisites:**
- No new tools/libraries — uses the existing `@fluentui/react-components`/`@fluentui/react-icons` (ADR-004), Vitest + React Testing Library stack already in `Financial.Web`. Fluent's `Combobox` component (used by `TargetAssetPicker`) is already part of `@fluentui/react-components`, already a dependency — no new package.
- No configuration or environment variables.
- No backend change of any kind — F01-F04 already shipped the full API surface this feature consumes; `npm run generate-api-types` does **not** need to be re-run (the committed `openapi.ts` already reflects the shipped contract — verified by reading it directly).

**PR slicing note:** This is a substantial UI feature (three type-specific entry forms, a shared search-or-create widget, a two-step confirmation flow, a history list, and a dashboard integration), so it is sliced into five PRs instead of one, each a complete, independently-mergeable vertical increment rather than disconnected scaffolding, and each cleanly under `docs/rules/design.md`'s 8-non-test-file guideline (no PR needs the "same mechanical change repeated" exception, so none is stretched past it):
- **PR1** (7 non-test files) ships the tab, the hook, and the Split form end-to-end — the simplest of the three types, and it proves out the shared plumbing (API client, types, `DetailPanel` wiring, history list) every later PR builds on.
- **PR2** (4 non-test files) adds the shared target-asset infrastructure Merger and Spin-off both need before either can be built: the `TargetAssetPicker` widget, its `useAssetSearchOptions` hook, and the one-time `StatusBadge` extraction (a mechanical, no-behavior-change move of an existing component out of `TaxPage.tsx`, done here since Merger is the first type to need it). Each piece has its own direct unit-test coverage, so it is a genuine, independently-verifiable increment even before PR3 wires it into a form — not scaffolding left dangling.
- **PR3** (4 non-test files) adds Merger: its field group, the two-step confirmation flow, the history table's new Tax Status column, and the `/corporate-actions/merger` API client/types — consuming PR2's picker and `StatusBadge`.
- **PR4** (4 non-test files) adds Spin-off, which reuses everything PR2/PR3 already built (`TargetAssetPicker`, the confirm-step shape, `StatusBadge`) and only needs its own field group, live preview, and the `/corporate-actions/spin-off` API client/types.
- **PR5** (5 non-test files) adds the dashboard warning category and the click-through-to-specific-record deep link, which depends on the history list (PR1) already existing to focus a row inside.

Each PR ships passing `npm run lint`, `npm test`, and `npm run build`, per `docs/rules/ui.md`'s completion requirement, and is independently reviewable against `docs/ui/review-checklist.md`.

### Stage 1: Foundation and Split (PR1)

**1. API Client and Type Aliases (Split slice)** — Add `CorporateActionDto`, `CorporateActionType`, `CorporateActionRole`, `CorporateActionSplitCreateDto`/`UpdateDto`, `CorporateActionDeleteDto` to `Financial.Web/src/api/types.ts` (direct `Schema<'...DTO'>` aliases, matching the file's existing convention), and `addSplit`/`updateSplit`/`deleteCorporateAction` to `Financial.Web/src/api/financialApiClient.ts`, following `addTransaction`/`updateTransaction`/`deleteTransaction`'s exact request-building shape against the already-shipped `/corporate-actions/split` and `/corporate-actions` routes.

**2. `useCorporateActions` Hook (Split slice)** — Add the reducer-driven state/effects hook (spec.md §5), scoped to Split only for this PR: fetch via the asset's already-loaded `corporateActions` list, inline-form open/edit/cancel, save (add/update), delete, with the same `isSaving`/`saveError`/`saveErrorFields`/`deleteError` shape `useTransactions` already establishes.

**3. `CorporateActionForm` Component (Split fields only)** — Add the inline form component with its type selector (`Select`, initially offering only "Split / Reverse Split" until Stages 2-3 add the other two options), the Split field group (Effective Date, N/M ratio inputs computing `ratioFactor` client-side with the PRD-specified inline example text, Note), validation, and the saving/server-error/disabled states per spec.md §4.

**4. `CorporateActionsTab` Component** — Add the tab container: "New Corporate Action" trigger, the form when open, and the history table (Date, Type, Affected/Linked Asset, Resulting Change — Tax Status column and its `StatusBadge` are added in Stage 2 since Split never has a `calculationStatus`) with Edit/Delete row actions (`confirmThenRun` on Delete), loading/empty/error states via the shared `LoadingState`/`ErrorState`.

**5. `DetailPanel` Wiring** — Add `corporateActions` to `DetailPanel`'s `TabId`/asset tab list (lazy-loaded, matching every other tab); no deep-link routing yet (Stage 4/PR4 adds that).

### Stage 2: Shared Target-Asset Infrastructure (PR2)

**6. `StatusBadge` Extraction** — Move `TaxPage.tsx`'s private `StatusBadge`/`STATUS_PRESENTATION` into `Financial.Web/src/components/StatusBadge.tsx` unchanged, update `TaxPage.tsx` to import it — a pure, no-behavior-change refactor, done here because Merger (Stage 3) is the first corporate-action type that produces a `CalculationStatus` to display.

**7. `useAssetSearchOptions` Hook** — Add the target-asset candidate-list hook: calls the existing `getAdminAssets()`, filters client-side to the current broker+portfolio, excludes the asset the user is already on.

**8. `TargetAssetPicker` Component** — Add the shared search-or-create widget (Fluent `Combobox` over `useAssetSearchOptions`, inline identity fields reusing `AssetFormDialog`'s field set when the typed name matches nothing, surfacing the server's 409 name-collision message as the field's own error) per spec.md §4/§5. Covered by its own component tests in this PR, independent of any consumer.

### Stage 3: Merger (PR3)

**9. `CorporateActionForm` (Merger fields + confirmation step)** — Extend the type selector with "Merger", add its field group (Effective Date, read-only Source Asset, `TargetAssetPicker`, Exchange Ratio, optional Cash-in-Lieu, Note), and the two-step `fields`/`confirm` flow with the PRD-specified summary sentence and "Confirm & Save"/"Back" actions.

**10. `CorporateActionsTab` (Tax Status column)** — Add the history table's Tax Status column using the now-shared `StatusBadge`, populated from each history entry's `calculationStatus` (present for Merger/Spin-off receiving-side records, `null`/absent for Split).

**11. API Client and Type Aliases (Merger slice)** — Add `CorporateActionMergerCreateDto`/`UpdateDto`/`ResultDto` to `types.ts` and `addMerger`/`updateMerger` to `financialApiClient.ts`, against the already-shipped `/corporate-actions/merger` routes.

### Stage 4: Spin-off (PR4)

**12. `CorporateActionForm` (Spin-off fields + live preview)** — Extend the type selector with "Spin-off", add its field group (Effective Date, read-only Parent Asset, `TargetAssetPicker` reused for the New Asset field, Quantity Received, Allocation % with `InfoLabel` help text, Note) and the live currency-split preview text computed from the already-loaded asset's `quantity`/`averagePrice` and the typed percentage.

**13. `CorporateActionsTab` (Spin-off history rendering)** — Extend the history row's "Resulting Change" formatting to cover Spin-off's parent/new-side fields alongside the existing Split/Merger formatting already added in Stages 1/3.

**14. API Client and Type Aliases (Spin-off slice)** — Add `CorporateActionSpinOffCreateDto`/`UpdateDto`/`ResultDto` to `types.ts` and `addSpinOff`/`updateSpinOff` to `financialApiClient.ts`, against the already-shipped `/corporate-actions/spin-off` routes.

### Stage 5: Dashboard Integration and Deep Link (PR5)

**15. Type Alias for the New Finding** — Add `CorporateActionAwaitingTaxReviewFindingDto` to `types.ts` (the `corporateActionsAwaitingTaxReview` array already exists on the generated `DataQualityReportDto` with no change needed there).

**16. `DataQualityWarningsPanel` New Category** — Add `'corporateActionAwaitingTaxReview'` to `WarningCategoryId` and a `WarningCategory` mapping `report.corporateActionsAwaitingTaxReview` to rows (secondary text: type + tax year, matching the existing categories' secondary-text density), positioned per spec.md §2's information-hierarchy call (grouped with the existing tax-related category).

**17. `useHoldingNavigation` Deep-Link Parameter** — Add the optional `corporateActionId` parameter to `navigateToHolding`, carried in router state as `pendingCorporateActionId` alongside the existing `pendingSelection`.

**18. `DetailPanel` Deep-Link Routing** — Read `location.state.pendingCorporateActionId` via `useLocation`; when present for the node just resolved, select the `corporateActions` tab instead of the default reset-to-`summary`, and pass the id down to `CorporateActionsTab` as `focusRecordId`.

**19. `CorporateActionsTab` Focus Behavior** — Add `focusRecordId` handling: once the history list has loaded, scroll the matching row into view and move focus to it, briefly highlighting it — mirroring `DataQualityWarningsPanel`'s own existing `focusToken` pattern for consistency.
