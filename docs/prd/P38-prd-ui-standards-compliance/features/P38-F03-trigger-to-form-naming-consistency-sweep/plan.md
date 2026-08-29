# Implementation Plan: F03. Trigger-to-Form Naming Consistency Sweep

**Prerequisites:**
- F01 merged (already the case) — not a hard dependency of this feature, but keeps the branch base
  current.
- No new tools/libraries/environment variables required.

### Stage 1: Web CashFlow Entry-Form Text Fixes

**1. Fix Transfer and Balance Correction naming (Web)** - Update `TransferForm.tsx`'s and
`BalanceAdjustmentForm.tsx`'s header and confirm-button text so the trigger's noun carries through
unchanged into both create and edit mode, per the spec's target-wording table.

**2. Fix Withdrawal and Income Split naming (Web)** - Update `WithdrawalForm.tsx`'s and
`IncomeSplitForm.tsx`'s header and confirm-button text the same way; both are create-only, so no
edit-mode branch is affected.

### Stage 2: Investment "New" Trigger Visibility (Web + WPF)

**3. Give the Web Investment tabs' bare "New" buttons a visible entity name** - Update
`TransactionsTab.tsx`, `CreditsTab.tsx`, and `PriceHistoryTab.tsx`'s trigger buttons to match each
tab's own already-correct, already-sentence-case form title exactly.

**4. Give the WPF Investment views' bare "New" buttons a visible entity name** - Update
`TransactionsView.xaml`, `CreditsView.xaml`, and `PriceHistoryView.xaml`'s trigger `Content` to the
Title-Case entity name already present in each button's `ToolTip`, keeping the `ToolTip` itself
unchanged.

### Stage 3: Add Bill WPF Confirm Button

**5. Fix the Add Bill WPF confirm button text** - Update `AddBillFormView.xaml`'s confirm-button
content from the bare `Add`/`Adding...` to `Add Bill`/`Adding Bill...`, widening the button to fit
the longer text.

### Stage 4: Test Updates and Verification

**6. Update the Web test suite for the new text** - Update every RTL assertion in the test files the
spec lists (`MonthlyPage`, `TransferForm`, `BalanceAdjustmentForm`, `ReservaPage`, `TransactionsTab`,
`CreditsTab`, `PriceHistoryTab`) to query the new strings — same behaviors under test, new text only.

**7. Verify the full sweep** - Grep every AC-named file for the retired strings to confirm none
remain, then run the full Web and WPF build/test suites to confirm no regression.
