# Implementation Plan: F02. Shared Form UX Primitives (Core Scope)

**Prerequisites:**
- F01 merged (design tokens available — `SystemFillColorCriticalBrush` already proven on WPF; Web
  tokens already declared).
- No new tools/libraries/environment variables required — Fluent `Field`/`InfoLabel` and WPF-UI
  `Flyout`/`SymbolIcon` are already available in the installed package versions.

### Stage 1: Web Per-Field Validation and Required-Field Primitives

**1. Extract the shared `useFieldError` hook** - Pull the identical inline `fieldError` lookup
function out of `TransferForm.tsx` and `BalanceAdjustmentForm.tsx` into one shared hook, and switch
both forms to use it. Behavior is unchanged — this is a pure extraction.

**2. Mark the genuinely required fields on both forms** - Add Fluent `Field`'s `required` prop to
Transfer's Date/From/To/Amount and Balance Correction's Bank/Target Balance fields, leaving optional
fields (Note) unmarked.

### Stage 2: Web Contextual Help Primitive

**3. Add the first real `InfoLabel` usage** - Wrap `BalanceAdjustmentForm.tsx`'s "Target Balance"
field label with Fluent `InfoLabel`, explaining that the field is a target the app diffs against the
current calculated balance, not a delta amount.

### Stage 3: WPF Per-Field Validation and Required-Field Primitives

**4. Add the shared error-visibility converter and error-text style** - Create
`NullOrEmptyToVisibilityConverter` and register it plus a new `FieldErrorTextStyle` named style in
`App.xaml`, following the existing named-style convention (`NumericColumnTextStyle`, etc.).

**5. Derive Balance Correction's per-field error and wire it into the view** - Add
`AdjustmentWorkflowViewModel.TargetBalanceFieldError`, derived from the existing
`AdjustmentSaveError` the same way Web's `mapBalanceAdjustmentErrorToField.ts` already does, and bind
a `FieldErrorTextStyle` `TextBlock` under the Target Balance field in
`BalanceAdjustmentFormView.xaml`, additive to the existing bottom-of-form error message.

**6. Add the required-field asterisk to Bank and Target Balance** - Apply the two-`Run` label pattern
plus `AutomationProperties.HelpText="Required"` on both fields' input controls.

### Stage 4: WPF Contextual Help Primitive

**7. Build the reusable `HelpFlyoutButton` control** - A small `UserControl` with a `HelpText`
dependency property, following `Controls/FilterableColumnHeader.xaml`'s existing pattern, rendering
an `Info16` icon button that opens a `Flyout` with the help text.

**8. Apply it next to Target Balance's label** - Wire the same explanation used on Web into
`BalanceAdjustmentFormView.xaml` via the new control.
