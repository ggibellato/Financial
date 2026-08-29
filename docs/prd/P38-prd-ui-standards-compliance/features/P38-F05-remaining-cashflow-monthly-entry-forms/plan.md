# Implementation Plan: F05. Remaining CashFlow Monthly Entry Forms

**Prerequisites:**
- F01, F02, F03, F04 merged (tokens, shared primitives, naming fixes, and the proven Web/WPF
  per-field-validation pattern are all available).
- No new tools/libraries.
- This feature ships as **three separate PRs**, each its own branch off `main`, merged before the next
  starts — see spec.md Decision D1. Each stage below corresponds to one PR.

### Stage (a): Income + Transfer — PR 1

**1. Web Income field reorder and validation** - Reorder `IncomeForm.tsx`'s fields to Date → Source →
Bank → Description → Gross Value (conditional) → Net Value → Split checkbox (conditional); tag each of
`useIncomeForm.ts`'s existing `SAVE_ERROR` dispatches with its field and wire `useFieldError`.

**2. WPF Income field reorder, checkbox reposition, and validation** - Apply the same field order to
`IncomeFormView.xaml`, move the "split to reserve" checkbox to immediately follow Net Value, and add
derived per-field error properties to `IncomeWorkflowViewModel.cs` with their `FieldErrorTextStyle`
bindings and required-field markers.

**3. WPF Transfer validation** - Add derived per-field error properties to
`TransferWorkflowViewModel.cs` and wire `FieldErrorTextStyle`/required markers into
`TransferFormView.xaml` (no field reorder needed — order is already correct).

**4. WPF Balance Correction £ symbol fix** - Add the `£` symbol to the "Balance Corrected" confirmation
text's `StringFormat` in `BalanceAdjustmentFormView.xaml`, matching Web.

**5. Test suite alignment (Stage a)** - Update `IncomeForm.test.tsx` and `useIncomeForm.test.ts` for the
new field order and validation tagging; add per-field-error `[Fact]` tests to
`IncomeWorkflowViewModelTests.cs` and `TransferWorkflowViewModelTests.cs`; confirm
`TransferForm.test.tsx`/`BalanceAdjustmentForm.test.tsx` and their WPF equivalents still pass unmodified.

### Stage (b): Withdrawal + Edit Reserve Movement — PR 2

**6. Web Withdrawal and Edit Movement field reorder and validation** - Reorder both
`WithdrawalForm.tsx` and `EditMovementForm.tsx` to Date → Bucket → Description → Amount; tag
`useReserva.ts`'s `WITHDRAWAL_ERROR` and `SAVE_MOVEMENT_ERROR` dispatches with their field and wire
`useFieldError` into both forms.

**7. WPF Withdrawal layout modernization, reorder, and validation** - Migrate `WithdrawalFormView.xaml`
from its single-column label-left layout to the standard 4-column label-above-control layout with
themed controls, apply the Date-first field order, and add derived per-field error properties to
`WithdrawalViewModel.cs` with their bindings and required markers.

**8. WPF Edit Reserve Movement layout modernization, reorder, and validation** - Same treatment for
`EditReserveMovementFormView.xaml` and the Edit Movement properties on `ReservaViewModel.cs`.

**9. Test suite alignment (Stage b)** - Update `WithdrawalForm.test.tsx`, `EditMovementForm.test.tsx`,
and `useReserva.test.ts` for the new field order and validation tagging; add per-field-error `[Fact]`
tests to `WithdrawalViewModelTests.cs` and `ReservaViewModelTests.cs`.

### Stage (c): Income Split — PR 3

**10. Web Income Split validation** - Tag `useReserva.ts`'s `SPLIT_ERROR` dispatch with its field and
wire `useFieldError` into `IncomeSplitForm.tsx` (no field reorder).

**11. WPF Income Split layout modernization and validation** - Migrate `IncomeSplitFormView.xaml` to the
standard 4-column layout with themed controls, and add derived per-field error properties to
`IncomeSplitViewModel.cs` with their bindings and required markers.

**12. Test suite alignment (Stage c) and manual verification** - Update `IncomeSplitForm.test.tsx` and
`useReserva.test.ts` for the new validation tagging; add per-field-error `[Fact]` tests to
`IncomeSplitViewModelTests.cs`. Manually verify all six forms on both platforms per
`docs/ui/review-checklist.md` — field order, per-field validation, the WPF checkbox position, and the
£ symbol fix — since this final stage is where the feature's PRD acceptance criteria are confirmed and
checked off as a whole.
