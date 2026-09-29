# Implementation Plan: Bank-Routed Withdrawal API

**Prerequisites:**
- .NET SDK matching `Financial.slnx`; Node/npm for `Financial.Web` type generation
- Work on its own branch/worktree (never on `main`); PR title uses a Conventional Commits prefix
- No new configuration or environment variables

### Stage 1: Contract and Domain Constant

**1. Withdrawal request contract** - Add the two optional bank and category fields to the withdrawal request DTO, following the spec's API Contracts section.

**2. Reserva category name constant** - Add the named constant for the `Reserva` category on the `Category` entity so no magic string is introduced.

### Stage 2: Service Behavior

**3. Validation** - In the reserve service, add the bank/category pairing, bank existence, chosen-category and `Reserva`-category rules from the spec, keeping the existing validation order and overdraft check intact.

**4. Bank-side expense creation** - Build the two expenses for the bank path and include them, with the reserve movement, in the existing compensating save so all three are applied or rolled back together.

### Stage 3: Contract Artifacts and Verification

**5. OpenAPI snapshot** - Regenerate the committed snapshot, review that only the two new optional properties changed, and confirm the contract test passes.

**6. Web generated types** - Regenerate the Web API types from the snapshot and commit the result so the freshness test passes.

**7. Tests and Definition of Done** - Add the service and endpoint tests listed in the spec, run the full backend suite and the Web build/tests, and complete the Definition of Done checklist from `docs/rules/implementation.md`, including a comment/XML-doc grep of new code.
