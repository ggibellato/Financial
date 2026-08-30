# Implementation Plan: F10. Persistent Create-Form Defaults Within a Session

**Prerequisites:**
- F01-F09 merged — every form this feature touches has its final field structure and validation pattern
  in place.
- No new tools/libraries — Web reuses the browser's `sessionStorage`; WPF reuses each workflow
  ViewModel's existing singleton lifetime.
- This feature ships as **four separate PRs**, each its own branch off `main`, merged before the next
  starts — see spec.md Decision D3. Each stage below corresponds to one PR.

### Stage (a): Storage module + Expense + Income + Transfer — PR 1

**1. Create `createFormDefaults.ts`** - New generic `sessionStorage`-backed key-value module, following
`domainStorage.ts`'s try/catch-guarded shape (spec.md Decision D1).

**2. Wire Expense (Web + WPF)** - `useExpenseForm.ts`'s `showCreateForm` reads date/payment-source/
credit-card/category from storage instead of unconditionally blanking them; a successful save writes
them back. `ExpenseWorkflowViewModel.cs` gets the equivalent private-field treatment.

**3. Wire Income (Web + WPF)** - Same treatment for `useIncomeForm.ts`/`IncomeWorkflowViewModel.cs`
(date, bank, income source).

**4. Wire Transfer (Web + WPF)** - Same treatment for `useTransferForm.ts`/`TransferWorkflowViewModel.cs`
(source bank, destination bank — date already defaults to today on both platforms).

**5. Test suite alignment (Stage a)** - Add persist-after-save and always-blank-amount/description tests
for all three forms on both platforms.

### Stage (b): Balance Correction + Withdrawal + Income Split — PR 2

**6. Wire Balance Correction (Web + WPF)** - `useBalanceAdjustmentForm.ts`/`AdjustmentWorkflowViewModel.cs`
persist the bank field, reversing the existing no-preselect behavior (spec.md Decision D4).

**7. Wire Withdrawal (Web + WPF)** - `useReserva.ts`'s withdrawal slice/`WithdrawalViewModel.cs` persist
date and bucket.

**8. Wire Income Split (Web + WPF)** - `useReserva.ts`'s split slice/`IncomeSplitViewModel.cs` persist
date (no entity-relation field exists for this form).

**9. Test suite alignment (Stage b)** - Same coverage shape as Stage (a) for these three forms.

### Stage (c): Add Bill + Create Entry + Investment Transaction — PR 3

**10. Wire Add Bill (Web + WPF)** - Persist Area only (no date field exists on this form).

**11. Wire Create Entry (Web + WPF)** - Persist date and Currency.

**12. Wire Investment Transaction (Web + WPF)** - Persist date and Type (Buy/Sell); confirm the exact
WPF ViewModel file (`TransactionDialogViewModel.cs`, per the audit's citation) before editing.

**13. Test suite alignment (Stage c)** - Same coverage shape for these three forms.

### Stage (d): Investment Credit + Price History — PR 4

**14. Wire Investment Credit (Web + WPF)** - Persist date and Type (Dividend/Rent/JCP); confirm the
exact WPF ViewModel file (`CreditDialogViewModel.cs`) before editing.

**15. Wire Price History (Web + WPF)** - Persist date only (no entity-relation field exists); confirm
the exact WPF ViewModel file (`PriceDialogViewModel.cs`) before editing.

**16. Test suite alignment (Stage d) and manual verification** - Same coverage shape for these two
forms. Manually verify all 11 forms across all four PRs, on both platforms, per
`docs/ui/review-checklist.md` — open a "New X" form, save, close, reopen, confirm date/entity-relation
persisted and amount/description are blank. This is the final stage where F10's (and the whole P38
PRD's) acceptance criteria are confirmed and checked off.
