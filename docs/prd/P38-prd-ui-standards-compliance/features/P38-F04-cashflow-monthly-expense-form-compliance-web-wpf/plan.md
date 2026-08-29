# Implementation Plan: F04. CashFlow Monthly Expense Form Compliance (Web + WPF)

**Prerequisites:**
- F01, F02, F03 merged (tokens, shared primitives, and naming fixes all available).
- No new tools/libraries — Fluent `TabList`/`Tab`/`Table`/`TableHeader`/`TableRow`/`TableHeaderCell`/
  `TableBody`/`TableCell` are already part of the installed `@fluentui/react-components` package.

### Stage 1: Web Field Reorder and Per-Field Validation

**1. Tag each existing validation branch with its field** - Add `saveErrorField` to
`useExpenseForm.ts`'s reducer state, set alongside the existing `saveError` message at each of the
sequential validation checks (date, description, value, card, round-up amount).

**2. Reorder `ExpenseForm.tsx`'s fields and wire F02's primitives** - Change the field sequence to
Date → Description → Payment Source/Card (or the settled notice) → Value → Category → conditional
fields (Round-Up, Invoice Month, Counts-toward-tithe); adopt the shared `useFieldError` hook for
per-field messages and add `required` to the genuinely required fields.

### Stage 2: Web TabList Migration

**3. Replace `MonthlyPage.tsx`'s custom tab buttons with Fluent `TabList`/`Tab`** - Drive the
existing `activeTab` state and `handleTabClick` logic through `TabList`'s `selectedValue`/
`onTabSelect`, then remove the now-dead custom tab CSS (including the hardcoded active-tab color).

### Stage 3: Web Table Migration

**4. Replace `ExpensesSection.tsx`'s native `<table>` with Fluent `Table` primitives** - Swap
`<table>`/`<thead>`/`<tr>`/`<th>`/`<tbody>`/`<td>` for `Table`/`TableHeader`/`TableRow`/
`TableHeaderCell`/`TableBody`/`TableCell`, keeping the existing `SortableColumnHeader`/
`ColumnFilterMenu`/sort-and-filter hooks unchanged inside the new structure.

### Stage 4: WPF Field Reorder and Per-Field Validation

**5. Add per-field derived error properties to `ExpenseWorkflowViewModel`** - One property per
field (`DateFieldError`, `DescriptionFieldError`, `CategoryFieldError`, `ValueFieldError`,
`PaymentModeFieldError`, `RoundUpAmountFieldError`), each matching `ExpenseSaveError` against
`ExpenseFormValidation`'s known per-violation text, notified alongside `ExpenseSaveError` itself.

**6. Reorder `ExpenseFormView.xaml`'s fields and wire the new error properties** - Same target order
as Web; add `FieldErrorTextStyle` text under each field bound to its derived error property, and the
required-asterisk/`AutomationProperties.HelpText` pattern from F02 on the required fields.

### Stage 5: Test Suite Alignment and Manual Verification

**7. Update and extend the affected test suites** - `ExpenseForm.test.tsx`, `useExpenseForm.test.ts`,
`MonthlyPage.test.tsx`, `ExpensesSection.test.tsx` (Web) and `ExpenseWorkflowViewModelTests.cs`
(WPF), covering the new field order, the per-field validation primitive, and confirming existing
settled-state and sort/filter behavior still passes unmodified.

**8. Manually verify both platforms** - Open the Expense form and Monthly page on Web (arrow-key tab
navigation, field order, per-field validation, settled-state locking) and on WPF (field order,
per-field validation, settled-state locking) per `docs/ui/review-checklist.md`.
