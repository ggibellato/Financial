# Implementation Plan: F03. Monthly Expense Tracking

**Prerequisites:**
- Existing `Financial.CashFlow.Domain`/`.Application`/`.Infrastructure` projects and `data-cashflow.json` repository pattern from F02
- No new NuGet packages

### Stage 1: Domain Layer

**1. Flesh out the Expense entity** - Replace the placeholder `Expense` with its real fields (date, description, value, category, payment source, optional card tag), a `Create` factory that assigns a new id, and an `UpdateDetails` instance method for in-place edits, matching the private-setter style already used across the codebase.

**2. Remove-by-id on the root aggregate** - Add a `RemoveExpense` method to `CashFlowData` that removes the matching expense from its collection, leaving every other collection untouched.

**3. Domain tests** - Extend `CashFlowDataTests` for `RemoveExpense` and add `ExpenseTests` covering `Create` and `UpdateDetails`.

### Stage 2: Repository Extension

**4. Extend the repository abstraction** - Add a `DeleteExpense` method to `ICashFlowRepository` and implement it in `CashFlowJsonRepository` by delegating to the root aggregate's removal method.

### Stage 3: Application Layer — Service and Validation

**5. Expense DTOs** - Add the read model, create request, update request, and category-total DTOs.

**6. Enum parsers** - Add small parsers for `Category`, `PaymentSource`, and `CreditCard` string values, following the existing enum-parsing convention.

**7. Expense service** - Add `IExpenseService`/`ExpenseService` covering add, update, delete, list-by-month, and category-totals-by-month, validating input and raising descriptive errors for invalid or missing data before touching the repository.

**8. Application dependency injection** - Add the first DI extension for `Financial.CashFlow.Application`, registering the new service.

**9. Application-layer tests** - Add service tests covering every validation rule, the update/delete not-found paths, and the monthly query/aggregation behavior, plus tests for each enum parser.

### Stage 4: Presentation Layer

**10. Expenses controller** - Add HTTP endpoints for add/update/delete/list-by-month/category-totals-by-month, translating validation and not-found failures into descriptive error responses.

**11. Wire into Financial.Api** - Register the new Application-layer DI extension in `Program.cs` alongside the existing CashFlow registrations.

**12. API integration tests** - Add endpoint tests covering the full add/update/delete/list/category-totals round trip over HTTP, including the validation-message and not-found error responses.

### Stage 5: Verification

**13. Full-suite validation** - Build the entire solution and run every test project, confirming no regression to Investments or the existing CashFlow storage tests from F02.

**14. Manual verification** - Run `Financial.Api` locally and exercise the new endpoints directly (add, edit, delete, list, category totals) to confirm the behavior matches the acceptance criteria end-to-end, not just at the unit-test level.
