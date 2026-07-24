# Implementation Plan: F01. Expense Payment State Model

**Prerequisites:**
- .NET SDK (solution builds via `Financial.slnx`)
- Node/npm for `Financial.Web` (vitest, `tsc -b --noEmit`)
- No new packages, configuration, or environment variables

### Stage 1: Domain Model

**1. Payment status enum** - Add the `ExpensePaymentStatus` enum to the CashFlow domain enums, holding the three computed states. See spec Section 4.

**2. Expense entity state model** - Make the bank tag nullable, add the settlement date field, and expose the computed payment status on the `Expense` entity. See spec Sections 3 and 6 for the derivation rule and persistence expectations.

**3. Entity invariant and transitions** - Enforce the three valid field shapes in the entity's factory and update paths, and add the settle/unsettle transitions as the only operations that produce or clear the settled shape. See spec Section 3 (Decisions 1 and 4).

### Stage 2: Application and API Surface

**4. Expense validation relaxation** - Update the expense service so the bank tag is parsed only when provided, delegating shape rules to the entity while keeping existing field checks and error translation. See spec Section 5 for the resulting error behavior.

**5. DTO and mapping updates** - Make the bank tag nullable across the expense DTOs and expose the settlement date and computed payment status on the read model and its mapping. See spec Sections 4 and 5.

### Stage 3: Downstream Contract Alignment

**6. Importer accommodation** - Adjust the spreadsheet importer so rows that resolve a card tag construct expenses without a bank tag, keeping the importer valid under the new invariant. See spec Section 3 (Decision 5).

**7. Web contract types** - Update the web app's API types and monthly hook payloads to match the new expense shape without changing UI behavior. See spec Section 4 (Frontend).

**8. Full-solution verification** - Run the .NET test suite, the web test suite, and the TypeScript build check to confirm the solution is green end to end.
