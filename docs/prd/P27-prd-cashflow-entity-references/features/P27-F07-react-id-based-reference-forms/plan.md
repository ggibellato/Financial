# Implementation Plan: F07. React Id-Based Reference Forms

**Prerequisites:**
- Node/npm matching the existing `Financial.Web` project (Vite, TypeScript, Vitest)
- No new npm packages or environment variables
- F05 merged (Web API routes and DTOs already Guid-based)

### Stage 1: DTO Shapes and API Client

**1. `types.ts` Corrections** - Update `BankDto`, `ExpenseDto`/`CreateExpenseDto`/`UpdateExpenseDto`, `IncomeDto`/`CreateIncomeDto`/`UpdateIncomeDto`, `TransferDto`/`CreateTransferDto`/`UpdateTransferDto`, `BalanceAdjustmentDto`, and `MarkCardStatementPaidDto` to the real Guid+Name JSON shape the backend already sends/expects.

**2. API Client** - Rename `financialApiClient.ts`'s balance-adjustment methods' `bankName` parameter to `bankId`, keeping the same URL construction (now correctly matching F05's `{id:guid}` route).

### Stage 2: Hooks

**3. `useMonthly.ts`** - Change Expense/Income create/update payloads to send Id fields; switch `bankTotals`'s round-up lookup and `INCOME_SOURCES_WITH_GROSS_VALUE`'s visibility check from name-matching to Id-matching (resolving Id back to name only where the check itself is inherently name-keyed); switch `incomeTotals`'s grouping key to the denormalized name field; send the selected bank's Id when marking a card statement paid.

**4. `useTransferForm.ts` and `useBalanceAdjustmentForm.ts`** - Change form state and submit payloads to Id-based; update default-selection and edit-prefill logic to read the Id fields instead of name fields; update `useBalanceAdjustmentForm`'s `resolveCurrentBalance` and its `createBalanceAdjustment`/`updateBalanceAdjustment` calls to use the bank Id.

**5. `useBankOperations.ts`** - Update `getAdjustmentsByBank`/`deleteAdjustment` calls to pass a bank Id instead of a name; update `BankOperationEntry`'s fields to read the corrected denormalized name fields for display.

### Stage 3: Components

**6. Form Components** - Update `IncomeForm.tsx`, `ExpenseForm.tsx`, `TransferForm.tsx`, and `BalanceAdjustmentForm.tsx`'s bank/source `<select>` options from `value={x.name}` to `value={x.id}`, and their prop types from name strings to Id strings; update dependent logic (round-up visibility, same-bank/destination-exclusion checks) to compare by Id.

**7. Read-Only Displays** - Update `CardsGrid.tsx`'s mark-paid picker option value, and every grid/list component (`ExpensesSection.tsx`, `IncomeSection.tsx`, `BanksGrid.tsx`, `IncomingGrid.tsx`, `BankOperationsSection.tsx`) to read the corrected denormalized `*Name` field instead of the stale name field.

### Stage 4: Test Coverage

**8. Component and Hook Test Updates** - Update every test fixture that constructs a Bank/IncomeSource/Transfer/Income/Expense/BalanceAdjustment object to the corrected DTO shape; switch form-interaction and payload assertions from name strings to Id strings across the form component tests, the hook tests, and `MonthlyPage.tsx`'s integration test suite.

**9. Regression Confirmation** - Re-run `mapTransferErrorToField.test.ts` and `mapBalanceAdjustmentErrorToField.test.ts` unchanged to confirm the error-to-field mapping still works once the caller-side state it compares against is Guid-based, and confirm the full `Financial.Web` test suite is green.
