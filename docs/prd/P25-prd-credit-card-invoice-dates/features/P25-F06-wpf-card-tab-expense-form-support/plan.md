# Implementation Plan: F06. WPF — Card Tab & Expense Form Support

**Prerequisites:**
- F02 (Invoice-Period Settlement Matching), F04 (Backend Exposure of Charge/Invoice Fields), and F05 (Web — Card Tab & Expense Form Support, which shipped the shared `ExpenseService` sort-key fix) merged to `main`
- No new environment variables or configuration files

### Stage 1: Card Tab Display Fix

**1. Fix CreditCardExpensesView's Date Column** - Change the grid's date column binding from `Date` to `ChargeDate`, so the displayed charge date stays stable across settlement (the underlying sort order was already fixed server-side in F05).

### Stage 2: Expense Dialog Invoice Field

**2. Add Invoice Month State to MonthlyViewModel** - Add `ExpenseFormInvoiceYear`/`ExpenseFormInvoiceMonth` properties with the reactive-default-until-touched behavior described in the spec's §3 Technical Decisions, wired into `ShowCreateExpenseForm`/`ShowEditExpenseForm`.

**3. Add the Invoice Month Row to ExpenseFormView** - Insert a new row using the existing `MonthYearPicker` component, always visible when in card mode and disabled once the expense is settled, per the spec's §4 Component Overview.

**4. Wire InvoiceDate Into the Save Payload** - Update `SaveExpenseAsync` to include `InvoiceDate` in both the create and update DTOs, following the same card-mode-only rule already used for `CardTag`.

### Stage 3: Test Coverage

**5. ViewModel Test Coverage** - Add the default/resync/prefill/save tests described in the spec's §7 Testing Strategy, extending `TestStubs.cs`'s fixture-building helper to carry `ChargeDate`/`InvoiceDate` where needed.

### Stage 4: Full Verification

**6. Full Solution Build and Test Pass** - Build and test every affected project to confirm no regressions elsewhere, and do a final manual pass in the running WPF app (build and launch it) since RTL-equivalent UI tests aren't available for WPF the way they are for the Web frontend.
