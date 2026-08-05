# Implementation Plan: F05. Web — Card Tab & Expense Form Support

**Prerequisites:**
- F02 (Invoice-Period Settlement Matching) and F04 (Backend Exposure of Charge/Invoice Fields) merged to `main`
- No new environment variables or configuration files

### Stage 1: Backend Ordering Fix

**1. Fix Card Tab/Expense List Ordering** - Change `ExpenseService.GetExpensesByMonth`/`GetUnpaidCardChargesByMonth`'s sort key from `Date` to the existing `OriginationDate` helper (`ChargeDate ?? Date`), so a settled card expense keeps its charge-date position instead of jumping to wherever its new payment date sorts. Cover with a regression test proving position survives settlement, per the spec's §3 Technical Decisions on why this shared-layer fix belongs here.

### Stage 2: Frontend Contract Types

**2. Update Expense API Types** - In `Financial.Web/src/api/types.ts`, add `chargeDate`/`invoiceDate` to `ExpenseDto` and `invoiceDate` to `CreateExpenseDto`/`UpdateExpenseDto`, and remove the stale `settledAt` field left over from before F01. Update every test fixture across the frontend that still constructs an `ExpenseDto` literal with `settledAt`.

### Stage 3: Expense Form Invoice-Month Field

**3. Add the Invoice Month Field to ExpenseForm** - Add an editable "Invoice Month" picker shown only in card mode (reusing the app's existing native `<input type="month">` pattern, no new library), pre-filled from the charge date when untouched, and shown disabled once the expense is settled, per the spec's §3 Technical Decisions.

**4. Wire Invoice-Month State Through useMonthly and MonthlyPage** - Add `createInvoiceDate`/`editInvoiceDate` state and reducer cases to `useMonthly.ts`, thread the field through `MonthlyPage.tsx`'s form-field maps, and include `invoiceDate` in both the create and update request payloads per the spec's unified construction rule.

### Stage 4: Test Coverage

**5. Frontend and Backend Test Coverage** - Add the form-field tests, the position-unchanged-after-settle integration test, and the backend ordering regression test described in the spec's §7 Testing Strategy.

### Stage 5: Full Verification

**6. Full Solution Build and Test Pass** - Build and test every affected .NET project and the Web frontend (`npm run build`, `npm test`) to confirm no regressions elsewhere.
