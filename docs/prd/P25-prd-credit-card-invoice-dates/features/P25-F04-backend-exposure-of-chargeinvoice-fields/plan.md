# Implementation Plan: F04. Backend Exposure of Charge/Invoice Fields

**Prerequisites:**
- F01 (Expense Payment-Date Domain Model Rework) merged to `main` — provides `ChargeDate`/`InvoiceDate`/`SetInvoiceDate`
- No new environment variables or configuration files

### Stage 1: Contract and Service Wiring

**1. Expose ChargeDate/InvoiceDate on ExpenseDTO** - Add both fields to the read model and map them in `ExpenseService.ToDto`.

**2. Accept InvoiceDate Override on Create and Update** - Add the optional `InvoiceDate` field to `ExpenseCreateDTO`/`ExpenseUpdateDTO`, pass it through `AddExpenseAsync` to `Expense.Create`, and wire `UpdateExpenseAsync` to call `Expense.SetInvoiceDate` only when an actual change is requested, per the spec's §3 no-op-echo decision.

### Stage 2: Test Coverage

**3. Application and API Test Coverage** - Add the create/update/read coverage described in the spec's §7 Testing Strategy to both `ExpenseServiceTests` and `ExpenseEndpointsTests`.

### Stage 3: Full Verification

**4. Full Solution Build and Test Pass** - Build and test every affected project to confirm the new contract fields introduce no regressions elsewhere.
