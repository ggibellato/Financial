# Implementation Plan: F02. Expense Round-Up Capture

**Prerequisites:**
- F01 merged (`Bank` entity, `ICashFlowRepository.GetBanks()`, `BankNameResolver` available)
- .NET SDK; no new packages

### Stage 1: Domain Model

**1. `Expense` round-up field and invariant** - Add the stored round-up amount and the computed suggestion to the entity, along with the transition method that enforces the payment-shape and range invariants described in the spec, while leaving every other transition (`Create`, `UpdateDetails`, `Settle`, `Unsettle`) free of any round-up-recalculation side effect. See spec Sections 3 and 4.

### Stage 2: Application Layer

**2. Expense DTOs** - Add the round-up amount field to the create/update request DTOs and both the stored amount and the computed suggestion to the read DTO. See spec Sections 4 and 5.

**3. Expense service validation and suggestion** - Wire `ExpenseService` to validate a submitted round-up amount against the live bank list before applying it to the entity, and to compute the exposed suggestion under the eligibility rule. See spec Sections 3 and 4.

### Stage 3: Verification

**4. Full-solution verification** - Run the complete .NET test suite and exercise the create/update expense endpoints end-to-end to confirm the suggestion, validation errors, and update semantics behave as specified.
