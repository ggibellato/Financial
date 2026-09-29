# Implementation Plan: Web Withdrawal Form (React)

**Prerequisites:**
- F01 merged (extended `WithdrawalRequestDto` already in `Financial.Web/src/api/generated/openapi.ts`)
- Work on its own branch (never on `main`); PR title uses a Conventional Commits prefix
- Node/npm for `Financial.Web`; no new dependencies, no environment variables
- Read `docs/rules/ui.md`, `docs/ui/forms-data-and-visualisations.md` and `docs/ui/react.md` before coding; no comments in new code

### Stage 1: Rules and State

**1. Withdrawal category rules** - Add the small utility holding the `Reserva` name constant, the eligible-category filter and the bucket-name default lookup, as described in the spec's Component Overview.

**2. Reserva hook data and state** - Extend the Reserva hook to load banks and categories with the page data, hold the user's explicit bank and category choice, expose the effective category, and reset both on cancel and success.

**3. Validation and request** - Add the "category required when a bank is selected" rule to the withdrawal submit and send the bank and category ids (or nulls) on the request, keeping the overdraft confirmation flow unchanged.

### Stage 2: Presentation

**4. Withdrawal form fields** - Add the "Through bank" select with contextual help and the conditional "Expense category" select in the field order defined by the spec, and disable every control while saving.

**5. Page wiring** - Pass the new state and lists from the Reserva page into the withdrawal form.

### Stage 3: Verification

**6. Tests** - Add the utility, hook and page tests listed in the spec, updating the existing API-client mocks so the extended fetch does not break unrelated tests.

**7. UI review and Definition of Done** - Run type-check, lint and the Web test suite, run the manual dev-server check against a temporary copy of the data, complete `docs/ui/review-checklist.md` for the form, get the UI reviewer's sign-off, and complete the Definition of Done from `docs/rules/implementation.md`, including a grep of the diff for added comments.
